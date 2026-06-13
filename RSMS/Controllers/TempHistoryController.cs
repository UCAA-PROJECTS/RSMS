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

        //Make the limits mandatory to limit on the amount of data loaded into memory at a time. This is because from the associated
        //.js files, the method loadChartData(), is always passed with both start and end date.
        [HttpGet]
        public async Task<IActionResult> GetTemperatureHistory(string shelterCode, DateTime startDate, DateTime endDate) 
        {
            var data =await _context.Readings
                               .Where(reading => reading.ShelterCode == shelterCode && reading.TimeStamp >= startDate && reading.TimeStamp <= endDate)
                               .OrderByDescending(q => q.TimeStamp)
                               .Select(q => new TemperatureChartDTO
                               {
                                    Time = q.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                                    Temperature = q.Temperature
                               })
                               .ToListAsync(); 

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemperatureSummary(string shelterCode)
        {
            string status;
            //THIS IS 4 HOURS AGO, WHY NAME VARIABLE IS 6 HOURS?
            var sixHoursAgo = DateTime.UtcNow.AddHours(-1);

            var query = _context.Readings.Where(reading => reading.ShelterCode == shelterCode && reading.TimeStamp >= sixHoursAgo);
                                                
            
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

            //Time difference since last reading.
            //WHY ARE WE USING LAST READING TO CHECK SENSOR AVAILABILITY. IF SENSOR WENT OFF IN THE LAST < 5 MINUTES, WE SHALL CONTINUE TO
            //LOG SENSOR ONLINE, HOWEVER THERE WILL BE NO UPDATE ON THE LATEST SENSOR GRAPH.
            var minutesSinceLastReading = (DateTime.UtcNow - latestReading.TimeStamp).TotalMinutes;
            
            if (minutesSinceLastReading < 5)
            {
                status = "Online";
            }

            else if (minutesSinceLastReading < 15)
            {
                status = "Warning";
            }

            else
            {
                status = "Offline";
            }

            var hasData = await query.AnyAsync();

            //A SENSOR CAN HAVE DATA AND BE OFFLINE.RIGHT??
            if (!hasData)
            {
                return Json(new TemperatureSummaryDTO { SensorStatus = "Offline" });
            }

            var summary = new TemperatureSummaryDTO
            {
                AvgTemperature = Math.Round(await query.AverageAsync(r => r.Temperature), 1),
                MinTemperature = await query.MinAsync(r => r.Temperature),
                MaxTemperature = await query.MaxAsync(r => r.Temperature),
                SensorStatus = status
            };
            return Json(summary);


        }
    }
}
