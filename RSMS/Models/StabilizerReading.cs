namespace RSMS.Models
{
    public class StabilizerReading
    {
        public int Id { get; set; }
        public string ShelterCode { get; set; } = null!;
        public double InputVoltage { get; set; }
        public double Current { get; set; }
        public double OutputVoltage { get; set; }
        public double Frequency { get; set; }
        public double LoadPercentage { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = string.Empty;
    }
}
