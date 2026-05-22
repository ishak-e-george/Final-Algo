using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;
using RescueFlow.Web.Models.Entities;
using RescueFlow.Web.Models.Enums;

namespace RescueFlow.Web.Controllers;

public class VehiclesController : Controller
{
    private readonly RescueFlowDbContext _context;

    public VehiclesController(RescueFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var vehicles = await _context.RescueVehicles
            .Include(v => v.Station)
            .Include(v => v.VehicleEquipment)
                .ThenInclude(ve => ve.Equipment)
            .OrderBy(v => v.Code)
            .ToListAsync();

        return View(vehicles);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, VehicleStatus status)
    {
        var vehicle = await _context.RescueVehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        vehicle.Status = status;
        await _context.SaveChangesAsync();

        TempData["Message"] = $"Vehicle {vehicle.Code} status updated to {status}.";
        return RedirectToAction(nameof(Index));
    }
}
