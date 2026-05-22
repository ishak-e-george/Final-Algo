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

public class AutoAssignmentService
{
    private readonly RescueFlowDbContext _context;
    private readonly SafetyValidator _safetyValidator;

    public AutoAssignmentService(RescueFlowDbContext context, SafetyValidator safetyValidator)
    {
        _context = context;
        _safetyValidator = safetyValidator;
    }

    public async Task<ResponsePlan> AutoAssignAsync(
        RescueCase rescueCase,
        ResponsePlan responsePlan,
        List<RescueVehicle> selectedVehicles,
        Hospital? selectedHospital,
        List<ResourceRequirement> vehicleRequirements,
        HospitalRequirement hospitalRequirement)
    {
        // Start transaction
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Atomically reserve selected vehicles (Status = Assigned where Status = Available)
            foreach (var vehicle in selectedVehicles)
            {
                int affectedVehicles = await _context.RescueVehicles
                    .Where(v => v.Id == vehicle.Id && v.Status == VehicleStatus.Available)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(v => v.Status, VehicleStatus.Assigned));

                if (affectedVehicles == 0)
                {
                    throw new InvalidOperationException($"Safety Conflict: Vehicle {vehicle.Code} became unavailable or was assigned to another case.");
                }
            }

            // 2. Atomically reserve hospital capacity if required
            if (hospitalRequirement.Required && selectedHospital != null)
            {
                var capacityQuery = _context.HospitalCapacities.Where(c => c.HospitalId == selectedHospital.Id);

                bool hasIcu = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.ICU);
                bool hasTrauma = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.Trauma);
                bool hasBurn = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.BurnUnit);
                bool hasToxicology = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.Toxicology);
                bool hasGeneral = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.GeneralEmergency);

                if (hasIcu) capacityQuery = capacityQuery.Where(c => c.IcuCapacity - c.CurrentIcuLoad >= hospitalRequirement.RequiredCapacity);
                if (hasTrauma) capacityQuery = capacityQuery.Where(c => c.TraumaCapacity - c.CurrentTraumaLoad >= hospitalRequirement.RequiredCapacity);
                if (hasBurn) capacityQuery = capacityQuery.Where(c => c.BurnCapacity - c.CurrentBurnLoad >= hospitalRequirement.RequiredCapacity);
                if (hasToxicology) capacityQuery = capacityQuery.Where(c => c.ToxicologyCapacity - c.CurrentToxicologyLoad >= hospitalRequirement.RequiredCapacity);
                if (hasGeneral) capacityQuery = capacityQuery.Where(c => c.EmergencyCapacity - c.CurrentEmergencyLoad >= hospitalRequirement.RequiredCapacity);

                int affectedHospitals = await capacityQuery.ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.CurrentIcuLoad, c => hasIcu ? c.CurrentIcuLoad + hospitalRequirement.RequiredCapacity : c.CurrentIcuLoad)
                    .SetProperty(c => c.CurrentTraumaLoad, c => hasTrauma ? c.CurrentTraumaLoad + hospitalRequirement.RequiredCapacity : c.CurrentTraumaLoad)
                    .SetProperty(c => c.CurrentBurnLoad, c => hasBurn ? c.CurrentBurnLoad + hospitalRequirement.RequiredCapacity : c.CurrentBurnLoad)
                    .SetProperty(c => c.CurrentToxicologyLoad, c => hasToxicology ? c.CurrentToxicologyLoad + hospitalRequirement.RequiredCapacity : c.CurrentToxicologyLoad)
                    .SetProperty(c => c.CurrentEmergencyLoad, c => hasGeneral ? c.CurrentEmergencyLoad + hospitalRequirement.RequiredCapacity : c.CurrentEmergencyLoad)
                );

                if (affectedHospitals == 0)
                {
                    throw new InvalidOperationException($"Safety Conflict: Selected hospital {selectedHospital.Name} exceeded capacity limits during assignment.");
                }
            }

            // 3. Re-run final SafetyValidator after reservations (post-reservation check)
            // Load fresh reserved vehicles and hospital states to run validator
            var freshVehicles = await _context.RescueVehicles
                .Include(v => v.Station)
                .Include(v => v.VehicleEquipment)
                    .ThenInclude(ve => ve.Equipment)
                .Where(v => selectedVehicles.Select(sv => sv.Id).Contains(v.Id))
                .ToListAsync();

            Hospital? freshHospital = null;
            if (selectedHospital != null)
            {
                freshHospital = await _context.Hospitals
                    .Include(h => h.Capabilities)
                    .Include(h => h.Capacity)
                    .FirstOrDefaultAsync(h => h.Id == selectedHospital.Id);
            }

            // Temporarily mock vehicle status back to Available just for validation check
            var validationVehicles = freshVehicles.Select(v => new RescueVehicle
            {
                Id = v.Id,
                Code = v.Code,
                VehicleType = v.VehicleType,
                Status = VehicleStatus.Available, // mock for validator since we just set it to Assigned
                CurrentCrewCount = v.CurrentCrewCount,
                VehicleEquipment = v.VehicleEquipment,
                CurrentLatitude = v.CurrentLatitude,
                CurrentLongitude = v.CurrentLongitude,
                Station = v.Station
            }).ToList();

            var (isValid, violations) = _safetyValidator.Validate(
                vehicleRequirements,
                validationVehicles,
                hospitalRequirement,
                freshHospital
            );

            if (!isValid)
            {
                throw new InvalidOperationException($"Post-reservation Safety Validation failed: {string.Join("; ", violations)}");
            }

            // 4. Save ResponsePlan and ResponseAssignments
            // Attach relationships
            rescueCase.Status = CaseStatus.AutoAssigned;
            responsePlan.SafetyValidationPassed = true;
            responsePlan.ValidationMessage = "Safety validation passed. Assigned automatically.";

            // Save ResponsePlan and Assignments (ExecuteUpdate updates fields directly in db, so we save plan via SaveChanges)
            _context.RescueCases.Update(rescueCase);
            _context.ResponsePlans.Add(responsePlan);
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return responsePlan;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
