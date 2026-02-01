using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
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

        public async Task<IActionResult> Temperature(string code) 
        {
            var data = await _context.Readings
                .Where(r => r.ShelterCode == code)
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new TemperatureView
                {
                    Time = r.TimeStamp,
                    Value = r.Temperature,
                })
                .ToListAsync();

            ViewBag.ShelterCode = code;
            return View(data);

        }
    }
}
