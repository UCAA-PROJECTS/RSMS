using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class HumHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HumHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Humidity(string? shelterCode, string? code)
        {
            var selectedShelterCode = (shelterCode ?? code)?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(selectedShelterCode))
            {
                return BadRequest("Shelter code is required.");
            }

            var data = await _context.Readings
                .Where(r => r.ShelterCode == selectedShelterCode)
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new HumidityView
                {
                    Time = r.TimeStamp,
                    Value = r.Humidity,
                })
                .ToListAsync();

            ViewBag.ShelterCode = selectedShelterCode;
            return View(data);

        }

        [HttpGet]
        public async Task<IActionResult> GetHumidityHistory(string shelterCode, DateTime? startDate, DateTime? endDate)
        {
            var data = _context.Readings
                .Where(r => r.ShelterCode == shelterCode);

            //Applying the date filtering
            if (startDate.HasValue)
            {
                data = data.Where(r => r.TimeStamp >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                data = data.Where(r => r.TimeStamp <= endDate.Value);
            }

            var query = await data
                .OrderByDescending(q => q.TimeStamp)
                .Select(q => new HumidityChartDTO
                {
                    Time = q.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                    Humidity = q.Humidity
                })
                .ToListAsync();
            return Json(query);
        }

        [HttpGet]
        public async Task<IActionResult> GetHumiditySummary(string shelterCode)
        {
            string Status;
            var sixHoursAgo = DateTime.UtcNow.AddHours(-1); // Checking for readings in the last 1 hour to determine sensor status

            var query = _context.Readings.Where(x => x.ShelterCode == shelterCode &&
            x.TimeStamp >= sixHoursAgo);

            var latestReading = await query.OrderByDescending(r => r.TimeStamp).FirstOrDefaultAsync();
            if (latestReading == null)
            {
                return Json(new HumiditySummaryDTO
                {
                    AvgHumidity = 0,
                    MinHumidity = 0,
                    MaxHumidity = 0,
                    SensorStatus = "Offline"

                });
            }

            //Time difference since last reading
            var minutesSinceLastReading = (DateTime.UtcNow - latestReading.TimeStamp).TotalMinutes;

            if (minutesSinceLastReading < 5)
            {
                Status = "Online";
            }

            else if (minutesSinceLastReading < 15)
            {
                Status = "Warning";
            }

            else
            {
                Status = "Offline";
            }
            var hasData = await query.AnyAsync();

            if (!hasData)
            {
                return Json(new HumiditySummaryDTO { SensorStatus = "Offline" });
            }

            var summary = new HumiditySummaryDTO
            {
                AvgHumidity = Math.Round(await query.AverageAsync(r => r.Humidity), 1),
                MinHumidity = await query.MinAsync(r => r.Humidity),
                MaxHumidity = await query.MaxAsync(r => r.Humidity),
                SensorStatus = Status
            };
            return Json(summary);


        }
    }
}
