namespace RSMS.DTO
{
    public class SensorDataSummaryDTO
    {
        public double AvgResult { get; set; }
        public double MinResult { get; set; }
        public double MaxResult { get; set; }
        public string SensorStatus { get; set; } = string.Empty;
    }
}
