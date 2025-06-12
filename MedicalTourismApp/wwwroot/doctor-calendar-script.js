const calendarBody = document.getElementById("calendarBody");
const calendarMonthYear = document.getElementById("calendarMonthYear");

let currentDate = new Date();

function renderCalendar(date) {
    calendarBody.innerHTML = '';
    const year = date.getFullYear();
    const month = date.getMonth();

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    calendarMonthYear.textContent = `${date.toLocaleString('default', { month: 'long' }).toUpperCase()} (${year})`;

    let day = 1;
    for (let i = 0; i < 6; i++) {
        let row = document.createElement("tr");

        for (let j = 0; j < 7; j++) {
            let cell = document.createElement("td");

            if (i === 0 && j < firstDay) {
                cell.textContent = '';
            } else if (day > daysInMonth) {
                cell.textContent = '';
            } else {
                cell.textContent = day;

                // Example: highlight some dates
                if ([7, 11, 16, 21, 23, 29].includes(day)) {
                    cell.classList.add("active");
                }

                // Highlight today
                const today = new Date();
                if (day === today.getDate() && month === today.getMonth() && year === today.getFullYear()) {
                    cell.style.fontWeight = 'bold';
                    cell.style.color = 'red';
                }

                day++;
            }

            row.appendChild(cell);
        }

        calendarBody.appendChild(row);
    }
}

document.getElementById("prevMonth").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar(currentDate);
});

document.getElementById("nextMonth").addEventListener("click", () => {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar(currentDate);
});

renderCalendar(currentDate);
