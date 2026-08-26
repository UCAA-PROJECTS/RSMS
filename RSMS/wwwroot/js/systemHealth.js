/* ============================================================
   System Health — infrastructure overview (live)
   Per-Pi node cards (MQTT/CPU/Memory/Disk/Network) + broker tile,
   driven by the GatewayUpdated SignalR event.
   ============================================================ */
(function () {
    "use strict";

    var ONLINE = 120, STALE = 600;
    var KEYS = ["mqtt", "cpu", "memory", "disk", "network"];

    var cards = Array.prototype.slice.call(document.querySelectorAll(".sh-card[data-shelter]"));
    var codes = cards.map(function (c) { return c.getAttribute("data-shelter"); });
    if (codes.length === 0) { return; }

    var state = {};
    codes.forEach(function (code) {
        var card = document.querySelector('.sh-card[data-shelter="' + code + '"]');
        var iso = card ? card.getAttribute("data-lastseen") : "";
        var s = { lastSeen: iso ? new Date(iso) : null };
        KEYS.forEach(function (k) { s[k] = readState("sh-" + k + "-" + code); });
        state[code] = s;
    });

    function $(id) { return document.getElementById(id); }
    function normalize(s) {
        switch (String(s || "").trim().toLowerCase()) {
            case "ok": case "normal": case "healthy": return "ok";
            case "warning": return "warning";
            case "alert": case "critical": return "alert";
            default: return "unknown";
        }
    }
    function readState(id) { var el = $(id); return el ? normalize(el.textContent) : "unknown"; }
    function label(s) { return s === "ok" ? "OK" : s === "warning" ? "WARNING" : s === "alert" ? "ALERT" : "UNKNOWN"; }
    function mod(s) { return "is-" + s; }
    function dotClass(s) { return s === "ok" ? "green-dot" : s === "warning" ? "yellow-dot" : s === "alert" ? "red-dot" : "gray-dot"; }
    function fmt(v, dp) { var n = Number(v); return isFinite(n) ? n.toFixed(dp) : "--"; }

    function setBadge(id, s) { var el = $(id); if (!el) return; el.textContent = label(s); el.classList.remove("is-ok", "is-warning", "is-alert", "is-unknown"); el.classList.add(mod(s)); }
    function setDot(id, s) { var el = $(id); if (!el) return; el.classList.remove("green-dot", "yellow-dot", "red-dot", "gray-dot"); el.classList.add(dotClass(s)); }
    function setIcon(code, key, s) {
        var dot = $("sh-" + key + "-dot-" + code);
        var row = dot ? dot.closest(".sh-sub") : null;
        var icon = row ? row.querySelector(".sh-sub__icon") : null;
        if (icon) { icon.classList.remove("is-ok", "is-warning", "is-alert", "is-unknown"); icon.classList.add(mod(s)); }
    }
    function flash(el) { if (!el) return; el.classList.remove("sh-flash"); void el.offsetWidth; el.classList.add("sh-flash"); }
    function setText(id, t) { var el = $(id); if (el && t != null) { el.textContent = t; flash(el); } }

    function ageSec(d) { return d ? (Date.now() - d.getTime()) / 1000 : Infinity; }
    function connFor(d) { var a = ageSec(d); return a <= ONLINE ? "online" : a <= STALE ? "stale" : "offline"; }

    function overall(code) {
        var s = state[code];
        var arr = KEYS.map(function (k) { return s[k]; });
        var base = arr.indexOf("alert") >= 0 ? "alert" : arr.indexOf("warning") >= 0 ? "warning"
            : arr.every(function (x) { return x === "ok"; }) ? "ok" : "unknown";
        if (connFor(s.lastSeen) === "offline" && s.lastSeen) return base === "alert" ? "alert" : "warning";
        return base;
    }

    function applyConn(code) {
        var c = connFor(state[code].lastSeen);
        var el = $("sh-conn-" + code);
        if (el) { el.classList.remove("is-online", "is-stale", "is-offline"); el.classList.add("is-" + c); var t = el.querySelector(".sh-conn__txt"); if (t) t.textContent = c.toUpperCase(); }
        var seen = $("sh-lastseen-" + code);
        if (seen && state[code].lastSeen) seen.textContent = "Last seen " + state[code].lastSeen.toLocaleTimeString();
    }

    function refreshNode(code) {
        var o = overall(code);
        setBadge("sh-overall-" + code, o);
        var card = document.querySelector('.sh-card[data-shelter="' + code + '"]');
        if (card) { card.classList.remove("sh-card--is-ok", "sh-card--is-warning", "sh-card--is-alert", "sh-card--is-unknown"); card.classList.add("sh-card--" + mod(o)); }
        applyConn(code);
        refreshSummary();
    }

    function refreshSummary() {
        var ok = 0, warn = 0, alert = 0, online = 0, newest = null;
        codes.forEach(function (code) {
            var o = overall(code);
            if (o === "ok") ok++; else if (o === "warning") warn++; else if (o === "alert") alert++;
            var ls = state[code].lastSeen;
            if (connFor(ls) === "online") online++;
            if (ls && (!newest || ls > newest)) newest = ls;
        });
        if ($("sh-ok")) $("sh-ok").textContent = ok;
        if ($("sh-warn")) $("sh-warn").textContent = warn;
        if ($("sh-alert")) $("sh-alert").textContent = alert;
        if ($("sh-online")) $("sh-online").textContent = online;

        // broker tile derived from ingest
        var bState = "unknown", bText = "No data received";
        if (newest) {
            var a = ageSec(newest);
            if (online > 0 || a <= ONLINE) { bState = "ok"; bText = "Receiving data"; }
            else if (a <= STALE) { bState = "warning"; bText = "Ingest stale"; }
            else { bState = "alert"; bText = "No recent messages"; }
        }
        setBadge("sh-svc-broker-badge", bState);
        var bsum = $("sh-svc-broker-summary"); if (bsum) bsum.textContent = bText;
        var bc = $("sh-svc-broker"); if (bc) { bc.classList.remove("sh-svc--is-ok", "sh-svc--is-warning", "sh-svc--is-alert", "sh-svc--is-unknown"); bc.classList.add("sh-svc--" + mod(bState)); }

        // overall system (nodes + db snapshot + broker)
        var dbState = readState("sh-svc-db-badge");
        var states = codes.map(overall).concat([dbState, bState]);
        var sys = states.indexOf("alert") >= 0 ? "alert" : states.indexOf("warning") >= 0 ? "warning"
            : states.every(function (x) { return x === "ok"; }) ? "ok" : "unknown";
        setBadge("sh-system", sys);
        if ($("sh-updated")) $("sh-updated").textContent = new Date().toLocaleString([], { month: "short", day: "numeric", year: "numeric", hour: "2-digit", minute: "2-digit", second: "2-digit" });
    }

    function update(code, key, status, summary) {
        state[code][key] = normalize(status);
        setDot("sh-" + key + "-dot-" + code, state[code][key]);
        setBadge("sh-" + key + "-" + code, state[code][key]);
        setIcon(code, key, state[code][key]);
        setText("sh-" + key + "-sum-" + code, summary);
    }

    var connection = new signalR.HubConnectionBuilder().withUrl("/shelterHub").withAutomaticReconnect().build();
    function join() { return Promise.all(codes.map(function (c) { return connection.invoke("JoinShelterGroup", c).catch(function () { }); })); }

    connection.on("GatewayUpdated", function (d) {
        var code = d && d.shelterCode;
        if (!code || !state[code]) return;
        state[code].lastSeen = new Date();
        update(code, "cpu", d.cpuStatus, fmt(d.cpuLoad, 0) + "% load · " + fmt(d.cpuTemperature, 0) + " °C · " + fmt(d.clockFrequencyMhz, 0) + " MHz");
        update(code, "memory", d.memoryStatus, fmt(d.memoryUsedPercent, 0) + "% · " + fmt(d.memoryUsedMb, 0) + "/" + fmt(d.memoryTotalMb, 0) + " MB");
        update(code, "disk", d.diskStatus, fmt(d.diskUsedPercent, 0) + "% · " + fmt(d.diskFreeGb, 1) + " GB free");
        update(code, "network", d.networkStatus, fmt(d.netThroughputKbps, 0) + " KB/s · " + fmt(d.packetLossPercent, 1) + "% loss");
        update(code, "mqtt", d.mqttStatus, d.publisherServiceActive ? fmt(d.publishLatencyMs, 0) + " ms latency" : "publisher down");
        refreshNode(code);
    });

    connection.onreconnected(join);
    connection.start().then(join).catch(function (e) { console.error("SystemHealth SignalR error:", e); });
    setInterval(function () { codes.forEach(function (c) { applyConn(c); }); refreshSummary(); }, 15000);
})();
