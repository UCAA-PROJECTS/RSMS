using System.ComponentModel.DataAnnotations;

namespace RSMS.Models
{
    public class SensorReading
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string ShelterCode { get; set; }
        public Shelter Shelter { get; set; } = null!;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public bool SmokeDetected { get; set; }
        public bool IntrusionDetected { get; set; }
        public bool WaterLeakDetected { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}