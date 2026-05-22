using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RescueFlow.Web.Data;

namespace RescueFlow.Web.Controllers;

public class HospitalsController : Controller
{
    private readonly RescueFlowDbContext _context;

    public HospitalsController(RescueFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hospitals = await _context.Hospitals
            .Include(h => h.Capabilities)
            .Include(h => h.Capacity)
            .OrderBy(h => h.Name)
            .ToListAsync();

        return View(hospitals);
    }
}
