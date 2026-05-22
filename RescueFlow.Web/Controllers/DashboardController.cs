using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Controllers;

public class DashboardController : Controller
{
    private readonly RescueFlowDbContext _context;

    public DashboardController(RescueFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // 1. Case Stats
        var cases = await _context.RescueCases.ToListAsync();
        ViewBag.TotalCases = cases.Count;
        ViewBag.PendingCases = cases.Count(c => c.Status == CaseStatus.Pending);
        ViewBag.AutoAssigned = cases.Count(c => c.Status == CaseStatus.AutoAssigned);
        ViewBag.BlockedNoSafePlan = cases.Count(c => c.Status == CaseStatus.BlockedNoSafePlan);
        ViewBag.Escalated = cases.Count(c => c.Status == CaseStatus.Escalated);

        // 2. Vehicle Stats
        var vehicles = await _context.RescueVehicles.ToListAsync();
        ViewBag.TotalVehicles = vehicles.Count;
        ViewBag.AvailableVehicles = vehicles.Count(v => v.Status == VehicleStatus.Available);
        ViewBag.AssignedVehicles = vehicles.Count(v => v.Status == VehicleStatus.Assigned);
        ViewBag.MaintenanceVehicles = vehicles.Count(v => v.Status == VehicleStatus.Maintenance);

        // 3. Hospital capacity load averages
        var capacities = await _context.HospitalCapacities.Include(c => c.Hospital).ToListAsync();
        ViewBag.Hospitals = capacities;

        // 4. Latest cases
        var latestCases = await _context.RescueCases
            .OrderByDescending(c => c.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View(latestCases);
    }
}
