using RSMS.Models;

namespace RSMS.Services
{
    public class ShelterStatusService: IShelterService
    {
        public ShelterStatusResult Evaluate(SensorReading read)
        {
            var result = new ShelterStatusResult
            {

                TemperatureStatus = EvaluateTemperature(read.Temperature),
                HumidityStatus = EvaluateHumidity(read.Humidity),
                smokeStatus = read.SmokeDetected? ShelterStatus.Alert : ShelterStatus.Ok,
                intrudeStatus = read.IntrusionDetected ? ShelterStatus.Alert : ShelterStatus.Ok

            };

            result.OverallStatus = DetermineOverallStatus(result);
            return result;
        }

        private ShelterStatus EvaluateTemperature(double temp) 
        {
            if (temp > 40)
                return ShelterStatus.Alert;
            if (temp > 30)
                return ShelterStatus.Warning;

            return ShelterStatus.Ok;
        }

        private ShelterStatus EvaluateHumidity(double humidity) 
        {
            if (humidity > 80)
                return ShelterStatus.Alert;

            if (humidity > 60)
                return ShelterStatus.Warning;

            return ShelterStatus.Ok;
        }

        private ShelterStatus DetermineOverallStatus(ShelterStatusResult result) 
        {
            if(result.TemperatureStatus == ShelterStatus.Alert ||
                result.HumidityStatus == ShelterStatus.Alert ||
                result.smokeStatus == ShelterStatus.Alert ||
                result.intrudeStatus == ShelterStatus.Alert)
            {
                return ShelterStatus.Alert;
            }
            if (result.TemperatureStatus == ShelterStatus.Warning ||
                result.HumidityStatus == ShelterStatus.Warning)
            {
                return ShelterStatus.Warning;
            }
            return ShelterStatus.Ok;
        }
 
    }
}
