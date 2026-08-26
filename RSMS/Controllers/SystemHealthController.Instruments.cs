using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Models;

namespace RSMS.Controllers
{
    /// <summary>
    /// "Sensor Health" — the working condition of each sensing instrument / monitor
    /// (the equipment doing the checks), derived purely from real telemetry:
    ///   * freshness  -> Online / Stale / Offline
    ///   * plausibility (physical range) -> Faulty
    /// Nothing is fabricated; an instrument with no reading shows "No data".
    /// </summary>
    public partial class SystemHealthController
    {
        private const int InstOnlineSeconds = 120;
        private const int InstStaleSeconds = 600;

        // GET: /SystemHealth/Sensors
        public async Task<IActionResult> Sensors()
        {
            var model = new SystemInstrumentsView { GeneratedAtUtc = DateTime.UtcNow };
            try
            {
                var shelters = await _context.Shelters
                    .AsNoTracking().OrderBy(s => s.ShelterCode).ToListAsync();

                foreach (var sh in shelters)
                    model.Shelters.Add(await BuildInstrumentsAsync(sh.ShelterCode, sh.ShelterName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sensor health could not be generated.");
                model.DataStoreReachable = false;
                model.ErrorMessage = "The live data store is currently unreachable.";
            }
            return View(model);
        }

        private async Task<ShelterInstruments> BuildInstrumentsAsync(string code, string name)
        {
            var si = new ShelterInstruments { ShelterCode = code, ShelterName = name };
            try
            {
                var recentEnv = await _context.Readings.AsNoTracking()
                    .Where(r => r.ShelterCode == code)
                    .OrderByDescending(r => r.TimeStamp)
                    .Take(12).ToListAsync();
                var env = recentEnv.FirstOrDefault();

                var battery = await _batteryService.GetLatestAsync(code);
                var stab = await _stabilizerService.GetLatestAsync(code);

                GatewayReading? gw = null;
                try
                {
                    gw = await _context.GatewayReadings.AsNoTracking()
                        .Where(r => r.ShelterCode == code)
                        .OrderByDescending(r => r.TimeStamp)
                        .FirstOrDefaultAsync();
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Gateway instrument unavailable for {Code}.", code); }

                si.Instruments.Add(BuildTemp(env, recentEnv));
                si.Instruments.Add(BuildHumidity(env, recentEnv));
                si.Instruments.Add(BuildSmoke(env));
                si.Instruments.Add(BuildIntrusion(env));
                si.Instruments.Add(BuildBatteryMonitor(battery));
                si.Instruments.Add(BuildStabilizerMonitor(stab));
                si.Instruments.Add(BuildGatewayInstrument(gw));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Instrument health build failed for {Code}.", code);
                si.HasError = true;
            }
            return si;
        }

        // ---- individual instruments ----

        private static InstrumentHealth BuildTemp(SensorReading? env, List<SensorReading> recent)
        {
            var i = new InstrumentHealth { Name = "Temperature sensor", Icon = "fa-temperature-half", Key = "temp", StreamKey = "env" };
            if (env == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = env.TimeStamp;
            i.Value = $"{env.Temperature:0.0} °C";
            bool plausible = env.Temperature >= -40 && env.Temperature <= 85;
            if (!plausible) i.Note = "Reading outside sensor range (-40…85 °C)";
            else if (recent.Count >= 8 && recent.Select(r => Math.Round(r.Temperature, 1)).Distinct().Count() == 1)
                i.Note = $"Value unchanged across last {recent.Count} readings — verify sensor";
            i.State = StateOf(env.TimeStamp, true, plausible);
            return i;
        }

        private static InstrumentHealth BuildHumidity(SensorReading? env, List<SensorReading> recent)
        {
            var i = new InstrumentHealth { Name = "Humidity sensor", Icon = "fa-droplet", Key = "hum", StreamKey = "env" };
            if (env == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = env.TimeStamp;
            i.Value = $"{env.Humidity:0} %";
            bool plausible = env.Humidity >= 0 && env.Humidity <= 100;
            if (!plausible) i.Note = "Reading outside 0–100 % range";
            else if (recent.Count >= 8 && recent.Select(r => Math.Round(r.Humidity, 1)).Distinct().Count() == 1)
                i.Note = $"Value unchanged across last {recent.Count} readings — verify sensor";
            i.State = StateOf(env.TimeStamp, true, plausible);
            return i;
        }

        private static InstrumentHealth BuildSmoke(SensorReading? env)
        {
            var i = new InstrumentHealth { Name = "Smoke detector", Icon = "fa-fire", Key = "smoke", StreamKey = "env", Note = "Binary sensor — reports state only" };
            if (env == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = env.TimeStamp;
            i.Value = env.SmokeDetected ? "Smoke detected" : "Clear";
            i.State = StateOf(env.TimeStamp, true, true);
            return i;
        }

        private static InstrumentHealth BuildIntrusion(SensorReading? env)
        {
            var i = new InstrumentHealth { Name = "Door / intrusion sensor", Icon = "fa-door-open", Key = "intrusion", StreamKey = "env", Note = "Binary sensor — reports state only" };
            if (env == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = env.TimeStamp;
            i.Value = env.IntrusionDetected ? "Intrusion / open" : "Secure";
            i.State = StateOf(env.TimeStamp, true, true);
            return i;
        }

        private static InstrumentHealth BuildBatteryMonitor(BatteryReading? b)
        {
            var i = new InstrumentHealth { Name = "Battery monitor", Icon = "fa-battery-full", Key = "battery", StreamKey = "battery" };
            if (b == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = b.TimeStamp;
            i.Value = $"{b.StateOfCharge:0} % · {b.Voltage:0.0} V";
            bool plausible = b.Voltage > 0 && b.StateOfCharge >= 0 && b.StateOfCharge <= 100;
            if (!plausible) i.Note = "Implausible reading (0 V or SOC out of range)";
            i.State = StateOf(b.TimeStamp, true, plausible);
            return i;
        }

        private static InstrumentHealth BuildStabilizerMonitor(StabilizerReading? s)
        {
            var i = new InstrumentHealth { Name = "Stabilizer monitor", Icon = "fa-plug", Key = "stabilizer", StreamKey = "stabilizer" };
            if (s == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = s.TimeStamp;
            i.Value = $"{s.OutputVoltage:0} V out · {s.Frequency:0.0} Hz";
            bool plausible = s.OutputVoltage > 0 || s.InputVoltage > 0;
            if (!plausible) i.Note = "No voltage reading — check monitor";
            i.State = StateOf(s.TimeStamp, true, plausible);
            return i;
        }

        private static InstrumentHealth BuildGatewayInstrument(GatewayReading? g)
        {
            var i = new InstrumentHealth { Name = "Gateway (Raspberry Pi)", Icon = "fa-microchip", Key = "gateway", StreamKey = "gateway" };
            if (g == null) { i.Value = "No data"; return i; }
            i.LastSeenUtc = g.TimeStamp;
            i.Value = $"{g.CpuTemperature:0} °C · up {FormatUptime(g.UptimeSeconds)}";
            bool plausible = g.NetworkUp;
            if (!plausible) i.Note = "Network link down";
            else if (g.UnderVoltage) i.Note = "Under-voltage flag set";
            else if (g.Throttled) i.Note = "Throttling has occurred";
            i.State = StateOf(g.TimeStamp, true, plausible);
            return i;
        }

        private static InstrumentState StateOf(DateTime? lastSeen, bool hasValue, bool plausible)
        {
            if (!hasValue || lastSeen == null) return InstrumentState.Unknown;
            if (!plausible) return InstrumentState.Faulty;

            var ts = lastSeen.Value;
            var utc = ts.Kind == DateTimeKind.Local ? ts.ToUniversalTime() : DateTime.SpecifyKind(ts, DateTimeKind.Utc);
            var age = (DateTime.UtcNow - utc).TotalSeconds;
            if (age < 0) age = 0;

            if (age <= InstOnlineSeconds) return InstrumentState.Online;
            if (age <= InstStaleSeconds) return InstrumentState.Stale;
            return InstrumentState.Offline;
        }
    }
}
