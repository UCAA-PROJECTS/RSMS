
document.addEventListener("DOMContentLoaded", () => {
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
		if (data.shelterCode !== shelterCode) {
			console.log(`shelter code ${data.shelterCode} doesnt match with ${shelterCode}`)
			return
		};
		console.log("Live update received:", data);

		/* -------------------------
			Update Smoke Value
		--------------------------*/
		const smokeElement = document.getElementById("current-smoke-value");
		if (smokeElement) {
			if (data.smokeDetected === false) {
				smokeElement.innerText = "No Smoke";
				smokeElement.className = "no-smoke";
			}
			else {
				smokeElement.innerText = "Smoke Detected";
				smokeElement.className = "yes-smoke";
			}
		}
		/* -------------------------
			Update Status Badge
		--------------------------*/
		const statusElement = document.getElementById("smokeStatus");

		if (statusElement || liveElement) {
			const status = (data.smokeStatus || "unknown").toLowerCase();
			statusElement.innerText = `Status: ${data.smokeStatus}`;

			statusElement.classList.remove(
				"status-ok",
				"status-warning",
				"status-alert",
				"status-unknown"
			);

			if (status === "alert") {
				statusElement.classList.add("status-alert");
			}
			else if (status === "warning") {
				statusElement.classList.add("status-warning");
			}
			else if (status === "ok") {
				statusElement.classList.add("status-ok");
			}
			else {
				statusElement.classList.add("status-unknown");
			}
		}

		/* -------------------------
			Update Table
		--------------------------*/
		const historyTableBody = document.querySelector("#smokeTable tbody");
		if (!historyTableBody) return;

		//create a new row
		const newRow = historyTableBody.insertRow(0);
		const cellTimeStamp = newRow.insertCell(0);
		const cellEvent = newRow.insertCell(1);
		const cellSensorName = newRow.insertCell(2);
		const cellStatus = newRow.insertCell(3);

		//create the status element
		const statusBadge = document.createElement("span");
		statusBadge.classList.add("btn", "rounded-pill", "btn-sm");

		//update the table with received values
		cellTimeStamp.innerText = data.timeStamp.replace('T', ' ').split('.')[0];
		cellSensorName.innerText = "Main Sensor";
		if (data.smokeDetected) {
			cellEvent.innerText = "Smoke Detected";

			statusBadge.innerText = "Detected";
			statusBadge.classList.add("btn-danger");


		} else {
			cellEvent.innerText = " No Smoke Detected";

			statusBadge.innerText = "Normal";
			statusBadge.classList.add("btn-success");
		}
		cellStatus.appendChild(statusBadge);
		
		

		//if (window.tempChart && !window.historicalMode) {
		//	const now = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

		//	window.tempChart.data.labels.push(now);
		//	window.tempChart.data.datasets[0].data.push(data.temperature);

		//	//keep latest 20 points only
		//	if (window.tempChart.data.labels.length > 20) {
		//		window.tempChart.data.labels.shift();
		//		window.tempChart.data.datasets[0].data.shift();
		//	}

		//	window.tempChart.update();
		//}

	});


	connection.start()
		.then(async () => {
			console.log("SignalR connected");
			await joinGroup();
		}).catch(err => console.error("SignalR connection error:", err));
});
