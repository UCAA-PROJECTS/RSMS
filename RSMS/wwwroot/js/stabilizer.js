"use strict";

let voltageChart;
let liveMode = true;
let liveBuffer = [];
let stabilizerConnection;

document.addEventListener("DOMContentLoaded", () => {
    initializeChart();
    initializeSignalR();
    initializeChartButtons();
});

function initializeChart() {
    const ctx = document.getElementById("voltageChart");

    if (!ctx) {
        console.warn("voltageChart canvas not found.");
        return;
    }

    voltageChart = new Chart(ctx, {
        type: "line",
        data: {
            labels: [],
            datasets: [
                {
                    label: "Input Voltage",
                    data: [],
                    tension: 0.35
                },
                {
                    label: "Output Voltage",
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
                        text: "Voltage (V)"
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
        console.error(
            "Stabilizer live updates cannot start because no shelter code was provided."
        );
        return;
    }

    stabilizerConnection = new signalR.HubConnectionBuilder()
        .withUrl("/shelterHub")
        .withAutomaticReconnect()
        .build();

    stabilizerConnection.on("StabilizerUpdated", payload => {
        const data = normalizeStabilizerPayload(payload);

        if (!data || data.shelterCode !== shelterCode) {
            return;
        }

        console.log("Stabilizer update received:", data);
        updateCards(data);
        prependLog(data);
        liveBuffer.push(data);
        if (liveBuffer.length > 20)
        {
            liveBuffer.shift();
        }
        if (liveMode)
        {
            renderChartFromReadings(liveBuffer);
        }
    });

    stabilizerConnection.onreconnecting(error => {
        console.warn("SignalR reconnecting...", error);
    });

    stabilizerConnection.onreconnected(async () => {
        console.log("SignalR reconnected.");
        await joinShelterGroup(stabilizerConnection, shelterCode);
    });

    stabilizerConnection.onclose(error => {
        console.error("SignalR connection closed.", error);
    });

    stabilizerConnection.start()
        .then(async () => {
            console.log("SignalR connected.");
            await joinShelterGroup(stabilizerConnection, shelterCode);
        })
        .catch(error => {
            console.error("SignalR connection failed:", error);
        });
}

async function joinShelterGroup(connection, shelterCode) {
    try {
        await connection.invoke("JoinShelterGroup", shelterCode);
        console.log("Joined stabilizer shelter group:", shelterCode);
    } catch (error) {
        console.error(`Could not join shelter group ${shelterCode}:`, error);
    }
}

function getShelterCode() {
    const query = new URLSearchParams(window.location.search);
    const code = window.shelterCode
        || query.get("shelterCode")
        || query.get("code")
        || "";

    return code.trim().toUpperCase();
}

function normalizeStabilizerPayload(payload) {
    if (!payload) {
        return null;
    }

    const data = {
        shelterCode: String(payload.shelterCode ?? payload.ShelterCode ?? "")
            .trim()
            .toUpperCase(),
        inputVoltage: Number(payload.inputVoltage ?? payload.InputVoltage),
        outputVoltage: Number(payload.outputVoltage ?? payload.OutputVoltage),
        current: Number(payload.current ?? payload.Current),
        frequency: Number(payload.frequency ?? payload.Frequency),
        loadPercentage: Number(payload.loadPercentage ?? payload.LoadPercentage),
        status: payload.status ?? payload.Status ?? "Unknown",
        statusClass: payload.statusClass ?? payload.StatusClass,
        timeStamp: payload.timeStamp
            ?? payload.TimeStamp
            ?? payload.timestamp
            ?? payload.Timestamp
    };

    const numericValues = [
        data.inputVoltage,
        data.outputVoltage,
        data.current,
        data.frequency,
        data.loadPercentage
    ];

    if (!data.shelterCode || numericValues.some(value => !Number.isFinite(value))) {
        console.warn("Ignored invalid stabilizer update:", payload);
        return null;
    }

    return data;
}

function updateCards(data) {
    const voltageDifference = data.inputVoltage - data.outputVoltage;

    setText("inputVoltage", `${data.inputVoltage.toFixed(1)} V`);
    setText("outputVoltage", `${data.outputVoltage.toFixed(1)} V`);
    setText("voltageDifference", `${voltageDifference.toFixed(1)} V`);
    setText("currentValue", `${data.current.toFixed(2)} A`);
    setText("frequencyValue", `${data.frequency.toFixed(1)} Hz`);
    setText("loadValue", `${data.loadPercentage.toFixed(0)}%`);
    setText("statusText", data.status);

    const badge = document.getElementById("statusBadge");

    if (badge) {
        badge.innerText = data.status;
        badge.className = `status-pill ${data.statusClass ?? getStatusClass(data.status)}`;
    }

    setText(
        "lastUpdated",
        data.timeStamp ? new Date(data.timeStamp).toLocaleString() : "--"
    );
}

//function updateChart(data) {
//    if (!voltageChart) {
//        return;
//    }

//    const timestamp = data.timeStamp || new Date();
//    const label = new Date(timestamp).toLocaleTimeString([], {
//        hour: "2-digit",
//        minute: "2-digit"
//    });

//    voltageChart.data.labels.push(label);
//    voltageChart.data.datasets[0].data.push(data.inputVoltage);
//    voltageChart.data.datasets[1].data.push(data.outputVoltage);

//    if (voltageChart.data.labels.length > 20) {
//        voltageChart.data.labels.shift();
//        voltageChart.data.datasets[0].data.shift();
//        voltageChart.data.datasets[1].data.shift();
//    }

//    voltageChart.update();
//}

function prependLog(data) {
    const tbody = document.getElementById("logsTableBody");

    if (!tbody) {
        return;
    }

    const voltageDifference = data.inputVoltage - data.outputVoltage;
    const timestamp = data.timeStamp || new Date();
    const row = document.createElement("tr");

    row.innerHTML = `
        <td>${new Date(timestamp).toLocaleString()}</td>
        <td>${data.inputVoltage.toFixed(1)}</td>
        <td>${data.outputVoltage.toFixed(1)}</td>
        <td>${voltageDifference.toFixed(1)}</td>
        <td>${data.current.toFixed(2)}</td>
        <td>${data.loadPercentage.toFixed(0)}</td>
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

function setText(elementId, value) {
    const element = document.getElementById(elementId);

    if (element) {
        element.innerText = value;
    }
}

function getStatusClass(status) {
    switch ((status || "").toLowerCase()) {
        case "critical":
        case "alert":
            return "status-critical";

        case "warning":
            return "status-warning";

        case "normal":
        case "ok":
            return "status-normal";

        default:
            return "status-unknown";
    }
}

function initializeChartButtons()
{
    const liveBtn = document.getElementById("liveChartBtn");
    const last24Btn = document.getElementById("last24HourBtn");

    if (liveBtn)
    {
        liveBtn.addEventListener("click", () => {
            liveMode = true;
            setActiveButton("live");
            renderChartFromReadings(liveBuffer);

        });
    }

    if (last24Btn)
    {
        last24Btn.addEventListener("click", async () => {
            liveMode = false;
            setActiveButton("history");
            await loadLast24Hours();
        });
    }
}

function setActiveButton(mode)
{
    document.getElementById("liveChartBtn")?.classList.remove("active-chart-btn");
    document.getElementById("last24HourBtn")?.classList.remove("active-chart-btn");

    if (mode === "live") {
        document.getElementById("liveChartBtn")?.classList.add("active-chart-btn");
    }
    else
    {
        document.getElementById("last24HourBtn")?.classList.add("active-chart-btn");
    }
}

async function loadLast24Hours() {
    try {
        const response = await fetch(`/Stabilizer/Last24Hours?shelterCode=${encodeURIComponent(window.shelterCode)}`);

        if (!response.ok) {
            console.error("Failed to load last 24 hours data");
            return;

        }

        const readings = await response.json();
        renderChartFromReadings(readings);
    }
    catch (err)
    {
        console.error("Error loading last 24 hours data:", err);
    }
}

function renderChartFromReadings(readings)
{
    if (!voltageChart) return;
    
    voltageChart.data.labels = readings.map(x =>
        new Date(x.timeStamp).toLocaleString([], {
            hour: "2-digit",
            minute: "2-digit"
        })
    );
    voltageChart.data.datasets[0].data = readings.map(x => Number(x.inputVoltage));
    voltageChart.data.datasets[1].data = readings.map(x => Number(x.outputVoltage));

    voltageChart.update();
}
