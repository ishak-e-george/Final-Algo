using System;
using System.Collections.Generic;

namespace RescueFlow.Web.Models.Entities;

public class ResponsePlan
{
    public int Id { get; set; }

    public int RescueCaseId { get; set; }
    public RescueCase RescueCase { get; set; } = null!;

    public int? SelectedHospitalId { get; set; }
    public Hospital? SelectedHospital { get; set; }

    public int SeverityLevel { get; set; }

    public bool RequiresHospital { get; set; }

    public bool SafetyValidationPassed { get; set; }
    public string ValidationMessage { get; set; } = "";

    public double EstimatedSceneEtaMinutes { get; set; }
    public double? EstimatedHospitalEtaMinutes { get; set; }

    // Safety Auditing and Explanation variables
    public string AlgorithmVersion { get; set; } = "SC-RCMDA-v1.0";
    
    // JSON payloads representing details
    public string RequiredVehiclesJson { get; set; } = "[]";
    public string SelectedVehiclesJson { get; set; } = "[]";
    public string RejectedVehiclesJson { get; set; } = "[]";
    public string RejectedHospitalsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ResponseAssignment> Assignments { get; set; } = new();
}
