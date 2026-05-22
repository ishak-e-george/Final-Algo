namespace RescueFlow.Web.Models.Entities;

public class VehicleEquipment
{
    public int RescueVehicleId { get; set; }
    public RescueVehicle RescueVehicle { get; set; } = null!;

    public int EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
}
