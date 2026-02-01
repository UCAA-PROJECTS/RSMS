using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using System.Text;

namespace RSMS.Controllers
{
    public class LogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCSV() 
        {
            var data = await _context.Readings
                .OrderByDescending(r => r.TimeStamp)
                .ToListAsync();

            var sb = new StringBuilder();

            sb.AppendLine("ShelterCode,Temperature,Humidity,Smoke Detected,Intrusion Detected,Water Leakage Detected,TimeStamp");

            foreach (var r in data) 
            {
                sb.AppendLine($"{r.ShelterCode},{r.Temperature},{r.Humidity},{r.SmokeDetected},{r.IntrusionDetected},{r.WaterLeakDetected},{r.TimeStamp}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());

            return File(bytes, "text/csv", "sensor_logs.csv");
        }
    }
}
