/* ============================================================
   System Health — history charts (Chart.js)
   Reads server-rendered JSON from #sh-hist-data and draws:
     1) incidents over time (line, one series per shelter)
     2) health score by shelter (bar)
   ============================================================ */
(function () {
    "use strict";

    var el = document.getElementById("sh-hist-data");
    if (!el || typeof Chart === "undefined") { return; }

    var payload;
    try { payload = JSON.parse(el.textContent || "{}"); }
    catch (e) { console.error("System Health history: bad payload", e); return; }

    var palette = ["#2e6bdc", "#28a745", "#d9534f", "#8e5bd0", "#f0ad4e", "#17a2b8"];
    function color(i) { return palette[i % palette.length]; }
    function withAlpha(hex, a) {
        var n = parseInt(hex.slice(1), 16);
        return "rgba(" + ((n >> 16) & 255) + "," + ((n >> 8) & 255) + "," + (n & 255) + "," + a + ")";
    }

    // ---- Incidents over time ----
    var incCanvas = document.getElementById("incidentsChart");
    if (incCanvas && payload.series && payload.buckets) {
        var datasets = payload.series.map(function (s, i) {
            return {
                label: s.name,
                data: s.data,
                borderColor: color(i),
                backgroundColor: withAlpha(color(i), 0.12),
                borderWidth: 2,
                tension: 0.35,
                pointRadius: 2,
                pointHoverRadius: 5,
                fill: true
            };
        });
        new Chart(incCanvas.getContext("2d"), {
            type: "line",
            data: { labels: payload.buckets, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: "index", intersect: false },
                plugins: {
                    legend: { position: "bottom", labels: { usePointStyle: true, boxWidth: 8 } },
                    tooltip: { enabled: true }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { maxRotation: 0, autoSkip: true, maxTicksLimit: 12 } },
                    y: { beginAtZero: true, ticks: { precision: 0 }, title: { display: true, text: "Incidents" } }
                }
            }
        });
    }

    // ---- Health score by shelter ----
    var scoreCanvas = document.getElementById("scoreChart");
    if (scoreCanvas && payload.scores) {
        var labels = payload.scores.map(function (s) { return s.name; });
        var data = payload.scores.map(function (s) { return s.score; });
        var bg = data.map(function (v) {
            return v >= 99 ? "#28a745" : v >= 90 ? "#f0ad4e" : v > 0 ? "#d9534f" : "#94a3b8";
        });
        new Chart(scoreCanvas.getContext("2d"), {
            type: "bar",
            data: { labels: labels, datasets: [{ label: "Health score (%)", data: data, backgroundColor: bg, borderRadius: 6, maxBarThickness: 70 }] },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: { grid: { display: false } },
                    y: { beginAtZero: true, max: 100, ticks: { callback: function (v) { return v + "%"; } } }
                }
            }
        });
    }
})();
