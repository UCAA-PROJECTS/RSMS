using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.Models;

namespace RSMS.Services
{
    public class BatteryService: IBatteryService
    {
        private readonly ApplicationDbContext _context;
        public BatteryService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<BatteryReading?> GetLatestAsync(string shelterCode)
        {
            return await _context.BatteryReadings
                 .Where(r => r.ShelterCode == shelterCode)
                 .OrderByDescending(r => r.TimeStamp)
                 .FirstOrDefaultAsync();
        }

        public async Task<List<BatteryReading>> GetRecentAsync(string shelterCode)
        {
            return await _context.BatteryReadings
                 .Where(r => r.ShelterCode == shelterCode)
                 .OrderByDescending(r => r.TimeStamp)
                 .ToListAsync();
        }

        public async Task<List<BatteryReading>> GetTrendAsync(string shelterCode)
        {
            var fromDate = DateTime.UtcNow.AddHours(-24);
            return await _context.BatteryReadings
                .Where(r => r.ShelterCode == shelterCode && r.TimeStamp >= fromDate)
                .OrderByDescending(r => r.TimeStamp)
                .ToListAsync();
        }
    }
}

