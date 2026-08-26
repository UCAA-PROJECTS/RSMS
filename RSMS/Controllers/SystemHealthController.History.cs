using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RSMS.Models;

namespace RSMS.Controllers
{
    /// <summary>
    /// History / "compare health over time" actions for the System Health page.
    /// All figures are aggregated in SQL (GROUP BY date parts) so even a 30-day
    /// window pulls only small bucketed rows, never millions of raw readings.
    /// Wrapped in try/catch so an outage degrades to an empty, non-crashing view.
    /// </summary>
    public partial class SystemHealthController
    {
        // GET: /SystemHealth/History?range=7d
        public async Task<IActionResult> History(string range = "7d")
        {
            var (from, to, hourly, label, unit) = ResolveRange(range);
            var model = new SystemHealthHistory
            {
                Range = (range ?? "7d").ToLowerInvariant(),
                RangeLabel = label,
                BucketUnit = unit,
                FromUtc = from,
                ToUtc = to
            };

            try
            {
                var shelters = await _context.Shelters
                    .AsNoTracking()
                    .OrderBy(s => s.ShelterCode)
                    .ToListAsync();

                var buckets = BuildBuckets(from, to, hourly);
                model.Buckets = buckets
                    .Select(b => hourly ? b.ToLocalTime().ToString("MMM d HH:00")
                                        : b.ToLocalTime().ToString("MMM d"))
                    .ToList();

                foreach (var sh in shelters)
                {
                    var (stat, series) = await BuildHistoryForShelterAsync(
                        sh.ShelterCode, sh.ShelterName, from, to, buckets, hourly);
                    model.Shelters.Add(stat);
                    model.Series.Add(series);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "System Health history could not be generated.");
                model.DataStoreReachable = false;
                model.ErrorMessage = "Historical data is currently unavailable. Please try again once the data store is reachable.";
            }

            return View(model);
        }

        private static (DateTime from, DateTime to, bool hourly, string label, string unit) ResolveRange(string? range)
        {
            var now = DateTime.UtcNow;
            switch ((range ?? "").Trim().ToLowerInvariant())
            {
                case "24h": return (now.AddHours(-24), now, true, "Last 24 hours", "hour");
                case "30d": return (now.AddDays(-30), now, false, "Last 30 days", "day");
                case "7d":
                default: return (now.AddDays(-7), now, false, "Last 7 days", "day");
            }
        }

