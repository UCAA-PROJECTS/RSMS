namespace RSMS.Services
{
    public static class BatteryStatusEvaluator
    {
        public static string Evaluate(double voltage, double stateofCharge, double temperature, double backupHoursRemaining)
        {
            if(voltage < 48 || stateofCharge < 20 || temperature > 45 || backupHoursRemaining < 1)
                return "Critical";

            if (voltage < 50 || stateofCharge < 40 || temperature > 35 || backupHoursRemaining < 2)
                return "Warning";

            return "Healthy";
        }

        public static string CssClass(string status)
        {
            return status switch
            {
                "Critical" => "status-critical",
                "Warning" => "status-warning",
                "Healthy" => "status-normal",
                "Charging" => "status-normal",
                "Discharging" => "status-warning",
                _ => "status-unknown"
            };
        }
    }
}

