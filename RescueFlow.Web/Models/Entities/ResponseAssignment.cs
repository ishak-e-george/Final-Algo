using System;

namespace RescueFlow.Web.Models.Entities;

public class ResponseAssignment
{
    public int Id { get; set; }

    public int ResponsePlanId { get; set; }
    public ResponsePlan ResponsePlan { get; set; } = null!;

    public int RescueVehicleId { get; set; }
    public RescueVehicle RescueVehicle { get; set; } = null!;

    public double EtaMinutes { get; set; }

    // Active assignment protection fields
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReleasedAtUtc { get; set; }
}
