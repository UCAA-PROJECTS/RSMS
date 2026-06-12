using Microsoft.EntityFrameworkCore;
using RSMS.Data;
using RSMS.Models;

namespace RSMS.Services
{
    public class StabilizerService : IStabilizerService
    {
        private readonly ApplicationDbContext _context;
        public StabilizerService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<StabilizerReading?> GetLatestAsync(string shelterCode)
        {
           return  await _context.StabilizerReadings
                .Where(r => r.ShelterCode == shelterCode)
                .OrderByDescending(r => r.TimeStamp)
                .FirstOrDefaultAsync();
        }

        public async Task<List<StabilizerReading>> GetRecentAsync(string shelterCode)
        {
            return await _context.StabilizerReadings
                 .Where(r => r.ShelterCode == shelterCode)
                 .OrderByDescending(r => r.TimeStamp)
                 .ToListAsync();
        }

        public async Task<List<StabilizerReading>> GetTrendAsync(string shelterCode)
        {
            var fromDate = DateTime.UtcNow.AddHours(-24);
            return await _context.StabilizerReadings
                .Where(r => r.ShelterCode == shelterCode && r.TimeStamp >= fromDate)
                .OrderByDescending(r => r.TimeStamp)
                .ToListAsync();
        }
    }
}
