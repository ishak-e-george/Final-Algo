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

public class HospitalMatcher
{
    private readonly RescueFlowDbContext _context;
    private readonly IRoutingService _routingService;

    public HospitalMatcher(RescueFlowDbContext context, IRoutingService routingService)
    {
        _context = context;
        _routingService = routingService;
    }

    public async Task<(Hospital? Selected, List<string> RejectedReasons)> FindBestHospitalAsync(
        HospitalRequirement requirement, double caseLat, double caseLng)
    {
        var rejectedReasons = new List<string>();
        if (!requirement.Required)
            return (null, rejectedReasons);

        var hospitals = await _context.Hospitals
            .Include(h => h.Capabilities)
            .Include(h => h.Capacity)
            .Where(h => h.IsActive)
            .ToListAsync();

        var candidates = new List<(Hospital Hospital, double Eta, double Distance)>();

        foreach (var hospital in hospitals)
        {
            // Check capabilities
            bool hasCapabilities = requirement.RequiredCapabilities.All(reqCap =>
                hospital.Capabilities.Any(hc => hc.Capability == reqCap));

            if (!hasCapabilities)
            {
                rejectedReasons.Add($"Hospital {hospital.Name} missing required capabilities: {string.Join(", ", requirement.RequiredCapabilities)}");
                continue;
            }

            // Check capacity
            bool hasCapacity = HasCapacity(hospital, requirement);
            if (!hasCapacity)
            {
                rejectedReasons.Add($"Hospital {hospital.Name} has insufficient capacity for load: {requirement.RequiredCapacity}");
                continue;
            }

            // Estimate ETA from case to hospital
            var route = await _routingService.EstimateAsync(
                new Location(caseLat, caseLng),
                new Location(hospital.Latitude, hospital.Longitude)
            );

            candidates.Add((hospital, route.EtaMinutes, route.DistanceKm));
        }

        if (candidates.Count == 0)
        {
            return (null, rejectedReasons);
        }

        // Rank by lowest ETA
        var best = candidates
            .OrderBy(c => c.Eta)
            .First();

        return (best.Hospital, rejectedReasons);
    }

    public bool HasCapacity(Hospital hospital, HospitalRequirement requirement)
    {
        var capacity = hospital.Capacity;

        foreach (var capability in requirement.RequiredCapabilities)
        {
            bool ok = capability switch
            {
                HospitalCapability.Trauma =>
                    capacity.TraumaCapacity - capacity.CurrentTraumaLoad >= requirement.RequiredCapacity,

                HospitalCapability.ICU =>
                    capacity.IcuCapacity - capacity.CurrentIcuLoad >= requirement.RequiredCapacity,

                HospitalCapability.BurnUnit =>
                    capacity.BurnCapacity - capacity.CurrentBurnLoad >= requirement.RequiredCapacity,

                HospitalCapability.Toxicology =>
                    capacity.ToxicologyCapacity - capacity.CurrentToxicologyLoad >= requirement.RequiredCapacity,

                HospitalCapability.GeneralEmergency =>
                    capacity.EmergencyCapacity - capacity.CurrentEmergencyLoad >= requirement.RequiredCapacity,

                _ => true
            };

            if (!ok)
                return false;
        }

        return true;
    }
}
