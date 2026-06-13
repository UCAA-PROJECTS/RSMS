namespace RSMS.Models
{
    public interface IStabilizerService
    {
        Task<StabilizerReading?> GetLatestAsync(string shelterCode);
        Task<List<StabilizerReading>> GetRecentAsync(string shelterCode);
        Task<List<StabilizerReading>> GetTrendAsync(string shelterCode);
    }
}
