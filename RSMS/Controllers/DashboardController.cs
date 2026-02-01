using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.Models;
using RSMS.Services;

namespace RSMS.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IShelterStatusService _statusService;

        public DashboardController(ApplicationDbContext context, IShelterStatusService statusService)
        {
            _context = context;
            _statusService = statusService;
        }
        public async Task<IActionResult> Index()
        {
            var shelters = await _context.Shelters
                .Include(s => s.SensorReadings)
                .ToListAsync();

            var model = shelters.Select(p =>
            {
                var latest = p.SensorReadings
                .OrderByDescending(r => r.TimeStamp)
                .FirstOrDefault();

                return new DashboardModelView
                {
                    ShelterCode = p.ShelterCode,
                    ShelterName = p.ShelterName,
                    Temperature = latest?.Temperature ?? 0,
                    Humidity = latest?.Humidity ?? 0,
                    Status = latest == null
                        ? ShelterStatus.Ok
                        : _statusService.Evaluate(latest),
                    SmokeDetected = latest?.SmokeDetected ?? false,
                    IntrusionDetected = latest?.IntrusionDetected ?? false,
                    WaterLeakDetected = latest?.WaterLeakDetected ?? false
                };
            }).ToList();
            return View(model);
        }
    }
}
