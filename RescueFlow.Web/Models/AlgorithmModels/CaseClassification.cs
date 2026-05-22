using System.Collections.Generic;

namespace RescueFlow.Web.Models.AlgorithmModels;

public class CaseClassification
{
    public bool IsLifeThreatening { get; set; }
    public bool HasEnvironmentalHazards { get; set; }
    public bool RequiresSpecializedRescue { get; set; }
    public List<string> ClassificationTags { get; set; } = new();
}
