async function loadHumSummary() {
    const shelterCod = window.shelterCode;
    try {
        const response = await fetch(
            `/HumHistory/GetHumiditySummary?shelterCode=${shelterCod}`);
        const data = await response.json();

        document.getElementById("avgHum").innerText = `${data.avgHumidity}`;
        document.getElementById("minHum").innerText = `${data.minHumidity}`;
        document.getElementById("maxHum").innerText = `${data.maxHumidity}`;
    }
    catch (error) {
        console.error("Error loading summary:", error);
    }
}

async function loadSensorStatus() {
    try {
        const response = await fetch(
            `/HumHistory/GetHumiditySummary?shelterCode=${window.shelterCode}`);

        const data = await response.json();

        const statusElement = document.getElementById("sensorStatus");

        statusElement.innerText = data.sensorStatus;

        statusElement.classList.remove("online", "warning", "offline");

        if (data.sensorStatus === "Online") {
            statusElement.classList.add("online");
        }
        else if (data.sensorStatus === "Warning") {
            statusElement.classList.add("warning");
        } else {
            statusElement.classList.add("offline");
        }
    } catch (error) {
        console.error("Error loading sensor status:", error);
    }
}


document.addEventListener("DOMContentLoaded", () => {
    //load immediately
    loadHumSummary();
    loadSensorStatus();

    //refresh full summary every 1 hour
    setInterval(loadHumSummary, 3600000);
    //refresh every 1 minute: 1s = 1000ms
    setInterval(loadSensorStatus, 60000);
})