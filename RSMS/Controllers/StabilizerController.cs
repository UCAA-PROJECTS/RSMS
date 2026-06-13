using Microsoft.AspNetCore.Mvc;
using RSMS.Models;
using System.Text;

namespace RSMS.Controllers
{
    public class StabilizerController : Controller
    {
        private readonly IStabilizerService _stabilizerService;
        public StabilizerController(IStabilizerService stabilizerService)
        {
            _stabilizerService = stabilizerService;
        }
        public async Task<IActionResult> Stabilizer(string? shelterCode, string? code)
        {
            var selectedShelterCode = (shelterCode ?? code)?.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(selectedShelterCode))
            {
                return BadRequest("Shelter code is required.");
            }

            var latest = await _stabilizerService.GetLatestAsync(selectedShelterCode);
            var logs = await _stabilizerService.GetRecentAsync(selectedShelterCode);
            var trend = await _stabilizerService.GetTrendAsync(selectedShelterCode);

            ViewBag.ShelterCode = selectedShelterCode;
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

            var records = await _stabilizerService.GetRecentAsync(shelterCode);
            var csv = new StringBuilder();

            csv.AppendLine("TimeStamp,Shelter Code,Input Voltage,Output Voltage,Voltage Difference,Current,Frequency,Load Percentage,Status");
            foreach(var item in records.OrderByDescending(x => x.TimeStamp)) 
            {
                var voltageDifference = item.InputVoltage - item.OutputVoltage;
                csv.AppendLine(
              $"{item.TimeStamp:yyyy-MM-dd HH:mm:ss}," +
              $"{item.ShelterCode}," +
              $"{item.InputVoltage:0.0}," +
              $"{item.OutputVoltage:0.0}," +
              $"{voltageDifference:0.0}," +
              $"{item.Current:0.00}," +
              $"{item.Frequency:0.0}," +
              $"{item.LoadPercentage:0}," +
              $"{item.Status}"
                );
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = $"stabilizer-history-{shelterCode}-{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Last24Hours(string shelterCode)
        {
            if (string.IsNullOrWhiteSpace(shelterCode))
                return BadRequest("Shelter code is required");

            var records = await _stabilizerService.GetTrendAsync(shelterCode);

            var result = records.Select(x => new
            {
                x.InputVoltage,
                x.OutputVoltage,
                x.TimeStamp,
                x.Current,
                x.Frequency,
                x.LoadPercentage,
                x.Status
            });

            return Json(result);
        }
    }
}
