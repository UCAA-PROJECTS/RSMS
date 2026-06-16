"use strict"

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
	//Get the state of charge(battery percentage) from signalR
	connection.on("BatteryUpdated", data => {
		if (String(data.shelterCode || "").toUpperCase() !== shelterCode) return;

		if (data.stateOfCharge !== undefined && data.stateOfCharge !== null) {
			setText("batteryValue", `${Number(data.stateOfCharge).toFixed(0)}%`);
		}
		//updates the status badge
		updateStatus("batteryStatus", data.status);
	});

	//Get the value of the status of the stabilizer from signalR
	connection.on("StabilizerUpdated", data => {
		if (String(data.shelterCode || "").toUpperCase() !== shelterCode) return;

		setText("stabilizerValue", data.status ?? "--")
		//updates the status badge
		updateStatus("stabilizerStatus", data.status);
	});

	connection.start()
		.then(async () => {
			console.log("SignalR connected");
			await joinGroup();
		}).catch(err => console.error("SignalR connection error:", err));

	function setText(id, value) {
		const element = document.getElementById(id);
		if (element) {
			element.innerText = value ?? "--";
		}


	}

	function updateStatus(id, status) {
		const element = document.getElementById(id);
		if (!element) return;

		const normalised = normalisedStatus(status);

		element.innerText = status ?? "--";

		element.classList.remove(
			"status-alert",
			"status-warning",
			"status-ok",
			"status-unknown"
		);
		element.classList.add(`status-${normalised}`);
	}

	function normalisedStatus(status) {
		switch ((status || "").toLowerCase()) {
			case "critical":
			case "alert":
				return "alert";

			case "warning":
			case "discharging":
				return "warning";


			case "ok":
			case "normal":
			case "healthy":
			case "charging":
				return "ok";

			default:
				return "unknown";

		}
	}

	
});