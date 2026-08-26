/* ============================================================
   System Health — Sensor / instrument health (live)
   Updates each instrument's condition from SignalR + a freshness
   timer. Mirrors the server logic: range check -> Faulty,
   otherwise Online / Stale / Offline by age.
   ============================================================ */
(function () {
    "use strict";

    var ONLINE = 120, STALE = 600;
    var STREAM_KEYS = { env: ["temp", "hum", "smoke", "intrusion"], battery: ["battery"], stabilizer: ["stabilizer"], gateway: ["gateway"] };

    var rows = Array.prototype.slice.call(document.querySelectorAll(".sh-inst[data-inst]"));
    if (rows.length === 0) return;

    var codes = {};
    var st = {}; // key "code|inst" -> {state,lastSeen}
    rows.forEach(function (r) {
        var code = r.getAttribute("data-shelter"), key = r.getAttribute("data-inst");
        codes[code] = true;
        var iso = r.getAttribute("data-lastseen");
        st[code + "|" + key] = {
            state: readState("inst-state-" + code + "-" + key),
            lastSeen: iso ? new Date(iso) : null
        };
    });
    var shelterCodes = Object.keys(codes);

    function $(id) { return document.getElementById(id); }
    function readState(id) {
        var el = $(id); var t = el ? el.textContent.trim().toUpperCase() : "";
        return t === "ONLINE" ? "online" : t === "STALE" ? "stale" : t === "FAULTY" ? "faulty" : t === "OFFLINE" ? "offline" : "unknown";
    }
    function label(s) { return s === "online" ? "ONLINE" : s === "stale" ? "STALE" : s === "faulty" ? "FAULTY" : s === "offline" ? "OFFLINE" : "NO DATA"; }
    function pillMod(s) { return "inst-" + s; }
    function dotClass(s) { return s === "online" ? "green-dot" : s === "stale" ? "yellow-dot" : s === "faulty" ? "red-dot" : "gray-dot"; }
    function fmt(v, dp) { var n = Number(v); return isFinite(n) ? n.toFixed(dp) : "--"; }

    function applyState(code, key, s) {
        var pill = $("inst-state-" + code + "-" + key);
        if (pill) { pill.textContent = label(s); pill.className = "inst-pill " + pillMod(s); }
        var dot = $("inst-dot-" + code + "-" + key);
        if (dot) { dot.className = "dot " + dotClass(s); }
        var rec = st[code + "|" + key]; if (rec) rec.state = s;
    }
    function setVal(code, key, txt) { var el = $("inst-val-" + code + "-" + key); if (el && txt != null) el.textContent = txt; }
    function setNote(code, key, txt) { var el = $("inst-note-" + code + "-" + key); if (el) el.textContent = txt || ""; }
    function setSeenNow(code, key) {
        var el = $("inst-seen-" + code + "-" + key);
        if (el) el.textContent = new Date().toLocaleTimeString();
        var rec = st[code + "|" + key]; if (rec) rec.lastSeen = new Date();
    }

    // a fresh reading arrived for one instrument
    function report(code, key, plausible, value, note) {
        if (!st[code + "|" + key]) return;
        setVal(code, key, value);
        setNote(code, key, note || "");
        setSeenNow(code, key);
        applyState(code, key, plausible ? "online" : "faulty");
        recount();
    }

    function ageSec(d) { return d ? (Date.now() - d.getTime()) / 1000 : Infinity; }

    function recount() {
        var online = 0, faulty = 0, offstale = 0;
        var perShelter = {};
        shelterCodes.forEach(function (c) { perShelter[c] = 0; });
        Object.keys(st).forEach(function (k) {
            var s = st[k].state, code = k.split("|")[0];
            if (s === "online") { online++; perShelter[code]++; }
            else if (s === "faulty") faulty++;
            else if (s === "stale" || s === "offline" || s === "unknown") offstale++;
        });
        if ($("inst-c-online")) $("inst-c-online").textContent = online;
        if ($("inst-c-faulty")) $("inst-c-faulty").textContent = faulty;
        if ($("inst-c-offline")) $("inst-c-offline").textContent = offstale;
        shelterCodes.forEach(function (c) { if ($("inst-ok-" + c)) $("inst-ok-" + c).textContent = perShelter[c]; });
    }

    // freshness timer: degrade Online -> Stale -> Offline (leave Faulty/Unknown)
    function tick() {
        Object.keys(st).forEach(function (k) {
            var rec = st[k];
            if (rec.state === "faulty" || rec.state === "unknown") return;
            var a = ageSec(rec.lastSeen);
            var ns = a <= ONLINE ? "online" : a <= STALE ? "stale" : "offline";
            if (ns !== rec.state) { var p = k.split("|"); applyState(p[0], p[1], ns); }
        });
        recount();
    }

    // ---- SignalR ----
    var connection = new signalR.HubConnectionBuilder().withUrl("/shelterHub").withAutomaticReconnect().build();
    function join() { return Promise.all(shelterCodes.map(function (c) { return connection.invoke("JoinShelterGroup", c).catch(function () { }); })); }

    connection.on("ShelterUpdated", function (d) {
        var c = d && d.shelterCode; if (!c) return;
        var t = Number(d.temperature), h = Number(d.humidity);
        report(c, "temp", isFinite(t) && t >= -40 && t <= 85, fmt(d.temperature, 1) + " °C", (t < -40 || t > 85) ? "Reading outside sensor range (-40…85 °C)" : "");
        report(c, "hum", isFinite(h) && h >= 0 && h <= 100, fmt(d.humidity, 0) + " %", (h < 0 || h > 100) ? "Reading outside 0–100 % range" : "");
        report(c, "smoke", true, d.smokeDetected ? "Smoke detected" : "Clear", "Binary sensor — reports state only");
        report(c, "intrusion", true, d.intrusionDetected ? "Intrusion / open" : "Secure", "Binary sensor — reports state only");
    });
    connection.on("BatteryUpdated", function (d) {
        var c = d && d.shelterCode; if (!c) return;
        var v = Number(d.voltage), soc = Number(d.stateOfCharge);
        var ok = v > 0 && soc >= 0 && soc <= 100;
        report(c, "battery", ok, fmt(d.stateOfCharge, 0) + " % · " + fmt(d.voltage, 1) + " V", ok ? "" : "Implausible reading (0 V or SOC out of range)");
    });
    connection.on("StabilizerUpdated", function (d) {
        var c = d && d.shelterCode; if (!c) return;
        var ok = Number(d.outputVoltage) > 0 || Number(d.inputVoltage) > 0;
        report(c, "stabilizer", ok, fmt(d.outputVoltage, 0) + " V out · " + fmt(d.frequency, 1) + " Hz", ok ? "" : "No voltage reading — check monitor");
    });
    connection.on("GatewayUpdated", function (d) {
        var c = d && d.shelterCode; if (!c) return;
        var ok = !!d.networkUp;
        var note = !ok ? "Network link down" : d.underVoltage ? "Under-voltage flag set" : d.throttled ? "Throttling has occurred" : "";
        report(c, "gateway", ok, fmt(d.cpuTemperature, 0) + " °C", note);
    });

    connection.onreconnected(join);
    connection.start().then(join).catch(function (e) { console.error("SensorHealth SignalR error:", e); });
    setInterval(tick, 15000);
})();
