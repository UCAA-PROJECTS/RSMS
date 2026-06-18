namespace RSMS.Models
{
    /// <summary>
    /// Normalised health state used across the System Health page so every
    /// subsystem (environment, battery, stabilizer) maps to one consistent scale.
    /// </summary>
    public enum HealthState
    {
        Ok,
        Warning,
        Alert,
        Unknown
    }

    /// <summary>
    /// Telemetry freshness for a shelter, derived from the most recent reading
    /// timestamp. This tells operators whether a shelter is actually reporting.
    /// </summary>
    public enum ConnectivityState
    {
        Online,
        Stale,
        Offline
    }

    /// <summary>A single labelled metric (e.g. "Temperature" -> "28.4 °C").</summary>
    public class HealthMetric
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = "--";
        public HealthState State { get; set; } = HealthState.Unknown;
    }

    /// <summary>Roll-up of one monitored subsystem within a shelter.</summary>
    public class SubsystemHealth
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-microchip";
        public string Key { get; set; } = string.Empty; // env | battery | stabilizer
        public HealthState State { get; set; } = HealthState.Unknown;
        public string Summary { get; set; } = "No data";
        public DateTime? LastSeenUtc { get; set; }
        public List<HealthMetric> Metrics { get; set; } = new();
    }

    /// <summary>Complete health picture for one shelter.</summary>
    public class ShelterHealth
    {
        public string ShelterCode { get; set; } = string.Empty;
        public string ShelterName { get; set; } = string.Empty;
        public HealthState Overall { get; set; } = HealthState.Unknown;
        public ConnectivityState Connectivity { get; set; } = ConnectivityState.Offline;
        public DateTime? LastSeenUtc { get; set; }
        public bool HasError { get; set; }

        public SubsystemHealth Environment { get; set; } = new() { Name = "Environment", Icon = "fa-temperature-half", Key = "env" };
        public SubsystemHealth Battery { get; set; } = new() { Name = "Battery", Icon = "fa-battery-full", Key = "battery" };
        public SubsystemHealth Stabilizer { get; set; } = new() { Name = "Stabilizer", Icon = "fa-plug", Key = "stabilizer" };
        public SubsystemHealth Gateway { get; set; } = new() { Name = "Gateway (Raspberry Pi)", Icon = "fa-microchip", Key = "gateway" };

        public IEnumerable<SubsystemHealth> Subsystems()
            => new[] { Environment, Battery, Stabilizer, Gateway };
    }

    /// <summary>Top-level model for the System Health overview page.</summary>
    public class SystemHealthOverview
    {
        public List<ShelterHealth> Shelters { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public bool DataStoreReachable { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public int Total => Shelters.Count;
        public int OkCount => Shelters.Count(s => s.Overall == HealthState.Ok);
        public int WarningCount => Shelters.Count(s => s.Overall == HealthState.Warning);
        public int AlertCount => Shelters.Count(s => s.Overall == HealthState.Alert);
        public int OnlineCount => Shelters.Count(s => s.Connectivity == ConnectivityState.Online);
        public int OfflineCount => Shelters.Count(s => s.Connectivity != ConnectivityState.Online);

        public HealthState SystemState
        {
            get
            {
                if (Shelters.Count == 0) return HealthState.Unknown;
                if (AlertCount > 0) return HealthState.Alert;
                if (WarningCount > 0) return HealthState.Warning;
                if (Shelters.All(s => s.Overall == HealthState.Ok)) return HealthState.Ok;
                return HealthState.Unknown;
            }
        }
    }

    /// <summary>
    /// Centralised mapping from health/connectivity states to CSS modifier
    /// classes and display labels. Keeping this here keeps the Razor views clean
    /// and guarantees the server and client use identical vocabulary.
    /// </summary>
    public static class HealthUi
    {
        public static string Mod(HealthState s) => s switch
        {
            HealthState.Ok => "is-ok",
            HealthState.Warning => "is-warning",
            HealthState.Alert => "is-alert",
            _ => "is-unknown"
        };

        public static string Dot(HealthState s) => s switch
        {
            HealthState.Ok => "green-dot",
            HealthState.Warning => "yellow-dot",
            HealthState.Alert => "red-dot",
            _ => "gray-dot"
        };

        public static string Label(HealthState s) => s switch
        {
            HealthState.Ok => "OK",
            HealthState.Warning => "WARNING",
            HealthState.Alert => "ALERT",
            _ => "UNKNOWN"
        };

        public static string ConnMod(ConnectivityState c) => c switch
        {
            ConnectivityState.Online => "is-online",
            ConnectivityState.Stale => "is-stale",
            _ => "is-offline"
        };

        public static string ConnLabel(ConnectivityState c) => c switch
        {
            ConnectivityState.Online => "ONLINE",
            ConnectivityState.Stale => "STALE",
            _ => "OFFLINE"
        };
    }
}
