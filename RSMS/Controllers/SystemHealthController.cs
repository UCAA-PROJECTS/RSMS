using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RSMS.Data;
using RSMS.Models;
using RSMS.Services;

namespace RSMS.Controllers
{
    /// <summary>
    /// Backend / infrastructure System Health: per-shelter Raspberry Pi nodes
    /// (CPU, memory, disk, network, MQTT publisher) plus server-side Database and
    /// MQTT broker status. Read-only and fully exception-guarded so a data-store
    /// or service outage degrades to "Unknown / Offline" instead of a 500.
    ///
    /// (History and Sensor-Health live in the partial-class files
    ///  SystemHealthController.History.cs and SystemHealthController.Instruments.cs.)
    /// </summary>
    public partial class SystemHealthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IShelterService _shelterService;
        private readonly IBatteryService _batteryService;
        private readonly IStabilizerService _stabilizerService;
        private readonly ILogger<SystemHealthController> _logger;

        private const int OnlineWithinSeconds = 120;
        private const int StaleWithinSeconds = 600;

        public SystemHealthController(
            ApplicationDbContext context,
            IShelterService shelterService,
            IBatteryService batteryService,
            IStabilizerService stabilizerService,
            ILogger<SystemHealthController> logger)
        {
            _context = context;
            _shelterService = shelterService;
            _batteryService = batteryService;
            _stabilizerService = stabilizerService;
            _logger = logger;
        }

