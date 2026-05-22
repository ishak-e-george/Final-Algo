using System;

namespace RescueFlow.Web.Models.Entities;

public class HospitalCapacity
{
    public int Id { get; set; }

    public int HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public int EmergencyCapacity { get; set; }
    public int CurrentEmergencyLoad { get; set; }

    public int TraumaCapacity { get; set; }
    public int CurrentTraumaLoad { get; set; }

    public int IcuCapacity { get; set; }
    public int CurrentIcuLoad { get; set; }

    public int BurnCapacity { get; set; }
    public int CurrentBurnLoad { get; set; }

    public int ToxicologyCapacity { get; set; }
    public int CurrentToxicologyLoad { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
