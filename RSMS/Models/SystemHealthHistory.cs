namespace RSMS.Models
{
    /// <summary>
    /// Aggregated health statistics for one shelter over a chosen time window.
    /// Every figure is derived from real rows in Readings / BatteryReadings /
    /// StabilizerReadings — nothing is synthesised.
    /// </summary>
    public class ShelterHistoryStat
    {
        public string ShelterCode { get; set; } = string.Empty;
        public string ShelterName { get; set; } = string.Empty;

        public int EnvReadings { get; set; }
        public int BatteryReadings { get; set; }
        public int StabilizerReadings { get; set; }
        public int GatewayReadings { get; set; }
        public int EnvOk { get; set; }

        // Alert-level events
        public int TempAlerts { get; set; }
        public int HumidityAlerts { get; set; }
        public int SmokeEvents { get; set; }
        public int IntrusionEvents { get; set; }
        public int BatteryCritical { get; set; }
        public int StabilizerCritical { get; set; }
        public int GatewayCritical { get; set; }

        // Warning-level events
        public int BatteryWarning { get; set; }
        public int StabilizerWarning { get; set; }
        public int GatewayWarning { get; set; }
        public int Warnings { get; set; }

        public int Incidents { get; set; }      // total alert-level events
        public double HealthScore { get; set; } // 0..100, % of readings that were OK

        public double? AvgTemperature { get; set; }
        public double? MaxTemperature { get; set; }
        public double? MinStateOfCharge { get; set; }

        public int TotalReadings => EnvReadings + BatteryReadings + StabilizerReadings + GatewayReadings;
        public bool HasData => TotalReadings > 0;

        public HealthState ScoreState =>
            !HasData ? HealthState.Unknown :
            HealthScore >= 99 ? HealthState.Ok :
            HealthScore >= 90 ? HealthState.Warning :
            HealthState.Alert;
    }

    /// <summary>Per-shelter incident counts per chart bucket (time series).</summary>
    public class HistorySeries
    {
        public string ShelterCode { get; set; } = string.Empty;
        public string ShelterName { get; set; } = string.Empty;
        public List<int> Incidents { get; set; } = new();
    }

    /// <summary>Model for the System Health history / comparison page.</summary>
    public class SystemHealthHistory
    {
        public string Range { get; set; } = "7d";
        public string RangeLabel { get; set; } = "Last 7 days";
        public string BucketUnit { get; set; } = "day";
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public List<ShelterHistoryStat> Shelters { get; set; } = new();
        public List<string> Buckets { get; set; } = new();
        public List<HistorySeries> Series { get; set; } = new();

        public bool DataStoreReachable { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public int TotalIncidents => Shelters.Sum(s => s.Incidents);
        public int TotalWarnings => Shelters.Sum(s => s.Warnings);
        public long TotalReadings => Shelters.Sum(s => (long)s.TotalReadings);
        public double AvgHealthScore => Shelters.Count(s => s.HasData) == 0
            ? 0
            : Math.Round(Shelters.Where(s => s.HasData).Average(s => s.HealthScore), 1);
        public ShelterHistoryStat? MostIncidents =>
            Shelters.OrderByDescending(s => s.Incidents).FirstOrDefault(s => s.Incidents > 0);
        public bool AnyData => Shelters.Any(s => s.HasData);
    }
}
