using System.Collections.Generic;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.AlgorithmModels;

public class ResourceRequirement
{
    public VehicleType VehicleType { get; set; }
    public int RequiredVehicleCount { get; set; }
    public int RequiredPersonnelPerVehicle { get; set; }

    public List<string> RequiredEquipment { get; set; } = new();
}
