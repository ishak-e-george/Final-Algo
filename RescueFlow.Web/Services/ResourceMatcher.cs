using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Services;

public class ResourceMatcher
{
    private readonly RescueFlowDbContext _context;
    private readonly IRoutingService _routingService;

    public ResourceMatcher(RescueFlowDbContext context, IRoutingService routingService)
    {
        _context = context;
        _routingService = routingService;
    }

    public async Task<(List<RescueVehicle> Selected, List<string> RejectedReasons)> FindEligibleVehiclesAsync(
        List<ResourceRequirement> requirements, double caseLat, double caseLng)
    {
        var selectedVehicles = new List<RescueVehicle>();
        var rejectedReasons = new List<string>();

        // Load all available active vehicles of any needed type to calculate scarcity on station level
        var allAvailableVehicles = await _context.RescueVehicles
            .Include(v => v.Station)
            .Include(v => v.VehicleEquipment)
                .ThenInclude(ve => ve.Equipment)
            .Where(v => v.Status == VehicleStatus.Available && v.Station.IsActive)
            .ToListAsync();

        foreach (var req in requirements)
        {
            // 1. Hard filter candidates
            var candidates = allAvailableVehicles
                .Where(v => v.VehicleType == req.VehicleType && 
                            v.CurrentCrewCount >= req.RequiredPersonnelPerVehicle &&
                            req.RequiredEquipment.All(reqEquip => 
                                v.VehicleEquipment.Any(ve => ve.Equipment.Name == reqEquip)))
                .ToList();

            if (candidates.Count < req.RequiredVehicleCount)
            {
                rejectedReasons.Add($"Insufficient available vehicles for {req.VehicleType}. Required: {req.RequiredVehicleCount}, Available/Valid: {candidates.Count}");
                continue;
            }

            // 2. Calculate ETA for each candidate
            var candidateEstimates = new List<(RescueVehicle Vehicle, double Eta, double Distance, double ScarcityScore)>();

            foreach (var vehicle in candidates)
            {
                var route = await _routingService.EstimateAsync(
                    new Location(vehicle.CurrentLatitude, vehicle.CurrentLongitude),
                    new Location(caseLat, caseLng)
                );

                // Scarcity calculations:
                // Scarcity Penalty: If the station has very few available vehicles of this type, we penalize it
                // count of available vehicles of this type at this vehicle's station:
                double stationAvailableCount = allAvailableVehicles
                    .Count(v => v.StationId == vehicle.StationId && v.VehicleType == req.VehicleType);
                
                // Scarcity penalty is higher if station has fewer available vehicles left (1 / count)
                double scarcityPenalty = stationAvailableCount > 0 ? (1.0 / stationAvailableCount) : 10.0;

                candidateEstimates.Add((vehicle, route.EtaMinutes, route.DistanceKm, scarcityPenalty));
            }

            // 3. Sort candidates using multiple ranking criteria:
            // - Lowest ETA (primary)
            // - Lowest Scarcity Penalty (secondary tie-breaker: prefers stations with more backup vehicles)
            // - Highest Crew Surplus (third tie-breaker)
            var sortedCandidates = candidateEstimates
                .OrderBy(c => c.Eta)
                .ThenBy(c => c.ScarcityScore)
                .ThenByDescending(c => c.Vehicle.CurrentCrewCount - req.RequiredPersonnelPerVehicle)
                .Select(c => c.Vehicle)
                .Take(req.RequiredVehicleCount)
                .ToList();

            selectedVehicles.AddRange(sortedCandidates);
            
            // Remove selected vehicles from our local available pool so they aren't double-assigned in the same matching round
            foreach (var selected in sortedCandidates)
            {
                allAvailableVehicles.Remove(selected);
            }
        }

        return (selectedVehicles, rejectedReasons);
    }
}
