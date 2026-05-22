using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.AlgorithmModels;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;
using RescueFlow.Web.Models.ViewModels;
using RescueFlow.Web.Services;

namespace RescueFlow.Web.Controllers;

public class RescueCasesController : Controller
{
    private readonly RescueFlowDbContext _context;
    private readonly CaseFactsBuilder _factsBuilder;
    private readonly CaseClassifier _classifier;
    private readonly SeverityAnalyzer _severityAnalyzer;
    private readonly RequirementEngine _requirementEngine;
    private readonly ResourceMatcher _resourceMatcher;
    private readonly HospitalMatcher _hospitalMatcher;
    private readonly ResponsePlanGenerator _planGenerator;
    private readonly SafetyValidator _safetyValidator;
    private readonly AutoAssignmentService _assignmentService;
    private readonly AuditLogger _auditLogger;

    public RescueCasesController(
        RescueFlowDbContext context,
        CaseFactsBuilder factsBuilder,
        CaseClassifier classifier,
        SeverityAnalyzer severityAnalyzer,
        RequirementEngine requirementEngine,
        ResourceMatcher resourceMatcher,
        HospitalMatcher hospitalMatcher,
        ResponsePlanGenerator planGenerator,
        SafetyValidator safetyValidator,
        AutoAssignmentService assignmentService,
        AuditLogger auditLogger)
    {
        _context = context;
        _factsBuilder = factsBuilder;
        _classifier = classifier;
        _severityAnalyzer = severityAnalyzer;
        _requirementEngine = requirementEngine;
        _resourceMatcher = resourceMatcher;
        _hospitalMatcher = hospitalMatcher;
        _planGenerator = planGenerator;
        _safetyValidator = safetyValidator;
        _assignmentService = assignmentService;
        _auditLogger = auditLogger;
    }

    // GET: RescueCases
    public async Task<IActionResult> Index()
    {
        var cases = await _context.RescueCases
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(cases);
    }

    // GET: RescueCases/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var rescueCase = await _context.RescueCases
            .Include(c => c.ResponsePlan!)
                .ThenInclude(p => p!.SelectedHospital)
            .Include(c => c.ResponsePlan!)
                .ThenInclude(p => p!.Assignments)
                    .ThenInclude(a => a.RescueVehicle)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (rescueCase == null) return NotFound();

        // Load logs
        ViewBag.AuditLogs = await _context.AuditLogs
            .Where(l => l.RescueCaseId == id)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        ViewBag.DecisionLogs = await _context.DispatchDecisionLogs
            .Where(l => l.RescueCaseId == id)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        ViewBag.AlgorithmLogs = await _context.AlgorithmRunLogs
            .Where(l => l.RescueCaseId == id)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        ViewBag.ValidationResults = await _context.SafetyValidationResults
            .Where(l => l.RescueCaseId == id)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        ViewBag.SafetyViolations = await _context.SafetyViolations
            .Where(l => l.RescueCaseId == id)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync();

