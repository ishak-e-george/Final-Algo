using System;

namespace RescueFlow.Web.Models.Entities;

public class DispatchDecisionLog
{
    public int Id { get; set; }
    public int? RescueCaseId { get; set; }
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
