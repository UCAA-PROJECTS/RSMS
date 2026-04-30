namespace RSMS.Models
{
    public class ShelterStatusResult
    {
        public ShelterStatus OverallStatus { get; set; }
        public ShelterStatus TemperatureStatus { get; set; }
        public ShelterStatus HumidityStatus { get; set; }

        // added battery, shelter access and stabilizer
        public ShelterStatus BatteryStatus { get; set; }
        public ShelterStatus ShelterAccess { get; set; }
        public ShelterStatus StabilizerStatus { get; set; }

        public ShelterStatus smokeStatus { get; set; }
        public ShelterStatus intrudeStatus { get; set; }
    }
}
