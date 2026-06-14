using System.ComponentModel.DataAnnotations;

namespace RSMS.DTO
{
    public class SensorInputDTO
    {
        public DateTime TimeStamp { get; set; }
        public string ShelterCode { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public bool SmokeDetected { get; set; }
        public bool IntrusionDetected { get; set; }

    }
}
