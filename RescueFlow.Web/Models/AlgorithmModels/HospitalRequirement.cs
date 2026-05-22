using System.Collections.Generic;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.AlgorithmModels;

public class HospitalRequirement
{
    public bool Required { get; set; }
    public List<HospitalCapability> RequiredCapabilities { get; set; } = new();
    public int RequiredCapacity { get; set; }
}
