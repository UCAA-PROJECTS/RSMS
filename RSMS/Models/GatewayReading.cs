namespace RSMS.Models
{
    /// <summary>
    /// Backend/infrastructure health telemetry for a shelter's Raspberry Pi node.
    /// Published by the Pi (bash script) to topic: shelters/{code}/gateway.
    /// Percentages, packet-loss and publish-latency are computed server-side at ingest.
    /// </summary>
    public class GatewayReading
    {
        public int Id { get; set; }
        public string ShelterCode { get; set; } = null!;

        // ----- Node identity -----
        public string Hostname { get; set; } = string.Empty;
        public string PiModel { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string KernelVersion { get; set; } = string.Empty;
        public int CpuCores { get; set; }

        // ----- CPU -----
        public double CpuLoad { get; set; }            // %
        public double CpuTemperature { get; set; }     // °C
        public double ClockFrequencyMhz { get; set; }
        public bool UnderVoltage { get; set; }
        public bool Throttled { get; set; }
        public long UptimeSeconds { get; set; }
        public double Load1 { get; set; }              // load average 1/5/15 min
        public double Load5 { get; set; }
        public double Load15 { get; set; }

        // ----- Memory -----
        public double MemoryTotalMb { get; set; }
        public double MemoryUsedMb { get; set; }
        public double MemoryAvailableMb { get; set; }
        public double SwapUsedMb { get; set; }
        public double MemoryUsedPercent { get; set; }  // computed

        // ----- Disk -----
        public double DiskTotalGb { get; set; }
        public double DiskUsedGb { get; set; }
        public double DiskFreeGb { get; set; }
        public double DiskUsedPercent { get; set; }    // computed
        public double InodesUsedPercent { get; set; }

        // ----- Network -----
        public double NetThroughputKbps { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long PacketsLost { get; set; }
        public double PacketLossPercent { get; set; }  // computed
        public bool NetworkUp { get; set; }

        // ----- MQTT publisher (reported by the Pi) -----
        public bool PublisherServiceActive { get; set; }
        public bool ClockSynced { get; set; }
        public long FailedPublishCount { get; set; }

        // ----- Server-computed -----
        public double PublishLatencyMs { get; set; }   // receivedUtc - TimeStamp(sent)

        // ----- Overall -----
        public string Status { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;  // when the Pi captured/sent it
    }
}
