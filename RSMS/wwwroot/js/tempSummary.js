async function loadTempSummary() {
    const shelterCod = window.shelterCode;
    console.log(`Shelter code received: ${shelterCod}`);
    try {
        const response = await fetch(
            `/TempHistory/GetTemperatureSummary?shelterCode=${shelterCod}`);
        const data = await response.json();

        console.log("summary data received:", data);

        document.getElementById("avgTemp").innerText = `${data.avgTemperature}`;
        document.getElementById("minTemp").innerText = `${data.minTemperature}`;
        document.getElementById("maxTemp").innerText = `${data.maxTemperature}`;
    }
    catch (error) {
        console.error("Error loading summary:", error);
    }
}

async function loadSensorStatus() {
    try {
        const response = await fetch(
            `/TempHistory/GetTemperatureSummary?shelterCode=${window.shelterCode}`);

        const data = await response.json();

        const statusElement = document.getElementById("sensorStatus");

        statusElement.innerText = data.sensorStatus;

        statusElement.classList.remove("online", "warning", "offline");

        if (data.sensorStatus === "Online")
        {
            statusElement.classList.add("online");
        }
        else if (data.sensorStatus === "Warning")
        {
            statusElement.classList.add("warning");
        } else
        {
            statusElement.classList.add("offline");
        }
    } catch (error) {
        console.error("Error loading sensor status:", error);
    }
}
    

document.addEventListener("DOMContentLoaded", async () => {
    //load immediately
    await loadTempSummary();
    await loadSensorStatus();

    //refresh full summary every 1 hour
    setInterval(loadTempSummary, 3600000);

    //refresh every 1 minute: 1s = 1000ms
    setInterval(loadSensorStatus, 60000);
})