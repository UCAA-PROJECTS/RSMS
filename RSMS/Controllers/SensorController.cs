using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.DTO;
using RSMS.Hubs;
using RSMS.Models;
using RSMS.Services;

namespace RSMS.Controllers
{
    [ApiController]
    [Route("api/sensors")]
    public class SensorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IShelterStatusService _shelterStatusService;
        private readonly IHubContext<ShelterHub> _hub;

        public SensorController(ApplicationDbContext context, IShelterStatusService shelterStatusService, IHubContext<ShelterHub> hub)
        {
            _context = context;
            _shelterStatusService = shelterStatusService;
            _hub = hub;

        }

        [HttpPost]
        public async Task<IActionResult> PostReading([FromBody] SensorInputDTO dto)
        {
            var shelter = await _context.Shelters.
                FirstOrDefaultAsync(s => s.ShelterCode == dto.ShelterCode);

            if (shelter == null)
            {
                return BadRequest("Invalid Shelter Code");
            }

            var reading = new SensorReading
            {
                ShelterCode = shelter.ShelterCode,
                Temperature = dto.Temperature,
                Humidity = dto.Humidity,
                SmokeDetected = dto.SmokeDetected,
                IntrusionDetected = dto.IntrusionDetected,
                WaterLeakDetected = dto.WaterLeakDetected,

            };
            _context.Readings.Add(reading);
            await _context.SaveChangesAsync();
            Console.WriteLine("Sensor reading saved!");

            var status = _shelterStatusService.Evaluate(reading);

            await _hub.Clients.All.SendAsync("ShelterUpdated", new
            {
                shelter.ShelterCode,
                shelter.ShelterName,
                reading.Temperature,
                reading.Humidity,
                reading.SmokeDetected,
                reading.IntrusionDetected,
                reading.WaterLeakDetected,
                status = status.ToString()


            });
            return Ok();


        }
    }
}
