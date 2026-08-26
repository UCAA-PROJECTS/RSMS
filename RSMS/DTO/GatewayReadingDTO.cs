namespace RSMS.DTO
{
    /// <summary>
    /// Infrastructure-health payload published by each Raspberry Pi's bash script
    /// to shelters/{code}/gateway. Server computes percentages, packet-loss and
    /// publish-latency from these raw values.
    /// </summary>
    public class GatewayReadingDTO
    {
        public string ShelterCode { get; set; } = null!;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

        // Node identity
        public string Hostname { get; set; } = string.Empty;
        public string PiModel { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string OsVersion { get; set; } = string.Empty;
        public string KernelVersion { get; set; } = string.Empty;
        public int CpuCores { get; set; }

        // CPU
        public double CpuLoad { get; set; }
        public double CpuTemperature { get; set; }
        public double ClockFrequencyMhz { get; set; }
        public bool UnderVoltage { get; set; }
        public bool Throttled { get; set; }
        public long UptimeSeconds { get; set; }
        public double Load1 { get; set; }
        public double Load5 { get; set; }
        public double Load15 { get; set; }

        // Memory
        public double MemoryTotalMb { get; set; }
        public double MemoryUsedMb { get; set; }
        public double MemoryAvailableMb { get; set; }
        public double SwapUsedMb { get; set; }

        // Disk
        public double DiskTotalGb { get; set; }
        public double DiskUsedGb { get; set; }
        public double DiskFreeGb { get; set; }
        public double InodesUsedPercent { get; set; }

        // Network
        public double NetThroughputKbps { get; set; }
        public long PacketsSent { get; set; }
        public long PacketsReceived { get; set; }
        public long PacketsLost { get; set; }
        public bool NetworkUp { get; set; } = true;

        // MQTT publisher (Pi side)
        public bool PublisherServiceActive { get; set; } = true;
        public bool ClockSynced { get; set; } = true;
        public long FailedPublishCount { get; set; }
    }
}