        // GET: /SystemHealth  — infrastructure overview
        public async Task<IActionResult> Index()
        {
            var model = new InfraOverview { GeneratedAtUtc = DateTime.UtcNow };
            try
            {
                var shelters = await _context.Shelters
                    .AsNoTracking().OrderBy(s => s.ShelterCode).ToListAsync();

                foreach (var sh in shelters)
                    model.Nodes.Add(await BuildNodeAsync(sh.ShelterCode, sh.ShelterName, withTrend: false));

                model.Database = await BuildDatabaseServiceAsync();
                model.Broker = BuildBrokerService(model.Nodes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Infrastructure overview could not be generated.");
                model.DataStoreReachable = false;
                model.ErrorMessage = "The live data store is currently unreachable.";
            }
            return View(model);
        }

        // GET: /SystemHealth/Shelter?shelterCode=GP001  — single node detail
        public async Task<IActionResult> Shelter(string? shelterCode, string? code)
        {
            var sc = (shelterCode ?? code)?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(sc)) return RedirectToAction(nameof(Index));

            ViewBag.ShelterCode = sc;
            try
            {
                var shelter = await _context.Shelters.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.ShelterCode == sc);
                if (shelter == null) return NotFound($"Unknown shelter code: {sc}");

                return View(await BuildNodeAsync(shelter.ShelterCode, shelter.ShelterName, withTrend: true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Node detail could not be generated for {ShelterCode}.", sc);
                return View(new NodeHealth { ShelterCode = sc, ShelterName = sc, HasError = true });
            }
        }

        // ---------------------------------------------------------------------
        private async Task<NodeHealth> BuildNodeAsync(string code, string name, bool withTrend)
        {
            var node = new NodeHealth { ShelterCode = code, ShelterName = name };
            try
            {
                GatewayReading? latest = null;
                List<GatewayReading> recent = new();
                try
                {
                    var q = _context.GatewayReadings.AsNoTracking()
                        .Where(r => r.ShelterCode == code)
                        .OrderByDescending(r => r.TimeStamp);
                    if (withTrend) { recent = await q.Take(60).ToListAsync(); latest = recent.FirstOrDefault(); }
                    else latest = await q.FirstOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Gateway readings unavailable for {Code} (migration applied?).", code);
                }

                if (latest == null)
                {
                    node.Connectivity = ConnectivityState.Offline;
                    node.Overall = HealthState.Unknown;
                    return node;
                }

                node.HasData = true;
                node.Latest = latest;
                node.LastSeenUtc = latest.TimeStamp;
                node.Connectivity = Freshness(latest.TimeStamp);

                var cpuS = GatewayStatusEvaluator.Cpu(latest.CpuLoad, latest.CpuTemperature, latest.UnderVoltage, latest.Throttled);
                var memS = GatewayStatusEvaluator.Memory(latest.MemoryUsedPercent);
                var diskS = GatewayStatusEvaluator.Disk(latest.DiskUsedPercent, latest.InodesUsedPercent);
                var netS = GatewayStatusEvaluator.Network(latest.NetworkUp, latest.PacketLossPercent);
                var mqttS = GatewayStatusEvaluator.Mqtt(latest.PublisherServiceActive, latest.ClockSynced, latest.PublishLatencyMs);

                node.Cpu.State = MapStatusText(cpuS);
                node.Cpu.Summary = $"{latest.CpuLoad:0}% load · {latest.CpuTemperature:0} °C · {latest.ClockFrequencyMhz:0} MHz";
                node.Memory.State = MapStatusText(memS);
                node.Memory.Summary = $"{latest.MemoryUsedPercent:0}% · {latest.MemoryUsedMb:0}/{latest.MemoryTotalMb:0} MB";
                node.Disk.State = MapStatusText(diskS);
                node.Disk.Summary = $"{latest.DiskUsedPercent:0}% · {latest.DiskFreeGb:0.#} GB free";
                node.Network.State = MapStatusText(netS);
                node.Network.Summary = $"{latest.NetThroughputKbps:0} KB/s · {latest.PacketLossPercent:0.#}% loss";
                node.Mqtt.State = MapStatusText(mqttS);
                node.Mqtt.Summary = latest.PublisherServiceActive ? $"{latest.PublishLatencyMs:0} ms latency" : "publisher down";

                node.Overall = OverallFrom(
                    new[] { node.Mqtt.State, node.Cpu.State, node.Memory.State, node.Disk.State, node.Network.State },
                    node.Connectivity, node.LastSeenUtc);

                if (withTrend && recent.Count > 0)
                {
                    var ordered = recent.OrderBy(r => r.TimeStamp).ToList();
                    if (ordered.Count >= 2)
                    {
                        var span = (AsUtc(ordered[ordered.Count - 1].TimeStamp) - AsUtc(ordered[0].TimeStamp)).TotalSeconds;
                        node.PublishRatePerSec = span > 0 ? Math.Round((ordered.Count - 1) / span, 2) : 0;
                    }
                    node.TrendLabels = ordered.Select(r => r.TimeStamp.ToLocalTime().ToString("HH:mm:ss")).ToList();
                    node.CpuTrend = ordered.Select(r => Math.Round(r.CpuLoad, 1)).ToList();
                    node.MemTrend = ordered.Select(r => Math.Round(r.MemoryUsedPercent, 1)).ToList();
                    node.LatencyTrend = ordered.Select(r => Math.Round(r.PublishLatencyMs, 0)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Node build failed for {Code}.", code);
                node.HasError = true;
            }
            return node;
        }

        private async Task<ServiceHealth> BuildDatabaseServiceAsync()
        {
            var svc = new ServiceHealth { Key = "db", Name = "Database", Icon = "fa-database" };
            try
            {
                if (!await _context.Database.CanConnectAsync())
                {
                    svc.State = HealthState.Alert;
                    svc.Summary = "Unreachable";
                    svc.Details.Add(new HealthMetric { Label = "Connection", Value = "Failed", State = HealthState.Alert });
                    return svc;
                }

                DateTime? lastWrite = await _context.Readings.AsNoTracking()
                    .OrderByDescending(r => r.TimeStamp)
                    .Select(r => (DateTime?)r.TimeStamp).FirstOrDefaultAsync();
                long rows = await _context.Readings.AsNoTracking().LongCountAsync();

                var stale = lastWrite == null || (DateTime.UtcNow - AsUtc(lastWrite.Value)).TotalMinutes > 10;
                svc.State = stale ? HealthState.Warning : HealthState.Ok;
                svc.Summary = stale ? "Connected · no recent writes" : "Connected · writing";
                svc.Details.Add(new HealthMetric { Label = "Connection", Value = "Connected", State = HealthState.Ok });
                svc.Details.Add(new HealthMetric { Label = "Last write", Value = lastWrite == null ? "—" : lastWrite.Value.ToLocalTime().ToString("MMM d, HH:mm:ss"), State = stale ? HealthState.Warning : HealthState.Ok });
                svc.Details.Add(new HealthMetric { Label = "Env readings", Value = rows.ToString("N0") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database status check failed.");
                svc.State = HealthState.Alert;
                svc.Summary = "Unreachable";
            }
            return svc;
        }

        private ServiceHealth BuildBrokerService(List<NodeHealth> nodes)
        {
            var svc = new ServiceHealth { Key = "broker", Name = "MQTT Broker", Icon = "fa-tower-broadcast" };
            var seen = nodes.Where(n => n.LastSeenUtc != null).Select(n => n.LastSeenUtc!.Value).ToList();
            var online = nodes.Count(n => n.Connectivity == ConnectivityState.Online);
            svc.Details.Add(new HealthMetric { Label = "Nodes reporting", Value = $"{online}/{nodes.Count}" });

            if (seen.Count == 0)
            {
                svc.State = HealthState.Unknown;
                svc.Summary = "No data received";
                return svc;
            }

            var newest = seen.Max();
            svc.Details.Add(new HealthMetric { Label = "Last message", Value = newest.ToLocalTime().ToString("HH:mm:ss") });
            var ageSec = (DateTime.UtcNow - AsUtc(newest)).TotalSeconds;
            if (ageSec <= OnlineWithinSeconds) { svc.State = HealthState.Ok; svc.Summary = "Receiving data"; }
            else if (ageSec <= StaleWithinSeconds) { svc.State = HealthState.Warning; svc.Summary = "Ingest stale"; }
            else { svc.State = HealthState.Alert; svc.Summary = "No recent messages"; }
            return svc;
        }

        // ---------------------------------------------------------------------
        // Helpers (shared with the partial-class files)
        // ---------------------------------------------------------------------

        private static HealthState OverallFrom(HealthState[] states, ConnectivityState conn, DateTime? lastSeen)
        {
            HealthState fromComponents =
                states.Any(s => s == HealthState.Alert) ? HealthState.Alert :
                states.Any(s => s == HealthState.Warning) ? HealthState.Warning :
                states.All(s => s == HealthState.Ok) ? HealthState.Ok :
                HealthState.Unknown;

            if (conn == ConnectivityState.Offline && lastSeen != null)
                return fromComponents == HealthState.Alert ? HealthState.Alert : HealthState.Warning;
            return fromComponents;
        }

        private static HealthState MapStatusText(string? status) => (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "ok" or "normal" or "healthy" or "charging" => HealthState.Ok,
            "warning" or "discharging" => HealthState.Warning,
            "alert" or "critical" => HealthState.Alert,
            _ => HealthState.Unknown
        };

        private static DateTime AsUtc(DateTime t)
            => t.Kind == DateTimeKind.Local ? t.ToUniversalTime() : DateTime.SpecifyKind(t, DateTimeKind.Utc);

        private static ConnectivityState Freshness(DateTime? lastSeen)
        {
            if (lastSeen == null) return ConnectivityState.Offline;
            var age = (DateTime.UtcNow - AsUtc(lastSeen.Value)).TotalSeconds;
            if (age < 0) age = 0;
            if (age <= OnlineWithinSeconds) return ConnectivityState.Online;
            if (age <= StaleWithinSeconds) return ConnectivityState.Stale;
            return ConnectivityState.Offline;
        }

        private static string FormatUptime(long seconds)
        {
            if (seconds <= 0) return "--";
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
            if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
            return $"{ts.Minutes}m";
        }
    }
}
