namespace RSMS.Models
{
    public class ShelterHistoryView
    {
        public string ShelterCode { get; set; } = string.Empty;
        public List<TemperatureView> TemperatureHistory { get; set; } = new();
    }
}
