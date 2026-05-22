using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;

namespace RescueFlow.Web.Controllers;

public class StationsController : Controller
{
    private readonly RescueFlowDbContext _context;

    public StationsController(RescueFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var stations = await _context.Stations
            .Include(s => s.Vehicles)
            .ToListAsync();
        return View(stations);
    }
}
