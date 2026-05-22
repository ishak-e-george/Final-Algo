using System.Collections.Generic;

namespace RescueFlow.Web.Models.Entities;

public class Equipment
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public List<VehicleEquipment> VehicleEquipment { get; set; } = new();
}
