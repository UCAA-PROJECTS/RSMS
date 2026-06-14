export function addRowToTable(tableBodyElement, reading,targetSensor) {
	//create a new row
	const newRow = tableBodyElement.insertRow(0);
	const cellTimeStamp = newRow.insertCell(0);
	const cellEvent = newRow.insertCell(1);
	const cellStatus = newRow.insertCell(2);
	const cellDescription = newRow.insertCell(3);

	//create the status element
	const statusBadge = document.createElement("span");
	statusBadge.classList.add("btn", "rounded-pill", "btn-sm");

	//update the table with received values
	cellTimeStamp.innerText = reading.time;
	cellDescription.innerText = `Temperature Value: ${reading.temperature}`;

	if (reading.temperature > 40) {
		statusBadge.innerText = "Alert"
		statusBadge.classList.add("btn-danger");
		cellEvent.innerText = "New reading recorded";
	}
	else if (reading.temperature > 30) {
		statusBadge.innerText = "Warning"
		statusBadge.classList.add("btn-warning");
		cellEvent.innerText = "New reading recorded";
	}
	else {
		statusBadge.innerText = "Normal"
		statusBadge.classList.add("btn-success");
		cellEvent.innerText = "New reading recorded";
	}

	cellStatus.appendChild(statusBadge);
}

export function deleteAllRowsFromTable(targetTableElement) {
	targetTableElement.innerHTML = "";
}
