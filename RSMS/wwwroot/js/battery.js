"use strict";

let batteryChart;
let liveMode = true;
let liveBuffer = [];
let batteryConnection;

document.addEventListener("DOMContentLoaded", () => {
    initializeChart();
    initializeSignalR();
    initializeChartButtons();
});

function initializeChart() {
    const ctx = document.getElementById("batteryChart");

    if (!ctx) return;

    batteryChart = new Chart(ctx, {
        type: "line",
        data: {
            labels: [],
            datasets: [
                {
                    label: "Voltage",
                    data: [],
                    tension: 0.35
                },
                {
                    label: "State of Charge",
                    data: [],
                    tension: 0.35
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    title: {
                        display: true,
                        text: "Voltage / SOC"
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: "Time"
                    }
                }
            }
        }
    });
}

function initializeSignalR() {
    const shelterCode = getShelterCode();
    if (!shelterCode) {
        console.error("Battery updates cannot start without a shelter code.");
        return;
    }

    batteryConnection = new signalR.HubConnectionBuilder()
        .withUrl("/shelterHub")
        .withAutomaticReconnect()
        .build();

    batteryConnection.on("BatteryUpdated", payload => {
        const data = normalizeBatteryPayload(payload);

        if (!data || data.shelterCode !== shelterCode) return;

        updateCards(data);
        prependLog(data);

        liveBuffer.push(data);
        if (liveBuffer.length > 20) liveBuffer.shift();

        if (liveMode) renderChartFromReadings(liveBuffer);
    });

    batteryConnection.onreconnected(async () => {
        await batteryConnection.invoke("JoinShelterGroup", shelterCode);
        console.log("Rejoined battery group:", shelterCode);
    });

    batteryConnection.start()
        .then(async () => {
            await batteryConnection.invoke("JoinShelterGroup", shelterCode);
            console.log("Joined battery group:", shelterCode);
        })
        .catch(err => console.error("Battery SignalR error:", err));
}

function normalizeBatteryPayload(payload) {
    if (!payload) return null;

    return {
        shelterCode: String(payload.shelterCode ?? payload.ShelterCode ?? "").trim().toUpperCase(),
        voltage: Number(payload.voltage ?? payload.Voltage),
        current: Number(payload.current ?? payload.Current),
        stateOfCharge: Number(payload.stateOfCharge ?? payload.StateOfCharge),
        temperature: Number(payload.temperature ?? payload.Temperature),
        backupHoursRemaining: Number(payload.backupHoursRemaining ?? payload.BackupHoursRemaining),
        status: payload.status ?? payload.Status ?? "Unknown",
        statusClass: payload.statusClass ?? payload.StatusClass,
        timeStamp: payload.timeStamp ?? payload.TimeStamp ?? payload.timestamp ?? payload.Timestamp
    };
}

function updateCards(data) {
    setText("batteryVoltage", `${data.voltage.toFixed(1)} V`);
    setText("batteryCurrent", `${data.current.toFixed(2)} A`);
    setText("stateOfCharge", `${data.stateOfCharge.toFixed(0)}%`);
    setText("backupTime", `${data.backupHoursRemaining.toFixed(1)} hrs`);
    setText("batteryTemperature", `${data.temperature.toFixed(1)} °C`);
    setText("statusText", data.status);

    const badge = document.getElementById("statusBadge");
    if (badge) {
        badge.innerText = data.status;
        badge.className = `status-pill ${data.statusClass ?? getStatusClass(data.status)}`;
    }

    setText("lastUpdated", formatDate(data.timeStamp));
}

function prependLog(data) {
    const tbody = document.getElementById("logsTableBody");
    if (!tbody) return;

    const row = document.createElement("tr");

    row.innerHTML = `
        <td>${formatDate(data.timeStamp)}</td>
        <td>${data.voltage.toFixed(1)} V</td>
        <td>${data.current.toFixed(2)} A</td>
        <td>${data.stateOfCharge.toFixed(0)}%</td>
        <td>${data.temperature.toFixed(1)} °C</td>
        <td>${data.backupHoursRemaining.toFixed(1)} hrs</td>
        <td>
            <span class="status-pill ${data.statusClass ?? getStatusClass(data.status)}">
                ${data.status}
            </span>
        </td>
    `;

    tbody.prepend(row);

    while (tbody.rows.length > 20) {
        tbody.deleteRow(20);
    }
}

function initializeChartButtons() {
    document.getElementById("liveChartBtn")?.addEventListener("click", () => {
        liveMode = true;
        setActiveButton("live");
        renderChartFromReadings(liveBuffer);
    });

    document.getElementById("last24HoursBtn")?.addEventListener("click", async () => {
        liveMode = false;
        setActiveButton("history");
        await loadLast24Hours();
    });
}

function setActiveButton(mode) {
    document.getElementById("liveChartBtn")?.classList.remove("active-chart-btn");
    document.getElementById("last24HoursBtn")?.classList.remove("active-chart-btn");

    if (mode === "live") {
        document.getElementById("liveChartBtn")?.classList.add("active-chart-btn");
    } else {
        document.getElementById("last24HoursBtn")?.classList.add("active-chart-btn");
    }
}

async function loadLast24Hours() {
    const shelterCode = getShelterCode();

    const response = await fetch(`/Battery/Last24Hours?shelterCode=${encodeURIComponent(shelterCode)}`);
    const readings = await response.json();

    renderChartFromReadings(readings);
}

function renderChartFromReadings(readings) {
    if (!batteryChart) return;

    batteryChart.data.labels = readings.map(x =>
        formatTime(x.timeStamp ?? x.timestamp ?? x.TimeStamp ?? x.Timestamp)
    );

    batteryChart.data.datasets[0].data = readings.map(x => Number(x.voltage ?? x.Voltage));
    batteryChart.data.datasets[1].data = readings.map(x => Number(x.stateOfCharge ?? x.StateOfCharge));

    batteryChart.update();
}

function getShelterCode() {
    const query = new URLSearchParams(window.location.search);

    return (
        window.shelterCode ||
        query.get("shelterCode") ||
        query.get("code") ||
        ""
    ).trim().toUpperCase();
}

function setText(id, value) {
    const element = document.getElementById(id);
    if (element) element.innerText = value;
}

function formatDate(value) {
    if (!value) return "--";

    const date = new Date(value);
    if (isNaN(date.getTime())) return "--";

    return date.toLocaleString();
}

function formatTime(value) {
    if (!value) return "--";

    const date = new Date(value);
    if (isNaN(date.getTime())) return "--";

    return date.toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit"
    });
}

function getStatusClass(status) {
    switch ((status || "").toLowerCase()) {
        case "critical":
        case "alert":
            return "status-critical";

        case "warning":
        case "discharging":
            return "status-warning";

        case "healthy":
        case "charging":
        case "normal":
        case "ok":
            return "status-normal";

        default:
            return "status-unknown";
    }
}