using System.ComponentModel.DataAnnotations;

namespace RSMS.Models
{
    public class SensorReading
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string ShelterCode { get; set; } = string.Empty;
        public Shelter Shelter { get; set; } = null!;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public bool SmokeDetected { get; set; }
        public bool IntrusionDetected { get; set; }
        //public bool WaterLeakDetected { get; set; }
        
        //removed the timestamp default value = DateTime.Now(), this is because we want to record the time when data was sent 
        //from the sensor not the time when data was saved into the database.
        public DateTime TimeStamp { get; set; }
    }
}