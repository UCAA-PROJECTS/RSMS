using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.Services;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IShelterService _statusService;

        public DashboardController(
            ApplicationDbContext context,
            IShelterService statusService)
        {
            _context = context;
            _statusService = statusService;
        }

        public async Task<IActionResult> Index()
        {
            // Load shelters with latest reading only (optimized query)
            var shelters = await _context.Shelters
                .Select(s => new
                {
                    s.ShelterCode,
                    s.ShelterName,
                    LatestReading = s.SensorReadings
                        .OrderByDescending(r => r.TimeStamp)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var model = shelters.Select(s =>
            {
                var latest = s.LatestReading;

                return new DashboardModelView
                {
                    ShelterCode = s.ShelterCode,
                    ShelterName = s.ShelterName,
                    Temperature = latest?.Temperature ?? 0,
                    Humidity = latest?.Humidity ?? 0,
                    // SmokeDetected = latest?.SmokeDetected ?? false,
                    ShelterAccess = latest?.ShelterAccess ?? false,
                    Battery = latest?.Battery ?? 0,
                    Stabilizer = latest?.Stabilizer ?? 0,
                    // Settings = latest?.Settings ?? 0,


                    // added ShelterAccess to the model and set it to false if no reading is available
                    // ShelterAccess = latest?.ShelterAccess ?? false,

                    //WaterLeakDetected = latest?.WaterLeakDetected ?? false,
                    Status = latest == null
                        ? ShelterStatus.Ok
                        : _statusService.Evaluate(latest).OverallStatus
                };
            }).ToList();

            return View(model);
        }

        [HttpGet("/api/history")]
        public async Task<IActionResult> GetHistory(string code, string type)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Shelter code required.");

            var query = _context.Readings
                .Where(r => r.ShelterCode == code)
                .OrderByDescending(r => r.TimeStamp)
                .Take(100);

            List<object> data;

            switch (type.ToLower())
            {
                case "temperature":
                    data = await query.Select(r => new
                    {
                        r.TimeStamp,
                        Value = r.Temperature
                    }).ToListAsync<object>();
                    break;

                case "humidity":
                    data = await query.Select(r => new
                    {
                        r.TimeStamp,
                        Value = r.Humidity
                    }).ToListAsync<object>();
                    break;

                case "battery":
                    data = await query.Select(r => new
                    {
                        r.TimeStamp,
                        Value = r.Battery
                    }).ToListAsync<object>();
                    break;

                case "shelteraccess":
                    data = await query.Select(r => new
                    {
                        r.TimeStamp,
                        Value = r.ShelterAccess
                    }).ToListAsync<object>();
                    break;

                case "stabilizer":
                    data = await query.Select(r => new
                    {
                        r.TimeStamp,
                        Value = r.Stabilizer
                    }).ToListAsync<object>();
                    break;

                // case "smoke":
                //     data = await query.Select(r => new
                //     {
                //         r.TimeStamp,
                //         Value = r.SmokeDetected
                //     }).ToListAsync<object>();
                //     break;

                default:
                    return BadRequest("Invalid type");
            }

            return Json(data);
                    }
    }
}