        private static List<DateTime> BuildBuckets(DateTime from, DateTime to, bool hourly)
        {
            var list = new List<DateTime>();
            if (hourly)
            {
                var start = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, DateTimeKind.Utc);
                for (var t = start; t <= to; t = t.AddHours(1)) list.Add(t);
            }
            else
            {
                var start = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0, DateTimeKind.Utc);
                for (var t = start; t <= to; t = t.AddDays(1)) list.Add(t);
            }
            return list;
        }

        private async Task<(ShelterHistoryStat stat, HistorySeries series)> BuildHistoryForShelterAsync(
            string code, string name, DateTime from, DateTime to, List<DateTime> buckets, bool hourly)
        {
            var stat = new ShelterHistoryStat { ShelterCode = code, ShelterName = name };
            var series = new HistorySeries
            {
                ShelterCode = code,
                ShelterName = name,
                Incidents = buckets.Select(_ => 0).ToList()
            };

            int IndexOf(int y, int mo, int d, int h)
            {
                var key = hourly
                    ? new DateTime(y, mo, d, h, 0, 0, DateTimeKind.Utc)
                    : new DateTime(y, mo, d, 0, 0, 0, DateTimeKind.Utc);
                return buckets.FindIndex(b => b == key);
            }

            // ---- Environment ----
            var env = await _context.Readings.AsNoTracking()
                .Where(r => r.ShelterCode == code && r.TimeStamp >= from && r.TimeStamp <= to)
                .GroupBy(r => new { r.TimeStamp.Year, r.TimeStamp.Month, r.TimeStamp.Day, r.TimeStamp.Hour })
                .Select(g => new
                {
                    g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour,
                    Total = g.Count(),
                    TempAlert = g.Sum(x => x.Temperature > 40 ? 1 : 0),
                    TempWarn = g.Sum(x => (x.Temperature > 30 && x.Temperature <= 40) ? 1 : 0),
                    HumAlert = g.Sum(x => x.Humidity > 80 ? 1 : 0),
                    HumWarn = g.Sum(x => (x.Humidity > 60 && x.Humidity <= 80) ? 1 : 0),
                    Smoke = g.Sum(x => x.SmokeDetected ? 1 : 0),
                    Intr = g.Sum(x => x.IntrusionDetected ? 1 : 0),
                    EnvOk = g.Sum(x => (x.Temperature <= 30 && x.Humidity <= 60 && !x.SmokeDetected && !x.IntrusionDetected) ? 1 : 0),
                    AvgTemp = g.Average(x => x.Temperature),
                    MaxTemp = g.Max(x => x.Temperature)
                })
                .ToListAsync();

            double weightedTempSum = 0; int tempCount = 0;
            foreach (var e in env)
            {
                stat.EnvReadings += e.Total;
                stat.TempAlerts += e.TempAlert;
                stat.HumidityAlerts += e.HumAlert;
                stat.SmokeEvents += e.Smoke;
                stat.IntrusionEvents += e.Intr;
                stat.Warnings += e.TempWarn + e.HumWarn;
                stat.EnvOk += e.EnvOk;
                if (stat.MaxTemperature == null || e.MaxTemp > stat.MaxTemperature) stat.MaxTemperature = e.MaxTemp;
                weightedTempSum += e.AvgTemp * e.Total; tempCount += e.Total;
                var idx = IndexOf(e.Year, e.Month, e.Day, e.Hour);
                if (idx >= 0) series.Incidents[idx] += e.TempAlert + e.HumAlert + e.Smoke + e.Intr;
            }
            if (tempCount > 0) stat.AvgTemperature = Math.Round(weightedTempSum / tempCount, 1);

            // ---- Battery ----
            var bat = await _context.BatteryReadings.AsNoTracking()
                .Where(r => r.ShelterCode == code && r.TimeStamp >= from && r.TimeStamp <= to)
                .GroupBy(r => new { r.TimeStamp.Year, r.TimeStamp.Month, r.TimeStamp.Day, r.TimeStamp.Hour })
                .Select(g => new
                {
                    g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour,
                    Total = g.Count(),
                    Crit = g.Sum(x => x.Status == "Critical" ? 1 : 0),
                    Warn = g.Sum(x => x.Status == "Warning" ? 1 : 0),
                    MinSoc = g.Min(x => x.StateOfCharge)
                })
                .ToListAsync();

            foreach (var b in bat)
            {
                stat.BatteryReadings += b.Total;
                stat.BatteryCritical += b.Crit;
                stat.BatteryWarning += b.Warn;
                if (stat.MinStateOfCharge == null || b.MinSoc < stat.MinStateOfCharge) stat.MinStateOfCharge = b.MinSoc;
                var idx = IndexOf(b.Year, b.Month, b.Day, b.Hour);
                if (idx >= 0) series.Incidents[idx] += b.Crit;
            }

            // ---- Stabilizer ----
            var stab = await _context.StabilizerReadings.AsNoTracking()
                .Where(r => r.ShelterCode == code && r.TimeStamp >= from && r.TimeStamp <= to)
                .GroupBy(r => new { r.TimeStamp.Year, r.TimeStamp.Month, r.TimeStamp.Day, r.TimeStamp.Hour })
                .Select(g => new
                {
                    g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour,
                    Total = g.Count(),
                    Crit = g.Sum(x => x.Status == "Critical" ? 1 : 0),
                    Warn = g.Sum(x => x.Status == "Warning" ? 1 : 0)
                })
                .ToListAsync();

            foreach (var s in stab)
            {
                stat.StabilizerReadings += s.Total;
                stat.StabilizerCritical += s.Crit;
                stat.StabilizerWarning += s.Warn;
                var idx = IndexOf(s.Year, s.Month, s.Day, s.Hour);
                if (idx >= 0) series.Incidents[idx] += s.Crit;
            }

            // ---- Gateway (Raspberry Pi) ---- isolated: table may post-date the migration
            try
            {
                var gw = await _context.GatewayReadings.AsNoTracking()
                    .Where(r => r.ShelterCode == code && r.TimeStamp >= from && r.TimeStamp <= to)
                    .GroupBy(r => new { r.TimeStamp.Year, r.TimeStamp.Month, r.TimeStamp.Day, r.TimeStamp.Hour })
                    .Select(g => new
                    {
                        g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour,
                        Total = g.Count(),
                        Crit = g.Sum(x => x.Status == "Critical" ? 1 : 0),
                        Warn = g.Sum(x => x.Status == "Warning" ? 1 : 0)
                    })
                    .ToListAsync();

                foreach (var x in gw)
                {
                    stat.GatewayReadings += x.Total;
                    stat.GatewayCritical += x.Crit;
                    stat.GatewayWarning += x.Warn;
                    var idx = IndexOf(x.Year, x.Month, x.Day, x.Hour);
                    if (idx >= 0) series.Incidents[idx] += x.Crit;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gateway history unavailable for {ShelterCode} (has the migration been applied?).", code);
            }

            stat.Incidents = stat.TempAlerts + stat.HumidityAlerts + stat.SmokeEvents
                           + stat.IntrusionEvents + stat.BatteryCritical + stat.StabilizerCritical + stat.GatewayCritical;
            stat.Warnings += stat.BatteryWarning + stat.StabilizerWarning + stat.GatewayWarning;

            var total = stat.TotalReadings;
            var ok = stat.EnvOk
                   + (stat.BatteryReadings - stat.BatteryCritical - stat.BatteryWarning)
                   + (stat.StabilizerReadings - stat.StabilizerCritical - stat.StabilizerWarning)
                   + (stat.GatewayReadings - stat.GatewayCritical - stat.GatewayWarning);
            if (ok < 0) ok = 0;
            stat.HealthScore = total > 0 ? Math.Round(100.0 * ok / total, 1) : 0;

            return (stat, series);
        }
    }
}
