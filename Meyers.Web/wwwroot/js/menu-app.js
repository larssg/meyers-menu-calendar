// Constants
const TOAST_DURATION = 1800;
const AUTO_REFRESH_INTERVAL = 3600000; // 1 hour
const BASE_URL = window.location.origin;

// Current state
let currentMenuMode = 'simple';
let menuData = null;

// Helper functions
function $(id) {
    return document.getElementById(id);
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        const existing = $('copy-toast');
        if (existing) existing.remove();

        const toast = document.createElement('div');
        toast.id = 'copy-toast';
        toast.textContent = 'Copied to clipboard';
        toast.className = 'fixed bottom-6 left-1/2 -translate-x-1/2 z-50 px-4 py-2 ' +
            'rounded-lg bg-teal-700 text-white text-sm shadow';
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), TOAST_DURATION);
    });
}

function formatDate(dateStr) {
    const date = new Date(dateStr + 'T12:00:00');
    return {
        dayName: date.toLocaleDateString('en-US', {weekday: 'short'}),
        dayNumber: date.getDate(),
        monthName: date.toLocaleDateString('en-US', {month: 'short'})
    };
}

// Menu Mode Toggle
function toggleMenuMode() {
    const simpleRadio = document.querySelector('input[name="menuMode"][value="simple"]');
    const customRadio = document.querySelector('input[name="menuMode"][value="custom"]');
    const simpleMode = $('simpleMode');
    const customMode = $('customMode');

    if (simpleRadio.checked) {
        currentMenuMode = 'simple';
        simpleMode.classList.remove('hidden');
        customMode.classList.add('hidden');
    } else if (customRadio.checked) {
        currentMenuMode = 'custom';
        simpleMode.classList.add('hidden');
        customMode.classList.remove('hidden');
    }

    updateCalendarUrl();
}

// Calendar URL Generation
function updateCalendarUrl() {
    const baseUrl = BASE_URL.replace(/^http:/, 'https:');
    let calendarUrl = '';

    if (currentMenuMode === 'simple') {
        const simpleSelect = $('simpleMenuSelect');
        if (simpleSelect && simpleSelect.selectedOptions.length > 0) {
            const selectedOption = simpleSelect.selectedOptions[0];
            const slug = selectedOption.getAttribute('data-slug');
            if (slug) {
                calendarUrl = `${baseUrl}/calendar/${slug}.ics`;
            }
        }
    } else {
        // Custom mode
        const daySelects = document.querySelectorAll('.custom-day-select');
        const config = [];

        daySelects.forEach(select => {
            const dayOfWeek = parseInt(select.getAttribute('data-day'));
            const menuTypeId = select.value;

            if (menuTypeId && menuTypeId !== '') {
                const dayChar = getDayChar(dayOfWeek);
                if (dayChar) {
                    config.push(dayChar + menuTypeId);
                }
            }
        });

        if (config.length > 0) {
            const configString = config.join('');
            calendarUrl = `${baseUrl}/calendar/custom/${configString}.ics`;
        }
    }

    // Add alarm parameter if checkbox is checked
    const alarmCheckbox = $('alarmCheckbox');
    if (calendarUrl && alarmCheckbox && alarmCheckbox.checked) {
        const separator = calendarUrl.includes('?') ? '&' : '?';
        calendarUrl += `${separator}alarm=true`;
    }

    const input = $('calendarUrl');
    if (input) {
        input.value = calendarUrl;
    }

    // Also update the preview whenever URL changes
    updateWeeklyPreview();
}

function getDayChar(dayOfWeek) {
    // DayOfWeek enum: Sunday=0, Monday=1, Tuesday=2, Wednesday=3, Thursday=4, Friday=5, Saturday=6
    switch (dayOfWeek) {
        case 1:
            return 'M'; // Monday
        case 2:
            return 'T'; // Tuesday
        case 3:
            return 'W'; // Wednesday
        case 4:
            return 'R'; // Thursday
        case 5:
            return 'F'; // Friday
        default:
            return null;
    }
}

