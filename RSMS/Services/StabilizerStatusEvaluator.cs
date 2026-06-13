namespace RSMS.Services
{
    public  static class StabilizerStatusEvaluator
    {
        public static string Evaluate(double inputVoltage, double outputVoltage, double current, double loadPercentage)
        {
            if (inputVoltage < 180 || inputVoltage > 260)
                return "Critical"; 

            if (outputVoltage < 200 || outputVoltage > 245)
                return "Critical";

            if (current > 25 || loadPercentage > 90)
                return "Warning";

            if (inputVoltage < 200 || inputVoltage > 250)
                return "Warning";

            if (outputVoltage < 210 || outputVoltage > 235)
                return "Warning";

            if (current > 20 || loadPercentage > 80)
                return "Warning";

            return "Normal";
        }

        public static string CssClass(string status) 
        {
            return status switch
            {
                "Critical" => "status-critical",
                "Warning" => "status-warning",
               _ => "status-normal"
            };
        }
    }
}
