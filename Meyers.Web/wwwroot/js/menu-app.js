// Constants
const TOAST_DURATION = 1800;
const AUTO_REFRESH_INTERVAL = 3600000; // 1 hour
const BASE_URL = window.location.origin;

const MENU_NAME_IDS = ['selectedMenuTypeName', 'heroMenuTypeName', 'previewMenuTypeName'];
const TAB_ACTIVE_CLASS = 'bg-teal-600 text-white border-teal-600';
const TAB_INACTIVE_CLASS = 'bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-700/60';

// Templates
const templates = {
    error: '<p class="text-slate-500 dark:text-slate-400 text-sm">Failed to load menu</p>',
    noMenu: '<p class="text-slate-500 dark:text-slate-400 text-sm">No menu available for {period}</p>',
    menuItem: (title, details) => {
        let html = '<div><p class="font-medium text-slate-900 dark:text-slate-100 text-base">' + 
                  escapeHtml(title) + '</p>';
        if (details) {
            html += '<p class="text-slate-600 dark:text-slate-300 mt-2 text-sm">' + 
                   escapeHtml(details) + '</p>';
        }
        return html + '</div>';
    }
};

// Helper functions
function $(id) {
    return document.getElementById(id);
}

function updateElements(ids, value) {
    ids.forEach(id => {
        const el = $(id);
        if (el) el.textContent = value;
    });
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
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

// Menu data management
function getMenuInfo(menuTypeId) {
    // Try to get from selector first
    const selector = $('menuTypeSelector');
    if (selector) {
        const option = selector.options[selector.selectedIndex];
        if (option) {
            return {
                slug: option.getAttribute('data-slug'),
                name: option.getAttribute('data-name')
            };
        }
    }
    
    // Fallback to tab if available
    const tab = $(`tab-${menuTypeId}`);
    if (tab) {
        return {
            slug: tab.getAttribute('data-slug'),
            name: tab.textContent.trim()
        };
    }
    
    return { slug: null, name: null };
}

function updateUI(menuTypeId, slug, name) {
    // Update calendar URL
    if (slug) {
        // Always use HTTPS for production URLs
        const baseUrl = BASE_URL.replace(/^http:/, 'https:');
        const calendarUrl = `${baseUrl}/calendar/${slug}.ics`;
        const input = $('calendarUrl');
        if (input) input.value = calendarUrl;
    }
    
    // Update menu names
    if (name) {
        updateElements(MENU_NAME_IDS, name);
    }
}

function updateMenuContent(containerId, data, period) {
    const container = $(containerId);
    if (!container) return;
    
    if (data) {
        container.innerHTML = templates.menuItem(data.title, data.details);
    } else {
        container.innerHTML = templates.noMenu.replace('{period}', period);
    }
}

async function loadMenuPreview(menuTypeId) {
    try {
        const response = await fetch(`/api/menu-preview/${menuTypeId}`);
        if (response.ok) {
            const data = await response.json();
            updateMenuContent('todayMenuContent', data.today, 'today');
            updateMenuContent('tomorrowMenuContent', data.tomorrow, 'tomorrow');
        } else {
            throw new Error('Failed to fetch menu preview');
        }
    } catch (error) {
        console.error('Failed to fetch menu preview:', error);
        const todayContent = $('todayMenuContent');
        const tomorrowContent = $('tomorrowMenuContent');
        if (todayContent) todayContent.innerHTML = templates.error;
        if (tomorrowContent) tomorrowContent.innerHTML = templates.error;
    }
}

async function updateMenuType(menuTypeId) {
    const { slug, name } = getMenuInfo(menuTypeId);
    updateUI(menuTypeId, slug, name);
    await loadMenuPreview(menuTypeId);
}

function selectMenuTab(menuTypeId, slug, name) {
    // Update tab styling
    const tablist = document.querySelector('[role="tablist"]');
    if (tablist) {
        tablist.querySelectorAll('[role="tab"]').forEach(tab => {
            const isActive = tab.id === `tab-${menuTypeId}`;
            tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
            tab.className = `px-4 py-2 text-sm font-medium transition-colors rounded-lg border border-slate-200 dark:border-slate-700 ${
                isActive ? TAB_ACTIVE_CLASS : TAB_INACTIVE_CLASS
            }`;
            
            if (isActive && slug && !tab.getAttribute('data-slug')) {
                tab.setAttribute('data-slug', slug);
            }
        });
    }
    
    // Update UI and load preview
    updateUI(menuTypeId, slug, name);
    updateMenuType(menuTypeId);
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
document.addEventListener('DOMContentLoaded', startAutoRefresh);
document.addEventListener('visibilitychange', () => {
    if (document.hidden) stopAutoRefresh();
    else startAutoRefresh();
});
window.addEventListener('beforeunload', stopAutoRefresh);