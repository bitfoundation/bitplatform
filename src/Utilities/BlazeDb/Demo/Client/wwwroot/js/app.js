// First-party helpers for the BlazeDb demo. No third-party code.

const THEME_KEY = 'blazedb-theme';

export function getTheme() {
    return document.documentElement.dataset.theme === 'light' ? 'light' : 'dark';
}

export function setTheme(theme) {
    const value = theme === 'light' ? 'light' : 'dark';
    document.documentElement.dataset.theme = value;
    const meta = document.querySelector('meta[name="theme-color"]');
    if (meta) {
        meta.setAttribute('content', value === 'light' ? '#f7f8fa' : '#08090d');
    }
    try {
        localStorage.setItem(THEME_KEY, value);
    } catch (e) {
        // Storage can be blocked; the theme still applies for this session.
    }
    return value;
}

export async function copyText(text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch (e) {
        // Fall through to the textarea approach below.
    }
    try {
        const area = document.createElement('textarea');
        area.value = text;
        area.setAttribute('readonly', '');
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        const ok = document.execCommand('copy');
        document.body.removeChild(area);
        return ok;
    } catch (e) {
        return false;
    }
}

export function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'auto' });
}
