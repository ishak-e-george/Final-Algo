using System;

namespace RescueFlow.Web.Models.Entities;

public class SafetyValidationResult
{
    public int Id { get; set; }
    public int RescueCaseId { get; set; }
    public bool ValidationPassed { get; set; }
    public string Message { get; set; } = "";
    public string CheckedRulesJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