        return View(rescueCase);
    }

    // GET: RescueCases/Create
    public IActionResult Create()
    {
        return View(new RescueCaseCreateViewModel());
    }

    // POST: RescueCases/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RescueCaseCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // 1. Create and Save Pending RescueCase
        var rescueCase = new RescueCase
        {
            ReporterName = model.ReporterName,
            ReporterPhone = model.ReporterPhone,
            LocationDescription = model.LocationDescription,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            IncidentCategory = model.IncidentCategory,
            InjuryType = model.InjuryType,
            NumberOfPatients = model.NumberOfPatients,
            HasSevereBleeding = model.HasSevereBleeding,
            HasBreathingProblem = model.HasBreathingProblem,
            IsUnconscious = model.IsUnconscious,
            HasFire = model.HasFire,
            HasTrappedVictim = model.HasTrappedVictim,
            HasChemicalRisk = model.HasChemicalRisk,
            HasExplosionRisk = model.HasExplosionRisk,
            Description = model.Description,
            Status = CaseStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.RescueCases.Add(rescueCase);
        await _context.SaveChangesAsync();

        await _auditLogger.LogAuditAsync(rescueCase.Id, "CaseCreated", $"Incident reported by {model.ReporterName}. Status: Pending.");

        // 2. Perform Input Validation for Escalation
        List<string> escalationReasons = new();

        if (model.NumberOfPatients < 1)
        {
            escalationReasons.Add("Invalid patient count (must be >= 1).");
        }
        if (model.Latitude == 0.0 && model.Longitude == 0.0)
        {
            escalationReasons.Add("Impossible coordinates (0.0, 0.0).");
        }
        if (model.InjuryType == InjuryType.None && model.NumberOfPatients >= 1)
        {
            escalationReasons.Add("Contradictory input: InjuryType is 'None' but patient count is greater than zero.");
        }
        if (model.HasChemicalRisk && string.IsNullOrWhiteSpace(model.Description))
        {
            escalationReasons.Add("Incomplete input: HazMat risk reported but no chemical type or description specified.");
        }

        if (escalationReasons.Any())
        {
            rescueCase.Status = CaseStatus.Escalated;
            await _context.SaveChangesAsync();

            await _auditLogger.LogAuditAsync(rescueCase.Id, "InputValidationFailed", "Case escalated due to input validation errors.");
            await _auditLogger.LogDispatchDecisionAsync(rescueCase.Id, "EscalateCase", $"Input validation failed: {string.Join("; ", escalationReasons)}");
            
            return RedirectToAction(nameof(Details), new { id = rescueCase.Id });
        }

        // 3. Build CaseFacts & Classify
        var facts = _factsBuilder.Build(rescueCase);
        var classification = _classifier.Classify(facts);

        // 4. Calculate Severity (including hard safety overrides)
        int severity = _severityAnalyzer.CalculateSeverity(facts);
        rescueCase.CalculatedSeverity = severity;
        await _context.SaveChangesAsync();

        // 5. Generate Resource & Hospital Requirements
        var vehicleRequirements = _requirementEngine.GenerateVehicleRequirements(facts, classification, severity);
        var hospitalRequirement = _requirementEngine.GenerateHospitalRequirement(facts, classification, severity);

        // 6. Find Candidates
        var (selectedVehicles, rejectedVehicles) = await _resourceMatcher.FindEligibleVehiclesAsync(
            vehicleRequirements, rescueCase.Latitude, rescueCase.Longitude);

        Hospital? selectedHospital = null;
        List<string> rejectedHospitals = new();
        if (hospitalRequirement.Required)
        {
            var (hospital, rejected) = await _hospitalMatcher.FindBestHospitalAsync(
                hospitalRequirement, rescueCase.Latitude, rescueCase.Longitude);
            selectedHospital = hospital;
            rejectedHospitals = rejected;
        }

        // 7. Generate Response Plan
        var plan = await _planGenerator.GeneratePlanAsync(
            rescueCase,
            selectedVehicles,
            selectedHospital,
            rescueCase.CalculatedSeverity,
            vehicleRequirements,
            hospitalRequirement,
            rejectedVehicles,
            rejectedHospitals
        );

        // 8. Run Pre-Validation Safety Checks
        var (preValid, preViolations) = _safetyValidator.Validate(
            vehicleRequirements, selectedVehicles, hospitalRequirement, selectedHospital);

        await _auditLogger.LogSafetyValidationResultAsync(
            rescueCase.Id,
            preValid,
            preValid ? "Pre-reservation safety check passed." : $"Pre-reservation safety check failed: {string.Join("; ", preViolations)}",
            new List<string> { "VehicleCountRule", "VehicleCrewRule", "VehicleEquipmentRule", "HospitalCapabilityRule", "HospitalCapacityRule" }
        );

        if (!preValid)
        {
            // Mark as BlockedNoSafePlan
            rescueCase.Status = CaseStatus.BlockedNoSafePlan;
            plan.SafetyValidationPassed = false;
            plan.ValidationMessage = $"Pre-reservation Safety Validation Failed: {string.Join("; ", preViolations)}";
            _context.ResponsePlans.Add(plan);
            await _context.SaveChangesAsync();

            foreach (var violation in preViolations)
            {
                await _auditLogger.LogSafetyViolationAsync(rescueCase.Id, "PreReservationCheck", violation);
            }

            await _auditLogger.LogDispatchDecisionAsync(rescueCase.Id, "BlockDispatch", "Dispatch blocked. Ineligible resources or insufficient hospital capacities.");
            await _auditLogger.LogAlgorithmRunAsync(
                rescueCase.Id,
                rescueCase.CalculatedSeverity,
                vehicleRequirements,
                selectedVehicles,
                rejectedVehicles,
                selectedHospital?.Id,
                rejectedHospitals,
                false,
                string.Join("; ", preViolations),
                "Pre-reservation checks failed. Dispatch aborted."
            );

            return RedirectToAction(nameof(Details), new { id = rescueCase.Id });
        }

        // 9. Start Transaction and attempt Auto Assignment (locking resources atomically)
        try
        {
            var finalizedPlan = await _assignmentService.AutoAssignAsync(
                rescueCase,
                plan,
                selectedVehicles,
                selectedHospital,
                vehicleRequirements,
                hospitalRequirement
            );

            await _auditLogger.LogDispatchDecisionAsync(rescueCase.Id, "AutoAssignSuccess", $"Response plan successfully locked and assigned. Vehicles: {string.Join(", ", selectedVehicles.Select(v => v.Code))}. Hospital: {selectedHospital?.Name ?? "None"}.");
            await _auditLogger.LogAlgorithmRunAsync(
                rescueCase.Id,
                rescueCase.CalculatedSeverity,
                vehicleRequirements,
                selectedVehicles,
                rejectedVehicles,
                selectedHospital?.Id,
                rejectedHospitals,
                true,
                "",
                "SC-RCMDA completed successfully. Resources locked and committed."
            );
        }
        catch (Exception ex)
        {
            // Catch concurrency issues or post-reservation safety failures
            rescueCase.Status = CaseStatus.BlockedNoSafePlan;
            plan.SafetyValidationPassed = false;
            plan.ValidationMessage = $"Atomic Reservation Error: {ex.Message}";
            
            // Re-fetch context to reset tracked entities and save failed status
            _context.Entry(rescueCase).State = EntityState.Modified;
            _context.ResponsePlans.Add(plan);
            await _context.SaveChangesAsync();

            await _auditLogger.LogSafetyViolationAsync(rescueCase.Id, "AtomicReservationConflict", ex.Message);
            await _auditLogger.LogDispatchDecisionAsync(rescueCase.Id, "BlockDispatchConflict", $"Resource lock conflict: {ex.Message}");
            await _auditLogger.LogAlgorithmRunAsync(
                rescueCase.Id,
                rescueCase.CalculatedSeverity,
                vehicleRequirements,
                selectedVehicles,
                rejectedVehicles,
                selectedHospital?.Id,
                rejectedHospitals,
                false,
                ex.Message,
                "Atomic reservation exception. Aborted."
            );
        }

        return RedirectToAction(nameof(Details), new { id = rescueCase.Id });
    }

    // POST: RescueCases/Close/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var rescueCase = await _context.RescueCases
            .Include(c => c.ResponsePlan!)
                .ThenInclude(p => p!.Assignments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (rescueCase == null) return NotFound();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            rescueCase.Status = CaseStatus.Closed;

            if (rescueCase.ResponsePlan != null)
            {
                // Release vehicles
                var vehicleIds = rescueCase.ResponsePlan.Assignments
                    .Where(a => a.IsActive)
                    .Select(a => a.RescueVehicleId)
                    .ToList();

                if (vehicleIds.Any())
                {
                    await _context.RescueVehicles
                        .Where(v => vehicleIds.Contains(v.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(v => v.Status, VehicleStatus.Available));

                    await _context.ResponseAssignments
                        .Where(a => a.ResponsePlanId == rescueCase.ResponsePlan.Id && a.IsActive)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(a => a.IsActive, false)
                            .SetProperty(a => a.ReleasedAtUtc, DateTime.UtcNow)
                        );
                }

                // Release hospital capacity
                if (rescueCase.ResponsePlan.RequiresHospital && rescueCase.ResponsePlan.SelectedHospitalId != null)
                {
                    // Find what capacities were required. We reload facts and requirements.
                    var facts = _factsBuilder.Build(rescueCase);
                    var classification = _classifier.Classify(facts);
                    int severity = _severityAnalyzer.CalculateSeverity(facts);
                    var hospitalRequirement = _requirementEngine.GenerateHospitalRequirement(facts, classification, severity);

                    if (hospitalRequirement.Required)
                    {
                        bool hasIcu = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.ICU);
                        bool hasTrauma = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.Trauma);
                        bool hasBurn = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.BurnUnit);
                        bool hasToxicology = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.Toxicology);
                        bool hasGeneral = hospitalRequirement.RequiredCapabilities.Contains(HospitalCapability.GeneralEmergency);

                        await _context.HospitalCapacities
                            .Where(c => c.HospitalId == rescueCase.ResponsePlan.SelectedHospitalId)
                            .ExecuteUpdateAsync(setters => setters
                                .SetProperty(c => c.CurrentIcuLoad, c => hasIcu ? (c.CurrentIcuLoad - hospitalRequirement.RequiredCapacity > 0 ? c.CurrentIcuLoad - hospitalRequirement.RequiredCapacity : 0) : c.CurrentIcuLoad)
                                .SetProperty(c => c.CurrentTraumaLoad, c => hasTrauma ? (c.CurrentTraumaLoad - hospitalRequirement.RequiredCapacity > 0 ? c.CurrentTraumaLoad - hospitalRequirement.RequiredCapacity : 0) : c.CurrentTraumaLoad)
                                .SetProperty(c => c.CurrentBurnLoad, c => hasBurn ? (c.CurrentBurnLoad - hospitalRequirement.RequiredCapacity > 0 ? c.CurrentBurnLoad - hospitalRequirement.RequiredCapacity : 0) : c.CurrentBurnLoad)
                                .SetProperty(c => c.CurrentToxicologyLoad, c => hasToxicology ? (c.CurrentToxicologyLoad - hospitalRequirement.RequiredCapacity > 0 ? c.CurrentToxicologyLoad - hospitalRequirement.RequiredCapacity : 0) : c.CurrentToxicologyLoad)
                                .SetProperty(c => c.CurrentEmergencyLoad, c => hasGeneral ? (c.CurrentEmergencyLoad - hospitalRequirement.RequiredCapacity > 0 ? c.CurrentEmergencyLoad - hospitalRequirement.RequiredCapacity : 0) : c.CurrentEmergencyLoad)
                            );
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLogger.LogAuditAsync(id, "CaseClosed", "Case closed and all resources successfully released back to service.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = $"Failed to close case: {ex.Message}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
