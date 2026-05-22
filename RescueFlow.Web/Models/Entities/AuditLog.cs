using System;

namespace RescueFlow.Web.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int? RescueCaseId { get; set; }
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
