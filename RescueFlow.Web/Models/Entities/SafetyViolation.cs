using System;

namespace RescueFlow.Web.Models.Entities;

public class SafetyViolation
{
    public int Id { get; set; }
    public int RescueCaseId { get; set; }
    public string RuleName { get; set; } = "";
    public string ViolationDetails { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
