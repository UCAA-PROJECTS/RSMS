using System.ComponentModel.DataAnnotations;

namespace RSMS.DTO
{
    public class SensorInputDTO
    {
        public required string ShelterCode { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }

        // added battery, stabilizer and shelter access
        public double Battery { get; set; }
        public double Stabilizer { get; set; }
        public bool ShelterAccess { get; set; }
    
        public bool SmokeDetected { get; set; }
        public bool IntrusionDetected { get; set; }

    }
}
