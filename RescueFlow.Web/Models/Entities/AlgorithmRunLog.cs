using System;

namespace RescueFlow.Web.Models.Entities;

public class AlgorithmRunLog
{
    public int Id { get; set; }
    public int RescueCaseId { get; set; }
    public string AlgorithmVersion { get; set; } = "SC-RCMDA-v1.0";
    public int SeverityScore { get; set; }
    public string RequiredVehiclesJson { get; set; } = "[]";
    public string SelectedVehiclesJson { get; set; } = "[]";
    public string RejectedVehiclesJson { get; set; } = "[]";
    public int? SelectedHospitalId { get; set; }
    public string RejectedHospitalsJson { get; set; } = "[]";
    public bool ValidationPassed { get; set; }
    public string ValidationErrors { get; set; } = "";
    public string ExecutionTrace { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
