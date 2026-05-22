using System.Collections.Generic;

namespace RescueFlow.Web.Models.Entities;

public class Station
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool IsActive { get; set; } = true;

    public List<RescueVehicle> Vehicles { get; set; } = new();
}
