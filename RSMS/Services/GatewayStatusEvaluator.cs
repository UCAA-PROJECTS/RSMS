namespace RSMS.Services
{
    /// <summary>
    /// Evaluates each Raspberry Pi infrastructure component into
    /// Normal / Warning / Critical. Thresholds are tuned for a Pi running an
    /// MQTT publisher 24/7. Used by the MQTT ingestion service and the
    /// System Health controller (recomputed from the latest reading).
    /// </summary>
    public static class GatewayStatusEvaluator
    {
        // CPU: Pi throttles around 80-85 °C; sustained high load/heat is the risk.
        public static string Cpu(double load, double temp, bool underVoltage, bool throttled)
        {
            if (underVoltage || temp > 80 || load > 95) return "Critical";
            if (throttled || temp > 70 || load > 80) return "Warning";
            return "Normal";
        }

        public static string Memory(double usedPercent)
        {
            if (usedPercent > 95) return "Critical";
            if (usedPercent > 85) return "Warning";
            return "Normal";
        }

        // Disk fills on bytes OR inodes — either can take the node down.
        public static string Disk(double usedPercent, double inodesUsedPercent)
        {
            if (usedPercent > 95 || inodesUsedPercent > 95) return "Critical";
            if (usedPercent > 85 || inodesUsedPercent > 85) return "Warning";
            return "Normal";
        }

        public static string Network(bool linkUp, double packetLossPercent)
        {
            if (!linkUp || packetLossPercent > 10) return "Critical";
            if (packetLossPercent > 2) return "Warning";
            return "Normal";
        }

        // MQTT publisher health: process must be alive; clock must be synced for
        // latency to be meaningful; high latency signals a struggling link.
        public static string Mqtt(bool serviceActive, bool clockSynced, double latencyMs)
        {
            if (!serviceActive) return "Critical";
            if (!clockSynced || latencyMs > 2000) return "Warning";
            return "Normal";
        }

        /// <summary>Worst-of roll-up across component statuses.</summary>
        public static string Worst(params string[] statuses)
        {
            var crit = false; var warn = false;
            foreach (var s in statuses)
            {
                if (s == "Critical") crit = true;
                else if (s == "Warning") warn = true;
            }
            return crit ? "Critical" : warn ? "Warning" : "Normal";
        }

        public static string CssClass(string status) => status switch
        {
            "Critical" => "status-critical",
            "Warning" => "status-warning",
            _ => "status-normal"
        };
    }
}
