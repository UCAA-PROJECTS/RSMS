let tempChart;

// 1. Initialize the Chart ONCE when the page loads
document.addEventListener("DOMContentLoaded", function () {
    const ctx = document.getElementById("tempChart").getContext('2d');

    // Assign the chart to our global variable
    tempChart = new Chart(ctx, {
        type: "line",
        data: {
            labels: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            datasets: [{
                label: "Temperature",
                data: [26, 29, 28, 27, 25, 24, 23],
                borderColor: "#5a6acf",
                tension: 0.4,
                fill: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: { ticks: { autoSkip: false } }
            }
        }
    });

    // 2. Attach Click Listeners to Buttons
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            // UI Update
            const activeBtn = document.querySelector('.filter-btn.active');
            if (activeBtn) activeBtn.classList.remove('active');
            this.classList.add('active');
            
            // Logic Update
            const range = this.getAttribute('data-range');
            updateChartData(range); 
        });
    });
});

// 3. The Logic to update labels and data
function updateChartData(range) {
    const dataConfig = {
        'hourly': {
            labels: ["10:00", "11:00", "12:00", "13:00", "14:00", "15:00"],
            data: [22, 23, 24.5, 25, 24.8, 23.5],
            autoSkip: true
        },
        '7days': {
            labels: ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"],
            data: [21, 24, 23, 25, 24.8, 26, 25],
            autoSkip: false
        },
        '14days': {
            labels: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14"],
            data: [21, 22, 24, 23, 25, 26, 25, 24, 23, 22, 21, 20, 22, 23],
            autoSkip: false
        }
    };

    const config = dataConfig[range];

    // Check if chart exists and we have a config for the button clicked
    if (tempChart && config) {
        tempChart.data.labels = config.labels;
        tempChart.data.datasets[0].data = config.data;
        tempChart.options.scales.x.ticks.autoSkip = config.autoSkip;
        tempChart.update();
    }
}

// 4. Flatpickr Logic
flatpickr("#customRange", {
    mode: "range",
    maxDate: "today",
    onChange: function(selectedDates) {
        if (selectedDates.length === 2) {
            const diff = Math.ceil((selectedDates[1] - selectedDates[0]) / (1000 * 60 * 60 * 24));
            if (diff > 90) {
                alert("Please select a range within 90 days");
                this.clear();
            } else {
                // Here you would typically fetch data for the custom range
                console.log("Fetching data for " + diff + " days...");
            }
        }
    }
});
