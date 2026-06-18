namespace RSMS.Models
{
    /// <summary>Health of one infrastructure component on a node (CPU, memory, …).</summary>
    public class ComponentHealth
    {
        public string Key { get; set; } = string.Empty;   // mqtt | cpu | memory | disk | network
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-microchip";
        public HealthState State { get; set; } = HealthState.Unknown;
        public string Summary { get; set; } = "No data";
    }

    /// <summary>A shelter's Raspberry Pi node — its overall and per-component infra health.</summary>
    public class NodeHealth
    {
        public string ShelterCode { get; set; } = string.Empty;
        public string ShelterName { get; set; } = string.Empty;
        public HealthState Overall { get; set; } = HealthState.Unknown;
        public ConnectivityState Connectivity { get; set; } = ConnectivityState.Offline;
        public DateTime? LastSeenUtc { get; set; }
        public bool HasData { get; set; }
        public bool HasError { get; set; }
        public double PublishRatePerSec { get; set; }   // computed from recent reading cadence

        public ComponentHealth Mqtt { get; set; } = new() { Key = "mqtt", Name = "MQTT Service", Icon = "fa-tower-broadcast" };
        public ComponentHealth Cpu { get; set; } = new() { Key = "cpu", Name = "CPU", Icon = "fa-microchip" };
        public ComponentHealth Memory { get; set; } = new() { Key = "memory", Name = "Memory", Icon = "fa-memory" };
        public ComponentHealth Disk { get; set; } = new() { Key = "disk", Name = "Disk", Icon = "fa-hard-drive" };
        public ComponentHealth Network { get; set; } = new() { Key = "network", Name = "Network", Icon = "fa-network-wired" };

        public IEnumerable<ComponentHealth> Components() => new[] { Mqtt, Cpu, Memory, Disk, Network };

        // Latest raw reading (node detail) + trend series for the graphs.
        public GatewayReading? Latest { get; set; }
        public List<string> TrendLabels { get; set; } = new();
        public List<double> CpuTrend { get; set; } = new();
        public List<double> MemTrend { get; set; } = new();
        public List<double> LatencyTrend { get; set; } = new();
    }

    /// <summary>A server-side service tile (Database, MQTT broker).</summary>
    public class ServiceHealth
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-server";
        public HealthState State { get; set; } = HealthState.Unknown;
        public string Summary { get; set; } = "Unknown";
        public List<HealthMetric> Details { get; set; } = new();
    }

    /// <summary>Top-level model for the infrastructure System Health overview.</summary>
    public class InfraOverview
    {
        public List<NodeHealth> Nodes { get; set; } = new();
        public ServiceHealth Database { get; set; } = new() { Key = "db", Name = "Database", Icon = "fa-database" };
        public ServiceHealth Broker { get; set; } = new() { Key = "broker", Name = "MQTT Broker", Icon = "fa-tower-broadcast" };

        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public bool DataStoreReachable { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public int Total => Nodes.Count;
        public int OkCount => Nodes.Count(n => n.Overall == HealthState.Ok);
        public int WarningCount => Nodes.Count(n => n.Overall == HealthState.Warning);
        public int AlertCount => Nodes.Count(n => n.Overall == HealthState.Alert);
        public int OnlineCount => Nodes.Count(n => n.Connectivity == ConnectivityState.Online);

        public HealthState SystemState
        {
            get
            {
                var states = Nodes.Select(n => n.Overall).Append(Database.State).Append(Broker.State).ToList();
                if (states.Count == 0) return HealthState.Unknown;
                if (states.Any(s => s == HealthState.Alert)) return HealthState.Alert;
                if (states.Any(s => s == HealthState.Warning)) return HealthState.Warning;
                if (states.All(s => s == HealthState.Ok)) return HealthState.Ok;
                return HealthState.Unknown;
            }
        }
    }
}
