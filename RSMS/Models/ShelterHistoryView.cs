namespace RSMS.Models
{
    // for temperature history view
    public class ShelterHistoryView
    {
        public string ShelterCode { get; set; } = string.Empty;
        public List<TemperatureView> TemperatureHistory { get; set; } = new();
    }

    // for humidity history view
    public class ShelterHumidityHistoryView
    {
        public string ShelterCode { get; set; } = string.Empty;
        public List<HumidityView> HumidityHistory { get; set; } = new();
    }

    // for shelter access history review
    public class ShelterAccessHistoryView
    {        public string ShelterCode { get; set; } = string.Empty;
        public List<ShelterAccessView> ShelterAccessHistory { get; set; } = new();
    }

    // // for battery level history review
    public class ShelterBatteryHistoryView
    {        public string ShelterCode { get; set; } = string.Empty;
        public List<BatteryView> BatteryHistory { get; set; } = new();
    }

    // for stabilizer 
    public class ShelterStabilizerHistoryView
    {        public string ShelterCode { get; set; } = string.Empty;
        public List<StabilizerView> StabilizerHistory { get; set; } = new();
    }

}
