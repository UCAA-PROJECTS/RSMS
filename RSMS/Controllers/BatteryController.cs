using Microsoft.AspNetCore.Mvc;
using RSMS.Models;
using System.Text;

namespace RSMS.Controllers
{
    public class BatteryController : Controller
    {
        private readonly IBatteryService _batteryService;
        public BatteryController(IBatteryService batteryService)
        {
            _batteryService = batteryService;
        }
        public async Task<IActionResult> Battery(string? shelterCode)
        {

            if (string.IsNullOrWhiteSpace(shelterCode))
            {
                return BadRequest("Shelter code is required.");
            }

            var latest = await _batteryService.GetLatestAsync(shelterCode);
            var logs = await _batteryService.GetRecentAsync(shelterCode);
            var trend = await _batteryService.GetTrendAsync(shelterCode);

            ViewBag.ShelterCode = shelterCode;
            ViewBag.Latest = latest;
            ViewBag.Logs = logs;
            ViewBag.Trend = trend;
            return View();
        }

        public async Task<IActionResult> Download(string shelterCode)
        {
            if (string.IsNullOrWhiteSpace(shelterCode))
            {
                return BadRequest("Shelter code is required.");
            }

            var records = await _batteryService.GetRecentAsync(shelterCode);
            var csv = new StringBuilder();

            csv.AppendLine("TimeStamp,Shelter Code,Voltage,Current,State of Charge,Temperature,BackUpHoursRemaining,Status");
            foreach (var item in records.OrderByDescending(x => x.TimeStamp))
            {
                csv.AppendLine(
              $"{item.TimeStamp:yyyy-MM-dd HH:mm:ss}," +
              $"{item.ShelterCode}," +
              $"{item.Voltage:0.0}," +
              $"{item.Current:0.0}," +
              $"{item.StateOfCharge:0.00}," +
              $"{item.Temperature:0.0}," +
              $"{item.BackupHoursRemaining:0.0}," +
              $"{item.Status}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"battery-history-{shelterCode}-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Last24Hours(string shelterCode)
        {
            if (string.IsNullOrWhiteSpace(shelterCode))
                return BadRequest("Shelter code is required");

            var records = await _batteryService.GetTrendAsync(shelterCode);

            var result = records.Select(x => new
            {
                x.Voltage,
                x.StateOfCharge,
                x.TimeStamp,
                x.Current,
                x.Temperature,
                x.BackupHoursRemaining,
                x.Status
            });

            return Json(result);
        }
    }
}

