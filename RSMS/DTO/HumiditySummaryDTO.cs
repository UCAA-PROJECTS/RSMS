namespace RSMS.DTO
{
    public class HumiditySummaryDTO
    {
        public double AvgHumidity { get; set; }
        public double MinHumidity { get; set; }
        public double MaxHumidity { get; set; }
        public string SensorStatus { get; set; } = string.Empty;
    }
}
