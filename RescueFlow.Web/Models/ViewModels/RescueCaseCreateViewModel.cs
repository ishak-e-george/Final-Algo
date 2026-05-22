using System.ComponentModel.DataAnnotations;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Models.ViewModels;

public class RescueCaseCreateViewModel
{
    [Required(ErrorMessage = "Reporter Name is required")]
    [StringLength(100, ErrorMessage = "Reporter Name cannot be longer than 100 characters")]
    public string ReporterName { get; set; } = "";

    [Required(ErrorMessage = "Reporter Phone is required")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string ReporterPhone { get; set; } = "";

    [Required(ErrorMessage = "Location description is required")]
    public string LocationDescription { get; set; } = "";

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Incident Category is required")]
    public IncidentCategory IncidentCategory { get; set; }

    [Required(ErrorMessage = "Injury Type is required")]
    public InjuryType InjuryType { get; set; }

    [Required(ErrorMessage = "Number of Patients is required")]
    public int NumberOfPatients { get; set; } = 1;

    public bool HasSevereBleeding { get; set; }
    public bool HasBreathingProblem { get; set; }
    public bool IsUnconscious { get; set; }
    public bool HasFire { get; set; }
    public bool HasTrappedVictim { get; set; }
    public bool HasChemicalRisk { get; set; }
    public bool HasExplosionRisk { get; set; }

    public string Description { get; set; } = "";
}
