const connection = new signalR.HubConnectionBuilder()
    .withUrl("/shelterHub")
    .withAutomaticReconnect()
    .build();

// -----------------------------
// Join shelter groups
// -----------------------------
const shelterCodes = ["ILS002", "DVOR003", "GP001"];

async function joinShelterGroups() {
    for (const code of shelterCodes) {
        await connection.invoke("JoinShelterGroup", code);
        console.log("Joined group:", code);
    }
}

connection.onreconnected(async () => {
    console.log("Reconnected — rejoining groups");
    await joinShelterGroups();
});

// -----------------------------
// Receive updates
// -----------------------------
connection.on("ShelterUpdated", data => {

    console.log("SignalR update:", data);

    const shelter = data.shelterCode;
    if (!shelter) return;


    function updateValue(elementId, value) {
        const el = document.getElementById(elementId);
        if (el) el.innerText = value ?? "--";
    }


    function updateDot(dotId, status) {

        const dot = document.getElementById(dotId);
        if (!dot) return;

        dot.classList.remove(
            "red-dot",
            "yellow-dot",
            "green-dot",
            "gray-dot"
        );

        switch ((status || "").toLowerCase()) {
            case "alert":
                dot.classList.add("red-dot");
                break;

            case "warning":
                dot.classList.add("yellow-dot");
                break;

            case "ok":
                dot.classList.add("green-dot");
                break;

            default:
                dot.classList.add("gray-dot");
                break;
        }
    }
    function updateBadge(badgeId, status) {

        const badge = document.getElementById(badgeId);
        if (!badge) return;

        const normalized = (status || "unknown").toLowerCase();

        badge.innerText = normalized.toUpperCase();

        badge.classList.remove(
            "status-alert",
            "status-warning",
            "status-ok",
            "status-unknown"
        );

        switch (normalized) {
            case "alert":
                badge.classList.add("status-alert");
                break;

            case "warning":
                badge.classList.add("status-warning");
                break;

            case "ok":
                badge.classList.add("status-ok");
                break;

            default:
                badge.classList.add("status-unknown");
                break;
        }
    }

    // -----------------------------
    // Update numeric values
    // -----------------------------
    updateValue(`temp-${shelter}`, `${data.temperature} °C`);
    updateValue(`hum-${shelter}`, `${data.humidity} %`);
    // -----------------------------
    // Independent sensor dots
    // -----------------------------
    updateDot(`temp-dot-${shelter}`, data.temperatureStatus);
    updateDot(`hum-dot-${shelter}`, data.humidityStatus);
    updateDot(`ink-dot-${shelter}`, data.intrudeStatus);
    updateDot(`smk-dot-${shelter}`, data.smokeStatus);

    // -----------------------------
    // Overall badge (separate logic)
    // -----------------------------
    updateBadge(`status-banner-${shelter}`, data.overallStatus);
});



// -----------------------------
// Start connection
// -----------------------------
connection.start()
    .then(async () => {
        console.log("SignalR connected");
        await joinShelterGroups();
    })
    .catch(err => console.error("SignalR error:", err));