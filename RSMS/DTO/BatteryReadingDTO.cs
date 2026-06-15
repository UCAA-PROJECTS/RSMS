namespace RSMS.DTO
{
    public class BatteryReadingDTO
    {

        public string ShelterCode { get; set; } = null!;
        public double Voltage { get; set; }
        public double Current { get; set; }
        public double StateOfCharge { get; set; }
        public double Temperature { get; set; }
        public double BackupHoursRemaining { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
