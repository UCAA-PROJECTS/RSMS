namespace RSMS.DTO
{
    public class StabilizerReadingDTO
    {
        public string ShelterCode { get; set; } = null!;
        public double InputVoltage { get; set; }
        public double OutputVoltage { get; set; }
        public double Current { get; set; }
        public double Frequency { get; set; }
        public double LoadPercentage { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
