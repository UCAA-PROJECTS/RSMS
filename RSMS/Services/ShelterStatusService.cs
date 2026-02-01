using RSMS.Models;

namespace RSMS.Services
{
    public class ShelterStatusService : IShelterStatusService
    {
        public ShelterStatus Evaluate(SensorReading read)
        {
            if (read.SmokeDetected || read.IntrusionDetected || read.Temperature > 40)
            {
                return ShelterStatus.Alert;
            }

            else if (read.Temperature > 35 || read.Humidity > 70 || read.WaterLeakDetected)
            {
                return ShelterStatus.Warning;
            }


            else
            {
                return ShelterStatus.Ok;
            }

        }
    }
}
