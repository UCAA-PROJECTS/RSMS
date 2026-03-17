const ctx = document.getElementById("tempChart");

new Chart(ctx, {
	type: "line",
	data: {
		labels: [],
		datasets: [{
			label: "temperature",
			data: [],
			borderColor: "#5a6acf",
			tension: 0.4,
			fill: false
		}]
	},
	options: {
		plugins: {
			maintainAspectRatio: false,
			legend: { display: true }
		}
	}
});