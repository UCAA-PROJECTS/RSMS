namespace RSMS.Models
{
    /// <summary>
    /// Working condition of an individual sensing instrument / monitor — i.e. the
    /// health of the device taking the reading, distinct from the value itself.
    /// </summary>
    public enum InstrumentState
    {
        Online,    // reporting recently with a plausible value
        Stale,     // last reading is getting old
        Offline,   // not reporting
        Faulty,    // reporting, but the value is out of physical range / implausible
        Unknown    // never reported
    }

    public class InstrumentHealth
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-microchip";
        public string Key { get; set; } = string.Empty;        // temp | hum | smoke | intrusion | battery | stabilizer | gateway
        public string StreamKey { get; set; } = string.Empty;  // env | battery | stabilizer | gateway (drives freshness)
        public InstrumentState State { get; set; } = InstrumentState.Unknown;
        public string Value { get; set; } = "--";
        public string Note { get; set; } = string.Empty;
        public DateTime? LastSeenUtc { get; set; }
    }

    public class ShelterInstruments
    {
        public string ShelterCode { get; set; } = string.Empty;
        public string ShelterName { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public List<InstrumentHealth> Instruments { get; set; } = new();

        public int Healthy => Instruments.Count(i => i.State == InstrumentState.Online);
        public int Problems => Instruments.Count(i =>
            i.State == InstrumentState.Faulty || i.State == InstrumentState.Offline);
    }

    public class SystemInstrumentsView
    {
        public List<ShelterInstruments> Shelters { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
        public bool DataStoreReachable { get; set; } = true;
        public string? ErrorMessage { get; set; }

        private IEnumerable<InstrumentHealth> All => Shelters.SelectMany(s => s.Instruments);
        public int Total => All.Count();
        public int OnlineCount => All.Count(i => i.State == InstrumentState.Online);
        public int StaleCount => All.Count(i => i.State == InstrumentState.Stale);
        public int FaultyCount => All.Count(i => i.State == InstrumentState.Faulty);
        public int OfflineCount => All.Count(i => i.State == InstrumentState.Offline || i.State == InstrumentState.Unknown);
        public bool AllHealthy => Total > 0 && OnlineCount == Total;
    }

    public static class InstrumentUi
    {
        public static string Mod(InstrumentState s) => s switch
        {
            InstrumentState.Online => "inst-online",
            InstrumentState.Stale => "inst-stale",
            InstrumentState.Offline => "inst-offline",
            InstrumentState.Faulty => "inst-faulty",
            _ => "inst-unknown"
        };

        public static string Label(InstrumentState s) => s switch
        {
            InstrumentState.Online => "ONLINE",
            InstrumentState.Stale => "STALE",
            InstrumentState.Offline => "OFFLINE",
            InstrumentState.Faulty => "FAULTY",
            _ => "NO DATA"
        };

        public static string Dot(InstrumentState s) => s switch
        {
            InstrumentState.Online => "green-dot",
            InstrumentState.Stale => "yellow-dot",
            InstrumentState.Faulty => "red-dot",
            _ => "gray-dot"
        };
    }
}
