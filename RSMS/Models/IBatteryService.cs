namespace RSMS.Models
{
    public interface IBatteryService
    {
        Task<BatteryReading?> GetLatestAsync(string shelterCode);
        Task<List<BatteryReading>> GetRecentAsync(string shelterCode);
        Task<List<BatteryReading>> GetTrendAsync(string shelterCode);
    }
}
