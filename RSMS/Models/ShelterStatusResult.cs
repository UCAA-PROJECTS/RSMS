namespace RSMS.Models
{
    public class ShelterStatusResult
    {
        public ShelterStatus OverallStatus { get; set; }
        public ShelterStatus TemperatureStatus { get; set; }
        public ShelterStatus HumidityStatus { get; set; }
        public ShelterStatus smokeStatus { get; set; }
        public ShelterStatus intrudeStatus { get; set; }
    }
}
