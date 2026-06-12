/*  Add Event Listeners for Quick filters*/
document.getElementById("last1HourSmokeBtn")
		.addEventListener("click", () => loadLastHoursSmoke(1));

document.getElementById("last24HoursSmokeBtn")
		.addEventListener("click", () => loadLastHoursSmoke(24));

document.getElementById("last7DaysSmokeBtn")
		.addEventListener("click", () => loadLastDaysSmoke(7));

// adjust the tracking flag
window.historicalMode = false;

async function loadChartData(startDate = "", endDate = "") {
	//If user selected dates, switch to historical mode. 
	//We set it to && because everytime the method is called, both values are passed
	window.historicalMode = startDate !== "" && endDate !== "";
	const url = `/SmokeHistory/GetSmokeDataHistory` +
		`?shelterCode=${window.shelterCode}` +
		`&startDate=${startDate}` + `&endDate=${endDate}`;
	console.log(url);
	try {
		const response = await fetch(url);
		const data = await response.json();

		//Clear existing table data
		ClearHistoryTable();

		//Load new data
		const historyTableBody = document.querySelector("#smokeTable tbody");
		if (!historyTableBody) return;

		
		data.forEach(item => {
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
			cellTimeStamp.innerText = item.time;
			cellSensorName.innerText = "Main Sensor";
			if (item.result) {
				cellEvent.innerText = "Smoke Detected";

				statusBadge.innerText = "Detected";
				statusBadge.classList.add("btn-danger");


			} else {
				cellEvent.innerText = " No Smoke Detected";

				statusBadge.innerText = "Normal";
				statusBadge.classList.add("btn-success");
			}
			cellStatus.appendChild(statusBadge);
		});

		//window.tempChart.update();
	}
	catch (err) {
		console.error("Table loading error, Confirm if the url is the correct one. :", err);
	}
}

async function loadLastHoursSmoke(hours) {
	window.historicalMode = true;
	const endDate = new Date();
	const startDate = new Date();
	startDate.setHours(endDate.getHours() - hours);
	await loadChartData(startDate.toISOString(), endDate.toISOString());
}

async function loadLastDaysSmoke(days) {
	window.historicalMode = true;
	const endDate = new Date();
	const startDate = new Date()
	startDate.setDate(endDate.getDate() - days);
	await loadChartData(startDate.toISOString(), endDate.toISOString());
}

document.getElementById("showHistorySmokeBtn").addEventListener("click", () => {
	const startVal = document.getElementById("startSmokeDate").value;
	const endVal = document.getElementById("endSmokeDate").value;

	if (!startVal || !endVal) {
		alert("Please select both a start and end date.");
		return;
	}

	// Convert date strings to ISO datetime strings (start of day / end of day)
	const startDate = new Date(startVal)
	startDate.setHours(0, 0, 0, 0);

	const endDate = new Date(endVal)
	endDate.setHours(23, 59, 59, 999);

	loadChartData(startDate.toISOString(), endDate.toISOString());
});

/* =====================================================
LOAD INITIAL HISTORY
========================================================*/
//loadChartData();

/* =====================================================
RELOAD CHART WHEN DATES CHANGE
========================================================*/
const startDateInput =
	document.getElementById("startDate");

const endDateInput =
	document.getElementById("endDate");

function ClearHistoryTable() {
	const historyTable = document.querySelector("#smokeTable tbody")
	if (!historyTable) return;

	historyTable.innerHTML = "";
}

window.returntoLive = function () {
	window.historicalMode = false;
	ClearHistoryTable();
}
