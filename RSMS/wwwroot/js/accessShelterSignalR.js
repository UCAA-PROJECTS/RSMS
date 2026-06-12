
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
			Update shelter Access Value
		--------------------------*/
		const shelterAccessStatusElement = document.getElementById("current-access-value");
		const shelterAccessTimestamp = document.getElementById("lastUpdate");
		if (shelterAccessStatusElement) {
			if (data.intrusionDetected === false) {
				shelterAccessStatusElement.innerText = "No Intrusion";
				//update the element classes
				shelterAccessStatusElement.classList.remove("yes-intrusion");
				shelterAccessStatusElement.classList.remove("not-sure-intrusion");
				shelterAccessStatusElement.classList.add("no-intrusion");

			}
			else {
				shelterAccessStatusElement.innerText = "Intrusion Detected";
				shelterAccessTimestamp.innerText = data.timeStamp.replace("T", " ").split(".")[0];
				//update the element classes
				shelterAccessStatusElement.classList.remove("no-intrusion"); 
				shelterAccessStatusElement.classList.remove("not-sure-intrusion");
				shelterAccessStatusElement.classList.add("yes-intrusion");
			}
		}

		/* -------------------------
			Update Table
		--------------------------*/
		const historyTableBody = document.querySelector("#shelterAccessTable tbody");
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
		if (data.intrusionDetected) {
			cellEvent.innerText = "Intrusion Detected";

			statusBadge.innerText = "Detected";
			statusBadge.classList.add("btn-danger");


		} else {
			cellEvent.innerText = " No Intrusion Detected";

			statusBadge.innerText = "Normal";
			statusBadge.classList.add("btn-success");
		}
		cellStatus.appendChild(statusBadge);

	});


	connection.start()
		.then(async () => {
			console.log("SignalR connected");
			await joinGroup();
		}).catch(err => console.error("SignalR connection error:", err));
});
