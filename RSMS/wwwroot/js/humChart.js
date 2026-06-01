document.addEventListener("DOMContentLoaded", () => {

	const ctx = document.getElementById("humChart");

	window.humChart = new Chart(ctx, {

		type: "line",

		data: {

			labels: [],

			datasets: [{

				label: "",

				data: [],

				borderColor: "#5a6acf",

				tension: 0.4,

				fill: false
			}]
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
					//text: "Humidity History	
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

						text: "Humidity (%)"
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
=========================================*/
	window.historicalMode = false;

	async function loadChartData(startDate = "", endDate = "") {
		//If user selected dates, switch to historical mode
		window.historicalMode =
			startDate !== "" || endDate !== "";
		try {
			const url = `/HumHistory/GetHumidityHistory` +
				`?shelterCode=${window.shelterCode}` +
				`&startDate=${startDate}` + `&endDate=${endDate}`;

			console.log(url);
			const response = await fetch(url);

			const data = await response.json();

			//Clear existing Chart data
			window.humChart.data.labels = [];
			window.humChart.data.datasets[0].data = [];

			//Load new data

			data.forEach(item => {
				window.humChart.data.labels.push(item.time);
				window.humChart.data.datasets[0].data.push(item.humidity);
			});
			window.humChart.update();
		}
		catch (err) {

			console.error(
				"Chart loading error:",
				err
			);
		}
	}

	function formatDate(date) {
		return date.toISOString().split('T')[0];
	}

	async function loadLastHours(hours) {
		window.historicalMode = true;
		const endDate = new Date();
		const startDate = new Date();
		startDate.setHours(endDate.getHours() - hours);
		await loadChartData(startDate.toISOString(), endDate.toISOString());
	}

	async function loadLastDays(days) {
		window.historicalMode = true;
		const endDate = new Date();
		const startDate = new Date();
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
		const startDate = new Date(startVal);
		startDate.setHours(0, 0, 0, 0);

		const endDate = new Date(endVal);
		endDate.setHours(23, 59, 59, 999);

		loadChartData(startDate.toISOString(), endDate.toISOString());
	});
	/* =====================================================
	LOAD INITIAL HISTORY
=====================================================*/

	loadChartData();

	/* =====================================================
	RELOAD CHART WHEN DATES CHANGE
=====================================================*/
	const startDateInput =
		document.getElementById("startDate");

	const endDateInput =
		document.getElementById("endDate");

	window.returntoLive =  async function () {
		window.historicalMode = false;
		window.humChart.data.labels = [];
		window.humChart.data.datasets[0].data = [];
  
	}
});
