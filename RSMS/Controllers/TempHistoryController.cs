using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class TempHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TempHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Temperature(string? shelterCode, string? code)
        {
            var selectedShelterCode = (shelterCode ?? code)?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(selectedShelterCode))
            {
                return BadRequest("Shelter code is required.");
            }

            var data = await _context.Readings
                .Where(r => r.ShelterCode == selectedShelterCode)
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new TemperatureView
                {
                    Time = r.TimeStamp,
                    Value = r.Temperature,
                })
                .ToListAsync();

            ViewBag.ShelterCode = selectedShelterCode;
            return View(data);

        }

        [HttpGet]
        public async Task<IActionResult> GetTemperatureHistory(string shelterCode, DateTime? startDate, DateTime ?endDate) 
        {
            var data = _context.Readings
                .Where(r => r.ShelterCode == shelterCode);
                
            //Applying the date filtering
            if(startDate.HasValue)
            {
                data = data.Where(r => r.TimeStamp >= startDate.Value);
            }
            if (endDate.HasValue) 
            {
                data = data.Where(r => r.TimeStamp <= endDate.Value);
            }

            var query = await data
                .OrderByDescending(q => q.TimeStamp)
                .Select(q => new TemperatureChartDTO
                {
                    Time = q.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                    Temperature = q.Temperature
                })
                .ToListAsync();
            return Json(query);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemperatureSummary(string shelterCode)
        {
            string Status;
            var sixHoursAgo = DateTime.UtcNow.AddHours(-1);

            var query = _context.Readings.Where(x => x.ShelterCode == shelterCode &&
            x.TimeStamp >= sixHoursAgo);

            var latestReading = await query.OrderByDescending(r => r.TimeStamp).FirstOrDefaultAsync();
            if (latestReading == null)
            {
                return Json(new TemperatureSummaryDTO
                {
                    AvgTemperature = 0,
                    MinTemperature = 0,
                    MaxTemperature = 0,
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
                return Json(new TemperatureSummaryDTO { SensorStatus = "Offline" });
            }

            var summary = new TemperatureSummaryDTO
            {
                AvgTemperature = Math.Round(await query.AverageAsync(r => r.Temperature), 1),
                MinTemperature = await query.MinAsync(r => r.Temperature),
                MaxTemperature = await query.MaxAsync(r => r.Temperature),
                SensorStatus = Status
            };
            return Json(summary);


        }
    }
}
