//const { read } = require("@popperjs/core");

//import the reusable table script
import { addRowToTable, deleteAllRowsFromTable } from './readingsTable.js';

document.addEventListener("DOMContentLoaded", () => { 

	const temperatureChart = document.getElementById("tempChart");

	window.tempChart = new Chart(temperatureChart, {
		type: "line",
		data: {
			labels: [],
			datasets: [{label:"",data: [],borderColor: "#5a6acf",tension: 0.4,fill: false}]
		},
		options: {
			responsive: true,
			maintainAspectRatio: false,
			plugins: {
				legend: {
					display: false,
				},
				title: {
					display: false,
					//text: "Temperature History"
				}
			},
			scales: {
				x: {
					title: {
						display: true,
						text: ""
					},
					ticks: {
						//autoSkip: true,
						maxRotation: 45,
						//maxTicksLimit: 10,
						minRotation: 45
					}
				},
				y: {
					beginAtZero: false,
					title: {
						display: true,
						text: "Temperature (°C)"
					}
				}
			}
		}
	});

	/*  Add Event Listeners for Quick filters*/
	document.getElementById("last1HourBtn").
	addEventListener("click", () => loadLastHours(1));

	document.getElementById("last24HoursBtn").
	addEventListener("click", () => loadLastHours(24));

	document.getElementById("last7DaysBtn").
	addEventListener("click", () => loadLastDays(7));

	/* =========================================
	FLAG
	============================================*/
	window.historicalMode = false;

	async function loadChartData(startDate = "", endDate = "")
	{
		//If user selected dates, switch to historical mode. 
		//We set it to && because everytime the method is called, both values are passed
		window.historicalMode = startDate !== "" && endDate !== "";
		const url = `/TempHistory/GetTemperatureHistory` +
			`?shelterCode=${window.shelterCode}` +
			`&startDate=${startDate}` + `&endDate=${endDate}`;
		console.log(url);
		try
		{
			const response = await fetch(url);
			const data = await response.json();

			if (data) { 
				//Clear existing Chart data
				window.tempChart.data.labels = [];
				window.tempChart.data.datasets[0].data = [];

				//find table and clear all its contents
				const historyTableBody = document.querySelector("#tempTable tbody");
				deleteAllRowsFromTable(historyTableBody);

				//Load new data into chart and table
				data.forEach(item => {
					window.tempChart.data.labels.push(item.time);
					window.tempChart.data.datasets[0].data.push(item.temperature);

					addRowToTable(historyTableBody, item);
				});

				window.tempChart.update();
			} else {
				console.log("No history data received.")
			}
		}
		catch (err) {
			console.error("Chart loading error, Confirm the if the url is the correct one. :",err);
			}
	}

	async function loadLastHours(hours){
		window.historicalMode = true;
		const endDate = new Date();
		const startDate = new Date();
		startDate.setHours(endDate.getHours() - hours);
		await loadChartData(startDate.toISOString(), endDate.toISOString());
	}

	async function loadLastDays(days){
		window.historicalMode = true;
		const endDate = new Date();
		const startDate = new Date()
		startDate.setDate(endDate.getDate() - days);
		await loadChartData(startDate.toISOString(), endDate.toISOString());
	}

	document.getElementById("showHistoryBtn").addEventListener("click", () => {
		const startVal = document.getElementById("startDate").value;
		const endVal = document.getElementById("endDate").value;

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
	loadChartData();

	/* =====================================================
	RELOAD CHART WHEN DATES CHANGE
	========================================================*/
	const startDateInput =
	document.getElementById("startDate");

	const endDateInput =
	document.getElementById("endDate");

	window.returntoLive = function()
	{
		window.historicalMode = false;
		//clear chart and table
		window.tempChart.data.labels = [];
		window.tempChart.data.datasets[0].data = [];

		const historyTableBody = document.querySelector("#tempTable tbody");
		deleteAllRowsFromTable(historyTableBody);
	}
});
