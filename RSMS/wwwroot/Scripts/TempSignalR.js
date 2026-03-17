let connection;
function startTemperatureLiveUpdates(shelterCode) {
	const shelterCode = "@ViewBag.ShelterCode";

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

		if (data.shelterCode !== shelterCode) return;

		console.log("Live update received:", data);

		/* -------------------------
		   Update Temperature Value
		--------------------------*/

		const tempElement = document.getElementById("currentTemp");

		if (tempElement) {
			tempElement.innerText = `${data.temperature} °C`;
		}

		/* -------------------------
		   Update Status Badge
		--------------------------*/

		const statusElement = document.getElementById("tempStatus");

		if (statusElement) {

			const status = (data.temperatureStatus || "unknown").toLowerCase();

			statusElement.innerText = `Status: ${data.temperatureStatus}`;

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

		if (window.tempChart) {

			const now = new Date().toLocaleTimeString();

			window.tempChart.data.labels.push(now);
			window.tempChart.data.datasets[0].data.push(data.temperature);

			if (window.tempChart.data.labels.length > 20) {
				window.tempChart.data.labels.shift();
				window.tempChart.data.datasets[0].data.shift();
			}

			window.tempChart.update();
		}

	});

	connection.start()
		.then(async () => {
			console.log("SignalR connected");
			await joinGroup();
		})
		.catch(err => console.error("SignalR connection error:", err));
}