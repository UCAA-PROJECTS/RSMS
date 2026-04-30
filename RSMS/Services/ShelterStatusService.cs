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
                BatteryStatus = EvaluateBattery(read.Battery),
                StabilizerStatus = EvaluateStabilizer(read.Stabilizer),
                ShelterAccess = read.ShelterAccess? ShelterStatus.Alert : ShelterStatus.Ok,
                smokeStatus = read.SmokeDetected? ShelterStatus.Alert : ShelterStatus.Ok
                // intrudeStatus = read.IntrusionDetected ? ShelterStatus.Alert : ShelterStatus.Ok

            };

            result.OverallStatus = DetermineOverallStatus(result);
            return result;
        }

// evaluate temperature statuse
        private ShelterStatus EvaluateTemperature(double temp) 
        {
            if (temp > 40)
                return ShelterStatus.Alert;
            if (temp > 30)
                return ShelterStatus.Warning;

            return ShelterStatus.Ok;
        }
// evaluate humidity status
        private ShelterStatus EvaluateHumidity(double humidity) 
        {
            if (humidity > 80)
                return ShelterStatus.Alert;

            if (humidity > 60)
                return ShelterStatus.Warning;

            return ShelterStatus.Ok;
        }

        // evaluate Battery status
        private ShelterStatus EvaluateBattery(double battery) 
        {
            if (battery > 80)
                return ShelterStatus.Ok;

            if (battery > 60)
                return ShelterStatus.Warning;

            return ShelterStatus.Alert;
        }

        // evaluate stabilizer
        private ShelterStatus EvaluateStabilizer(double stabilizer) 
        {
            if (stabilizer > 80)
                return ShelterStatus.Ok;

            if (stabilizer > 60)
                return ShelterStatus.Warning;

            return ShelterStatus.Alert;
        }

        // overall shelter status
        private ShelterStatus DetermineOverallStatus(ShelterStatusResult result) 
        {
            if(result.TemperatureStatus == ShelterStatus.Alert ||
                result.HumidityStatus == ShelterStatus.Alert ||
                result.smokeStatus == ShelterStatus.Alert ||
                result.BatteryStatus == ShelterStatus.Alert ||
                result.StabilizerStatus == ShelterStatus.Alert ||
                result.ShelterAccess == ShelterStatus.Alert)
            {
                return ShelterStatus.Alert;
            }
            if (result.TemperatureStatus == ShelterStatus.Warning ||
            result.BatteryStatus == ShelterStatus.Warning ||
            result.StabilizerStatus == ShelterStatus.Warning ||
            result.ShelterAccess == ShelterStatus.Warning ||
                result.HumidityStatus == ShelterStatus.Warning)
            {
                return ShelterStatus.Warning;
            }
            return ShelterStatus.Ok;
        }
 
    }
}