// Weekly Preview
function updateWeeklyPreview() {
    const previewContainer = $('weeklyPreview');
    if (!previewContainer || !menuData) return;

    const startDate = new Date(menuData.startDate + 'T12:00:00');
    const weekDays = [];

    // Generate 7 days starting from today
    for (let i = 0; i < 7; i++) {
        const currentDate = new Date(startDate);
        currentDate.setDate(startDate.getDate() + i);
        weekDays.push(currentDate);
    }

    previewContainer.innerHTML = '';

    weekDays.forEach((date, index) => {
        const dateStr = date.toISOString().split('T')[0];
        const dayInfo = formatDate(dateStr);
        const isWeekend = date.getDay() === 0 || date.getDay() === 6; // Sunday or Saturday

        let menuEntry = null;
        let menuTypeName = '';

        if (!isWeekend) {
            // Get menu entry based on current mode
            if (currentMenuMode === 'simple') {
                const simpleSelect = $('simpleMenuSelect');
                if (simpleSelect && simpleSelect.value) {
                    const menuTypeId = simpleSelect.value;
                    if (menuData.weeklyData[menuTypeId] && menuData.weeklyData[menuTypeId][dateStr]) {
                        menuEntry = menuData.weeklyData[menuTypeId][dateStr];
                        const menuType = menuData.menuTypes.find(mt => mt.id == menuTypeId);
                        menuTypeName = menuType ? menuType.name : '';
                    }
                }
            } else {
                // Custom mode - find which menu type is selected for this day
                const dayOfWeek = date.getDay();
                const daySelect = document.querySelector(`select[data-day="${dayOfWeek}"]`);
                if (daySelect && daySelect.value) {
                    const menuTypeId = daySelect.value;
                    if (menuData.weeklyData[menuTypeId] && menuData.weeklyData[menuTypeId][dateStr]) {
                        menuEntry = menuData.weeklyData[menuTypeId][dateStr];
                        const menuType = menuData.menuTypes.find(mt => mt.id == menuTypeId);
                        menuTypeName = menuType ? menuType.name : '';
                    }
                }
            }
        }

        const dayElement = document.createElement('div');
        dayElement.className = `p-3 rounded-lg border ${
            isWeekend
                ? 'bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700'
                : 'bg-white dark:bg-slate-800 border-slate-200 dark:border-slate-700'
        } ${index === 0 ? 'ring-2 ring-teal-500' : ''}`;

        dayElement.innerHTML = `
            <div class="text-center mb-2">
                <div class="text-xs font-medium text-slate-600 dark:text-slate-400">${dayInfo.dayName}</div>
                <div class="text-lg font-semibold text-slate-900 dark:text-slate-100">${dayInfo.dayNumber}</div>
                <div class="text-xs text-slate-500 dark:text-slate-500">${dayInfo.monthName}</div>
            </div>
            ${isWeekend ?
            '<div class="text-center text-xs text-gray-500 dark:text-gray-400">Weekend</div>' :
            menuEntry ?
                `<div class="text-center">
                        <div class="text-xs font-medium text-teal-600 dark:text-teal-400 mb-1">${menuTypeName}</div>
                        <div class="text-xs text-slate-700 dark:text-slate-300 line-clamp-3" title="${menuEntry.title}">${menuEntry.title}</div>
                    </div>` :
                '<div class="text-center text-xs text-slate-500 dark:text-slate-400">No menu</div>'
        }
        `;

        previewContainer.appendChild(dayElement);
    });
}

// Auto-refresh functionality
let refreshInterval;

function startAutoRefresh() {
    refreshInterval = setInterval(() => {
        console.log('Auto-refreshing menu data...');
        window.location.reload();
    }, AUTO_REFRESH_INTERVAL);
}

function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

// Event listeners
document.addEventListener('DOMContentLoaded', () => {
    // Load menu data from global variable
    if (window.menuData) {
        menuData = window.menuData;
    }

    // Initialize interface
    updateCalendarUrl();

    startAutoRefresh();
});

document.addEventListener('visibilitychange', () => {
    if (document.hidden) stopAutoRefresh();
    else startAutoRefresh();
});

window.addEventListener('beforeunload', stopAutoRefresh);