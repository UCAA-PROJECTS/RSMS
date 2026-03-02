using System.ComponentModel.DataAnnotations;

namespace RSMS.DTO
{
    public class SensorInputDTO
    {
        public string ShelterCode { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public bool SmokeDetected { get; set; }
        public bool IntrusionDetected { get; set; }

    }
}
