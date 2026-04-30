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
// for temperature view
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


        // for humidity view
        public async Task<IActionResult> Humidity(string code)
        {
            var data = await _context.Readings
                .Where(r => r.ShelterCode == code)
                .OrderByDescending(r => r.TimeStamp)
                .Select(r => new HumidityView
                {
                    Time = r.TimeStamp,
                    Value = r.Humidity,
                })
                .ToListAsync();
            return View(data);
        }

        // for shelter access view
        public async Task<IActionResult> ShelterAccess(string code)
        {
            var data = await _context.Readings
                .Where(sa => sa.ShelterCode == code)
                .OrderByDescending(sa => sa.TimeStamp)
                 .Select(r => new ShelterAccessView
                {
                    Time = r.TimeStamp,
                    Value = r.ShelterAccess,
                })
                .ToListAsync();

            ViewBag.ShelterCode = code;
            return View(data);
    }

    // for stabilizer view
    public async Task<IActionResult> Stabilizer(string code)
        {
            var data = await _context.Readings
                .Where(sa => sa.ShelterCode == code)
                .OrderByDescending(sa => sa.TimeStamp)
                 .Select(r => new StabilizerView
                {
                    Time = r.TimeStamp,
                    Value = r.Humidity,
                })
                .ToListAsync();

            ViewBag.ShelterCode = code;
            return View(data);
    }

    // for battery view
    public async Task<IActionResult> Battery(string code)
        {
            var data = await _context.Readings
                .Where(sa => sa.ShelterCode == code)
                .OrderByDescending(sa => sa.TimeStamp)
                 .Select(r => new BatteryView
                {
                    Time = r.TimeStamp,
                    Value = r.Humidity,
                })
                .ToListAsync();

            ViewBag.ShelterCode = code;
            return View(data);
    }
    

    // for settings view
    public async Task<IActionResult> Settings(string code)
        {
            var data = await _context.Readings
                .Where(sa => sa.ShelterCode == code)
                .OrderByDescending(sa => sa.TimeStamp)
                 .Select(r => new SettingsView
                {
                    Time = r.TimeStamp,
                    Value = r.Humidity,
                })
                .ToListAsync();

            ViewBag.ShelterCode = code;
            return View(data);
    }
    }
}
