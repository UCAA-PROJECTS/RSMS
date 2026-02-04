using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.Models;
using RSMS.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        [HttpGet("/api/history")]
        public async Task<IActionResult> GetHistory(string code, string type)
        {
            var query = _context.Readings
                .Where(r => r.ShelterCode == code)
                .OrderByDescending(r => r.TimeStamp)
                .Take(100);

            var data = type == "temperature"
                ? await query.Select(r => new { r.TimeStamp, Value = r.Temperature }).ToListAsync()
                : await query.Select(r => new { r.TimeStamp, Value = r.Humidity }).ToListAsync();

            return Json(data);
        }


    }
}
