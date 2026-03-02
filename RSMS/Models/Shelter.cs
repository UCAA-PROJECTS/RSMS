using System.ComponentModel.DataAnnotations;

namespace RSMS.Models
{
    public class Shelter
    {
        [Key]
        [MaxLength(20)]
        public string ShelterCode { get; set; } = null!;
        public string ShelterName { get; set; } = null!;
        public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
    }
}
