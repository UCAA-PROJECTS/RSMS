/* ============================================================
   System Health — single Pi node detail (live + trend charts)
   ============================================================ */
(function () {
    "use strict";

    var code = window.shelterCode;
    if (!code) return;
    var ONLINE = 120, STALE = 600, MAXPTS = 60;

    function $(id) { return document.getElementById(id); }
    function normalize(s) {
        switch (String(s || "").trim().toLowerCase()) {
            case "ok": case "normal": case "healthy": return "ok";
            case "warning": return "warning";
            case "alert": case "critical": return "alert";
            default: return "unknown";
        }
    }
    function label(s) { return s === "ok" ? "OK" : s === "warning" ? "WARNING" : s === "alert" ? "ALERT" : "UNKNOWN"; }
    function mod(s) { return "is-" + s; }
    function fmt(v, dp) { var n = Number(v); return isFinite(n) ? n.toFixed(dp) : "--"; }
    function up(s) { s = Number(s) || 0; if (s <= 0) return "--"; var d = Math.floor(s / 86400), h = Math.floor((s % 86400) / 3600), m = Math.floor((s % 3600) / 60); return d >= 1 ? d + "d " + h + "h" : h >= 1 ? h + "h " + m + "m" : m + "m"; }
    function flash(el) { if (!el) return; el.classList.remove("sh-flash"); void el.offsetWidth; el.classList.add("sh-flash"); }
    function setVal(id, t) { var el = $(id); if (el && t != null) { el.textContent = t; flash(el); } }
    function setBadge(id, s) { var el = $(id); if (!el) return; el.textContent = label(s); el.classList.remove("is-ok", "is-warning", "is-alert", "is-unknown"); el.classList.add(mod(s)); }
    function setCardBorder(sub, s) {
        var card = document.querySelector('.sh-dcard[data-sub="' + sub + '"]');
        if (!card) return; card.classList.remove("sh-dcard--is-ok", "sh-dcard--is-warning", "sh-dcard--is-alert", "sh-dcard--is-unknown"); card.classList.add("sh-dcard--" + mod(s));
    }

    var lastSeen = (function () { var el = $("shd-lastseen"); var iso = el ? el.getAttribute("data-lastseen") : ""; return iso ? new Date(iso) : null; })();
    var comp = { mqtt: "unknown", cpu: "unknown", memory: "unknown", disk: "unknown", network: "unknown" };
    ["mqtt", "cpu", "memory", "disk", "network"].forEach(function (k) {
        var el = $("shd-" + (k === "memory" ? "mem" : k === "network" ? "net" : k) + "-badge");
        if (el) comp[k] = normalize(el.textContent);
    });

    function ageSec(d) { return d ? (Date.now() - d.getTime()) / 1000 : Infinity; }
    function connFor(d) { var a = ageSec(d); return a <= ONLINE ? "online" : a <= STALE ? "stale" : "offline"; }

    function refreshTop() {
        var arr = [comp.mqtt, comp.cpu, comp.memory, comp.disk, comp.network];
        var base = arr.indexOf("alert") >= 0 ? "alert" : arr.indexOf("warning") >= 0 ? "warning" : arr.every(function (x) { return x === "ok"; }) ? "ok" : "unknown";
        if (connFor(lastSeen) === "offline" && lastSeen) base = base === "alert" ? "alert" : "warning";
        setBadge("shd-overall", base);
        var c = connFor(lastSeen), el = $("shd-conn");
        if (el) { el.classList.remove("is-online", "is-stale", "is-offline"); el.classList.add("is-" + c); var t = el.querySelector(".sh-conn__txt"); if (t) t.textContent = c.toUpperCase(); }
        var seen = $("shd-lastseen"); if (seen && lastSeen) seen.textContent = "Last seen " + lastSeen.toLocaleString();
    }

    // ---- charts ----
    var cpuChart, memChart, latChart;
    (function initCharts() {
        var el = $("shd-trend");
        if (!el || typeof Chart === "undefined") return;
        var d; try { d = JSON.parse(el.textContent || "{}"); } catch (e) { return; }
        function line(canvasId, lbls, data, color, unit) {
            var cv = $(canvasId); if (!cv) return null;
            return new Chart(cv.getContext("2d"), {
                type: "line",
                data: { labels: (lbls || []).slice(), datasets: [{ data: (data || []).slice(), borderColor: color, backgroundColor: color.replace("rgb", "rgba").replace(")", ",0.12)"), borderWidth: 2, tension: 0.35, pointRadius: 0, fill: true }] },
                options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } }, scales: { x: { display: false }, y: { beginAtZero: true, ticks: { maxTicksLimit: 4, callback: function (v) { return v + unit; } } } } }
            });
        }
        cpuChart = line("cpuChart", d.labels, d.cpu, "rgb(46,107,220)", "%");
        memChart = line("memChart", d.labels, d.mem, "rgb(142,91,208)", "%");
        latChart = line("latencyChart", d.labels, d.latency, "rgb(40,167,69)", "");
    })();
    function push(chart, lbl, val) {
        if (!chart) return;
        chart.data.labels.push(lbl); chart.data.datasets[0].data.push(Number(val) || 0);
        if (chart.data.labels.length > MAXPTS) { chart.data.labels.shift(); chart.data.datasets[0].data.shift(); }
        chart.update("none");
    }

    var connection = new signalR.HubConnectionBuilder().withUrl("/shelterHub").withAutomaticReconnect().build();
    function join() { return connection.invoke("JoinShelterGroup", code).catch(function () { }); }

    connection.on("GatewayUpdated", function (d) {
        if (!d || d.shelterCode !== code) return;
        lastSeen = new Date();
        var t = new Date().toLocaleTimeString();

        // identity
        setVal("shd-id-model", d.piModel || "—");
        setVal("shd-id-host", d.hostname || "—");
        setVal("shd-id-ip", d.ipAddress || "—");
        setVal("shd-id-os", d.osVersion || "—");
        setVal("shd-id-kernel", d.kernelVersion || "—");
        if (d.cpuCores) setVal("shd-id-cores", d.cpuCores);
        setVal("shd-id-uptime", up(d.uptimeSeconds));

        comp.mqtt = normalize(d.mqttStatus); setBadge("shd-mqtt-badge", comp.mqtt); setCardBorder("mqtt", comp.mqtt);
        setVal("shd-mqtt-latency", fmt(d.publishLatencyMs, 0) + " ms");
        setVal("shd-mqtt-failed", (Number(d.failedPublishCount) || 0).toLocaleString());
        setVal("shd-mqtt-service", d.publisherServiceActive ? "Running" : "Stopped");
        setVal("shd-mqtt-clock", d.clockSynced ? "Synced" : "NOT synced");
        setVal("shd-mqtt-lastpub", t);

        comp.cpu = normalize(d.cpuStatus); setBadge("shd-cpu-badge", comp.cpu); setCardBorder("cpu", comp.cpu);
        setVal("shd-cpu-load", fmt(d.cpuLoad, 0) + " %");
        setVal("shd-cpu-temp", fmt(d.cpuTemperature, 1) + " °C");
        setVal("shd-cpu-clock", fmt(d.clockFrequencyMhz, 0) + " MHz");
        setVal("shd-cpu-uptime", up(d.uptimeSeconds));
        setVal("shd-cpu-power", d.underVoltage ? "Under-voltage" : "Normal");
        setVal("shd-cpu-throttle", d.throttled ? "Yes" : "No");
        if (d.cpuCores) setVal("shd-cpu-cores", d.cpuCores);
        setVal("shd-cpu-loadavg", fmt(d.load1, 2) + " / " + fmt(d.load5, 2) + " / " + fmt(d.load15, 2));

        comp.memory = normalize(d.memoryStatus); setBadge("shd-mem-badge", comp.memory); setCardBorder("memory", comp.memory);
        setVal("shd-mem-total", fmt(d.memoryTotalMb, 0) + " MB");
        setVal("shd-mem-used", fmt(d.memoryUsedMb, 0) + " MB");
        setVal("shd-mem-avail", fmt(d.memoryAvailableMb, 0) + " MB");
        setVal("shd-mem-swap", fmt(d.swapUsedMb, 0) + " MB");
        setVal("shd-mem-pct", fmt(d.memoryUsedPercent, 0) + " %");

        comp.disk = normalize(d.diskStatus); setBadge("shd-disk-badge", comp.disk); setCardBorder("disk", comp.disk);
        setVal("shd-disk-total", fmt(d.diskTotalGb, 1) + " GB");
        setVal("shd-disk-used", fmt(d.diskUsedGb, 1) + " GB");
        setVal("shd-disk-free", fmt(d.diskFreeGb, 1) + " GB");
        setVal("shd-disk-pct", fmt(d.diskUsedPercent, 0) + " %");
        setVal("shd-disk-inodes", fmt(d.inodesUsedPercent, 0) + " %");

        comp.network = normalize(d.networkStatus); setBadge("shd-net-badge", comp.network); setCardBorder("network", comp.network);
        setVal("shd-net-throughput", fmt(d.netThroughputKbps, 0) + " KB/s");
        setVal("shd-net-sent", (Number(d.packetsSent) || 0).toLocaleString());
        setVal("shd-net-recv", (Number(d.packetsReceived) || 0).toLocaleString());
        setVal("shd-net-lost", (Number(d.packetsLost) || 0).toLocaleString());
        setVal("shd-net-loss", fmt(d.packetLossPercent, 1) + " %");

        push(cpuChart, t, d.cpuLoad);
        push(memChart, t, d.memoryUsedPercent);
        push(latChart, t, d.publishLatencyMs);
        refreshTop();
    });

    connection.onreconnected(join);
    connection.start().then(join).catch(function (e) { console.error("Node detail SignalR error:", e); });
    setInterval(refreshTop, 15000);
})();
