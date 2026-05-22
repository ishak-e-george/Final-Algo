using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.Entities;

public class HospitalCapabilityEntity
{
    public int Id { get; set; }

    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public HospitalCapability Capability { get; set; }
}
