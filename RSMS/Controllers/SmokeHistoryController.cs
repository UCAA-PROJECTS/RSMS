using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class SmokeHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SmokeHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetSmokeData(string code)
        {
            var data = await _context.Readings
                                    .Where(reading => reading.ShelterCode == code)
                                    .OrderByDescending(reading => reading.TimeStamp)
                                    .Select(reading => new {
                                        Time = reading.TimeStamp,
                                        Value = reading.SmokeDetected
                                    })
                                    .ToListAsync();

            ViewBag.ShelterCode = code;

            //WHY SEND FULL SENSOR DATA  WHEN WE DONT ACTUALLY USE IT. THE USED DATA IS THE ONE FROM THE MOSQUITTO CLIENT
            return View(data);   
        }

        [HttpGet]
        public async Task<IActionResult> GetSmokeDataHistory(string shelterCode, DateTime startDate, DateTime endDate)
        {
            var data = await _context.Readings
                               .Where(reading => reading.ShelterCode == shelterCode && reading.TimeStamp >= startDate && reading.TimeStamp <= endDate)
                               .OrderByDescending(reading => reading.TimeStamp)
                               .Select(reading => new SmokeChartDTO
                               {
                                   Time = reading.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                                   Result = reading.SmokeDetected
                               })
                               .ToListAsync();

            return Json(data);
        }

       
    }
}
