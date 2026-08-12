// Browser-side helpers for the demo shell. They live in a plain script (not a module) because the
// components call them by name through IJSRuntime.InvokeAsync, and the file is loaded from
// App.razor before blazor.web.js so every function exists by the time the WebAssembly runtime
// hydrates the prerendered markup.

// ── Theme ────────────────────────────────────────────────────────────────────
//
// The stylesheets key off the data-theme attribute on <html>, exactly like the Bit.BlazorUI docs
// site. This half of the file is also inlined into the <head> by App.razor and runs before the
// first paint, so the stored theme is applied to the document rather than swapped in afterwards -
// which would show the reader a flash of the wrong one.

window.bmTheme = {
    KEY: 'bm-theme',

    // The stored choice if there is one, otherwise whatever the operating system asks for. Reading
    // localStorage can throw outright (a browser with site data blocked), so a failure here falls
    // through to the system preference rather than taking the page down with it.
    preferred() {
        let stored = null;
        try { stored = localStorage.getItem(window.bmTheme.KEY); } catch { /* no stored preference */ }
        if (stored === 'light' || stored === 'dark') return stored;
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    current() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    },

    apply(theme) {
        document.documentElement.setAttribute('data-theme', theme === 'dark' ? 'dark' : 'light');
    },

    set(theme) {
        window.bmTheme.apply(theme);
        try { localStorage.setItem(window.bmTheme.KEY, theme); } catch { /* preference is not persisted */ }
    },
};

window.bmToggleTheme = function () {
    const next = window.bmTheme.current() === 'dark' ? 'light' : 'dark';
    window.bmTheme.set(next);
    return next;
};

window.bmTheme.apply(window.bmTheme.preferred());

// ── "On this page" rail ──────────────────────────────────────────────────────
//
// The rail is built from the section headings actually present in the rendered page, so a demo
// page earns its entries by having sections rather than by maintaining a second list of them.

window.bmSections = {
    ids: [],
    dotNetRef: null,
    activeId: null,
    onScroll: null,

    // The offset the sticky header occupies, so "the section at the top of the screen" means the
    // one at the top of the part of the screen the reader can actually see. Read from the same
    // custom property the header sizes itself from, so the two can only agree.
    headerOffset() {
        const raw = getComputedStyle(document.documentElement).getPropertyValue('--demo-header-height');
        const rem = parseFloat(raw) || 4.0625;
        return rem * parseFloat(getComputedStyle(document.documentElement).fontSize || '16') + 24;
    },

    // The last heading that has passed under the header is the one being read. Falls back to the
    // first entry so a page scrolled to the very top still marks its opening section.
    update() {
        const offset = window.bmSections.headerOffset();
        let current = window.bmSections.ids[0] || null;

        for (const id of window.bmSections.ids) {
            const el = document.getElementById(id);
            if (!el) continue;
            if (el.getBoundingClientRect().top - offset <= 1) current = id;
        }

        if (current === window.bmSections.activeId) return;

        window.bmSections.activeId = current;
        window.bmSections.dotNetRef?.invokeMethodAsync('SetActiveSection', current);
    },
};

// Returns the section headings of the current page, giving any that lack one a stable id derived
// from its text so the rail's links survive a re-render.
window.bmCollectSections = function () {
    const headings = document.querySelectorAll('.page-container .demo-section > h2');
    const items = [];
    const used = new Set();

    headings.forEach((h, index) => {
        let id = h.id;
        if (!id) {
            const slug = (h.textContent || '').trim().toLowerCase()
                .replace(/[^a-z0-9]+/g, '-')
                .replace(/^-+|-+$/g, '');
            id = slug || `section-${index + 1}`;
            while (used.has(id)) id = `${id}-${index + 1}`;
            h.id = id;
        }
        used.add(id);
        items.push({ id, title: (h.textContent || '').trim() });
    });

    return items;
};

window.bmObserveSections = function (dotNetRef) {
    window.bmDisposeSections();

    window.bmSections.dotNetRef = dotNetRef;
    window.bmSections.ids = window.bmCollectSections().map(i => i.id);
    window.bmSections.activeId = null;

    // A passive listener with the work deferred to the next frame: scroll fires far more often than
    // the rail can meaningfully change, and this is running alongside the animations the page
    // exists to demonstrate.
    let queued = false;
    window.bmSections.onScroll = function () {
        if (queued) return;
        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            window.bmSections.update();
        });
    };

    window.addEventListener('scroll', window.bmSections.onScroll, { passive: true });
    window.addEventListener('resize', window.bmSections.onScroll, { passive: true });
    window.bmSections.update();
};

window.bmDisposeSections = function () {
    if (window.bmSections.onScroll) {
        window.removeEventListener('scroll', window.bmSections.onScroll);
        window.removeEventListener('resize', window.bmSections.onScroll);
    }

    window.bmSections.onScroll = null;
    window.bmSections.dotNetRef = null;
    window.bmSections.ids = [];
    window.bmSections.activeId = null;
};

// scrollIntoView would put the heading under the sticky header, so the position is computed instead
// and the header's height taken off it. The CSS scroll-margin on the headings covers the browser's
// own fragment navigation; this covers the rail's.
window.bmScrollToSection = function (id) {
    const el = document.getElementById(id);
    if (!el) return;

    const top = el.getBoundingClientRect().top + window.scrollY - window.bmSections.headerOffset();
    window.scrollTo({ top, behavior: 'smooth' });
};

window.bmScrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// ── Clipboard ────────────────────────────────────────────────────────────────
//
// Backs the "Copy" button of Shared/CodeSnippet.razor.

window.bmCopyToClipboard = async function (text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch { /* fall through to legacy path */ }
    const ta = document.createElement('textarea');
    try {
        ta.value = text;
        ta.style.position = 'fixed';
        ta.style.opacity = '0';
        document.body.appendChild(ta);
        ta.focus();
        ta.select();
        return document.execCommand('copy');
    } catch {
        return false;
    } finally {
        // Always remove the textarea, even if focus/select/execCommand throws.
        if (ta.parentNode) document.body.removeChild(ta);
    }
};
