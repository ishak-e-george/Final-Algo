using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;

namespace RescueFlow.Web.Services;

public class AuditLogger
{
    private readonly RescueFlowDbContext _context;

    public AuditLogger(RescueFlowDbContext context)
    {
        _context = context;
    }

    public async Task LogAuditAsync(int? rescueCaseId, string action, string details)
    {
        var log = new AuditLog
        {
            RescueCaseId = rescueCaseId,
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogDispatchDecisionAsync(int? rescueCaseId, string action, string details)
    {
        var log = new DispatchDecisionLog
        {
            RescueCaseId = rescueCaseId,
            Action = action,
            Details = details,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.DispatchDecisionLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogSafetyValidationResultAsync(int rescueCaseId, bool passed, string message, List<string> checkedRules)
    {
        var log = new SafetyValidationResult
        {
            RescueCaseId = rescueCaseId,
            ValidationPassed = passed,
            Message = message,
            CheckedRulesJson = JsonSerializer.Serialize(checkedRules),
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.SafetyValidationResults.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogSafetyViolationAsync(int rescueCaseId, string ruleName, string violationDetails)
    {
        var log = new SafetyViolation
        {
            RescueCaseId = rescueCaseId,
            RuleName = ruleName,
            ViolationDetails = violationDetails,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.SafetyViolations.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogAlgorithmRunAsync(
        int rescueCaseId,
        int severityScore,
        List<ResourceRequirement> requiredVehicles,
        List<RescueVehicle> selectedVehicles,
        List<string> rejectedVehicles,
        int? selectedHospitalId,
        List<string> rejectedHospitals,
        bool validationPassed,
        string validationErrors,
        string executionTrace)
    {
        var log = new AlgorithmRunLog
        {
            RescueCaseId = rescueCaseId,
            SeverityScore = severityScore,
            RequiredVehiclesJson = JsonSerializer.Serialize(requiredVehicles),
            SelectedVehiclesJson = JsonSerializer.Serialize(selectedVehicles.Select(v => new { v.Id, v.Code, v.VehicleType })),
            RejectedVehiclesJson = JsonSerializer.Serialize(rejectedVehicles),
            SelectedHospitalId = selectedHospitalId,
            RejectedHospitalsJson = JsonSerializer.Serialize(rejectedHospitals),
            ValidationPassed = validationPassed,
            ValidationErrors = validationErrors,
            ExecutionTrace = executionTrace,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.AlgorithmRunLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
