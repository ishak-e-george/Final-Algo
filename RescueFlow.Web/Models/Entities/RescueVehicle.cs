using System;
using System.Collections.Generic;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.Entities;

public class RescueVehicle
{
    public int Id { get; set; }

    public string Code { get; set; } = "";

    public int StationId { get; set; }
    public Station Station { get; set; } = null!;

    public VehicleType VehicleType { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    public int MaxCrewCapacity { get; set; }
    public int CurrentCrewCount { get; set; }

    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }

    public List<VehicleEquipment> VehicleEquipment { get; set; } = new();

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
