using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Models;

namespace RSMS.Controllers
{
    public class ShelterHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShelterHistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> GetShelterAccessHistory(string code)
        {
            var data = await _context.Readings
                                    .Where(reading => reading.ShelterCode == code)
                                    .OrderByDescending(reading => reading.TimeStamp)
                                    .Select(reading => new {
                                        Time = reading.TimeStamp,
                                        Value = reading.IntrusionDetected
                                    })
                                    .ToListAsync();

            ViewBag.ShelterCode = code;

            //WHY SEND FULL SENSOR DATA  WHEN WE DONT ACTUALLY USE IT. THE USED DATA IS THE ONE FROM THE MOSQUITTO CLIENT
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetShelterDataHistory(string shelterCode, DateTime startDate, DateTime endDate)
        {
            var data = await _context.Readings
                               .Where(reading => reading.ShelterCode == shelterCode && reading.TimeStamp >= startDate && reading.TimeStamp <= endDate)
                               .OrderByDescending(reading => reading.TimeStamp)
                               .Select(reading => new AccessShelterChartDTO
                               {
                                   Time = reading.TimeStamp.ToString("yyyy-MM-dd HH:mm"),
                                   Value = reading.SmokeDetected
                               })
                               .ToListAsync();

            return Json(data);
        }


    }
}
