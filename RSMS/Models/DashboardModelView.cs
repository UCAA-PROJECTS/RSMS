
namespace RSMS.Models
{
    public class DashboardModelView
    {
        public string ShelterCode { get; set; } = null!;
        public string ShelterName { get; set; } = null!;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public ShelterStatus Status { get; set; }

        // added ShelterAccess to the model
        public bool ShelterAccess { get; set; }

        // added battery
        public double Battery { get; set; }

        // added stabilizer
        public double Stabilizer { get; set; }

        // added settings
        public double Settings { get; set; }

        // public bool SmokeDetected { get; set; }
        // public bool IntrusionDetected { get; set; }
        //public bool WaterLeakDetected { get; set; }
      
    }
}
