
document.addEventListener("DOMContentLoaded", () => {
	const shelterCode = String(
		window.shelterCode
		|| new URLSearchParams(window.location.search).get("shelterCode")
		|| new URLSearchParams(window.location.search).get("code")
		|| ""
	).trim().toUpperCase();

	if (!shelterCode) {
		console.error("Humidity live updates cannot start without a shelter code.");
		return;
	}

	const connection = new signalR.HubConnectionBuilder()
		.withUrl("/shelterHub")
		.withAutomaticReconnect()
		.build();

	async function joinGroup() {
		await connection.invoke("JoinShelterGroup", shelterCode);
		console.log("Joined shelter group:", shelterCode);
	}

	connection.onreconnected(async () => {
		await joinGroup();
	});

	connection.on("ShelterUpdated", data => {

		if (String(data.shelterCode || "").toUpperCase() !== shelterCode) return;

		console.log("Live update received:", data);

		/* -------------------------
			Update Humidity Value
		--------------------------*/

		const humElement = document.getElementById("currentHum");

		if (humElement) {
			humElement.innerText = `${data.humidity} %`;
		}

		/* -------------------------
			Update Status Badge
		--------------------------*/

		const statusElement = document.getElementById("humStatus");

		if (statusElement) {

			const status = (data.humidityStatus || "unknown").toLowerCase();
			statusElement.innerText = `Status: ${data.humidityStatus}`;

			statusElement.classList.remove(
				"status-ok",
				"status-warning",
				"status-alert",
				"status-unknown"
			);

			if (status === "alert")
				statusElement.classList.add("status-alert");

			else if (status === "warning")
				statusElement.classList.add("status-warning");

			else if (status === "ok")
				statusElement.classList.add("status-ok");

			else
				statusElement.classList.add("status-unknown");
		}

		/* -------------------------
			Update Chart
		--------------------------*/

		if (window.humChart && !window.historicalMode) {

			const now = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });


			window.humChart.data.labels.push(now);
			window.humChart.data.datasets[0].data.push(data.humidity);

			//keep latest 20 points only
			if (window.humChart.data.labels.length > 20) {
				window.humChart.data.labels.shift();
				window.humChart.data.datasets[0].data.shift();
			}

			window.humChart.update();
		}

	});


	connection.start()
		.then(async () => {
			console.log("SignalR connected");
			await joinGroup();
		})
		.catch(err => console.error("SignalR connection error:", err));
});
