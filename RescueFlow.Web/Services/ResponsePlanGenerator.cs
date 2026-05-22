using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;

namespace RescueFlow.Web.Services;

public class ResponsePlanGenerator
{
    private readonly IRoutingService _routingService;

    public ResponsePlanGenerator(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    public async Task<ResponsePlan> GeneratePlanAsync(
        RescueCase rescueCase,
        List<RescueVehicle> selectedVehicles,
        Hospital? selectedHospital,
        int severityLevel,
        List<ResourceRequirement> vehicleRequirements,
        HospitalRequirement hospitalRequirement,
        List<string> rejectedVehicles,
        List<string> rejectedHospitals)
    {
        var plan = new ResponsePlan
        {
            RescueCaseId = rescueCase.Id,
            RescueCase = rescueCase,
            SelectedHospital = selectedHospital,
            SelectedHospitalId = selectedHospital?.Id,
            SeverityLevel = severityLevel,
            RequiresHospital = hospitalRequirement.Required,
            CreatedAt = DateTime.UtcNow,
            AlgorithmVersion = "SC-RCMDA-v1.0",
            RequiredVehiclesJson = JsonSerializer.Serialize(vehicleRequirements),
            SelectedVehiclesJson = JsonSerializer.Serialize(selectedVehicles.Select(v => new { v.Id, v.Code, v.VehicleType })),
            RejectedVehiclesJson = JsonSerializer.Serialize(rejectedVehicles),
            RejectedHospitalsJson = JsonSerializer.Serialize(rejectedHospitals)
        };

        // Estimate scene ETA: maximum ETA of any assigned vehicle (since all are dispatched simultaneously)
        double maxSceneEta = 0.0;
        foreach (var vehicle in selectedVehicles)
        {
            var route = await _routingService.EstimateAsync(
                new Location(vehicle.CurrentLatitude, vehicle.CurrentLongitude),
                new Location(rescueCase.Latitude, rescueCase.Longitude)
            );
            
            plan.Assignments.Add(new ResponseAssignment
            {
                ResponsePlan = plan,
                RescueVehicleId = vehicle.Id,
                RescueVehicle = vehicle,
                EtaMinutes = route.EtaMinutes,
                IsActive = true
            });

            if (route.EtaMinutes > maxSceneEta)
            {
                maxSceneEta = route.EtaMinutes;
            }
        }

        plan.EstimatedSceneEtaMinutes = maxSceneEta;

        // Estimate hospital ETA: scene ETA + ETA from scene to hospital
        if (selectedHospital != null)
        {
            var hospitalRoute = await _routingService.EstimateAsync(
                new Location(rescueCase.Latitude, rescueCase.Longitude),
                new Location(selectedHospital.Latitude, selectedHospital.Longitude)
            );
            plan.EstimatedHospitalEtaMinutes = maxSceneEta + hospitalRoute.EtaMinutes;
        }

        return plan;
    }
}
