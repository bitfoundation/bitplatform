// Browser-side helpers for the demo shell. They live in a plain script (not a module) because the
// components call them by name through IJSRuntime.InvokeAsync, and the file is loaded from
// App.razor before blazor.web.js so every function exists by the time the WebAssembly runtime
// hydrates the prerendered markup.
//
// Nothing here animates anything. Every moving thing on this site is Bit.Bmotion's job, and a
// second animation engine hiding in the chrome would be the demo arguing against its own point.
// What this file does is the handful of things a component cannot: read the document, listen to
// the window, and talk to the clipboard.

// ── Theme ────────────────────────────────────────────────────────────────────
//
// The stylesheets key off the data-theme attribute on <html>. This half of the file is also
// inlined into the <head> by App.razor and runs before the first paint, so the stored theme is
// applied to the document rather than swapped in afterwards - which would show the reader a flash
// of the wrong one.

window.blTheme = {
    KEY: 'bl-theme',

    // The stored choice if there is one, otherwise whatever the operating system asks for. Reading
    // localStorage can throw outright (a browser with site data blocked), so a failure here falls
    // through to the system preference rather than taking the page down with it.
    preferred() {
        let stored = null;
        try { stored = localStorage.getItem(window.blTheme.KEY); } catch { /* no stored preference */ }
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
        window.blTheme.apply(theme);
        try { localStorage.setItem(window.blTheme.KEY, theme); } catch { /* preference is not persisted */ }
    },
};

window.blToggleTheme = function () {
    const next = window.blTheme.current() === 'dark' ? 'light' : 'dark';
    window.blTheme.set(next);
    return next;
};

window.blTheme.apply(window.blTheme.preferred());

// ── Layout metrics ───────────────────────────────────────────────────────────
//
// The header's height and the gap under it are tokens in theme.css. Everything that has to clear
// the header reads them from there rather than repeating the number, so the CSS and this file can
// only ever agree.

window.blMetrics = {
    rem(name, fallback) {
        const root = getComputedStyle(document.documentElement);
        const value = parseFloat(root.getPropertyValue(name));
        return (isNaN(value) ? fallback : value) * parseFloat(root.fontSize || '16');
    },

    headerOffset() {
        return window.blMetrics.rem('--bl-layout-header', 3.5)
             + window.blMetrics.rem('--bl-layout-header-gap', 1.5);
    },
};

// ── Header ───────────────────────────────────────────────────────────────────
//
// The header is a translucent pane. It grows a shadow the moment there is something scrolled
// underneath it to lift away from, and loses it again at the top of the page - so the elevation
// says "there is more above" rather than being decoration that is always on.

window.blHeader = {
    onScroll: null,

    update() {
        const header = document.querySelector('.app-header');
        if (!header) return;
        header.setAttribute('data-scrolled', window.scrollY > 4 ? 'true' : 'false');
    },
};

(function watchHeader() {
    let queued = false;
    window.blHeader.onScroll = function () {
        if (queued) return;
        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            window.blHeader.update();
        });
    };

    window.addEventListener('scroll', window.blHeader.onScroll, { passive: true });
    document.addEventListener('DOMContentLoaded', window.blHeader.update);
    window.blHeader.update();
})();

// ── "On this page" rail ──────────────────────────────────────────────────────
//
// The rail is built from the section headings actually present in the rendered page, so a demo
// page earns its entries by having sections rather than by maintaining a second list of them.

window.blSections = {
    ids: [],
    dotNetRef: null,
    activeId: null,
    onScroll: null,

    // The last heading that has passed under the header is the one being read. Falls back to the
    // first entry so a page scrolled to the very top still marks its opening section.
    update() {
        const offset = window.blMetrics.headerOffset();
        let current = window.blSections.ids[0] || null;

        for (const id of window.blSections.ids) {
            const el = document.getElementById(id);
            if (!el) continue;
            if (el.getBoundingClientRect().top - offset <= 1) current = id;
        }

        if (current === window.blSections.activeId) return;

        window.blSections.activeId = current;
        window.blSections.dotNetRef?.invokeMethodAsync('SetActiveSection', current);
    },
};

// Returns the section headings of the current page, giving any that lack one a stable id derived
// from its text so the rail's links survive a re-render - and hanging a link on each, so a reader
// can send a colleague the one section that answers their question. A section is addressable for
// exactly the same reason it is listed in the rail, which is why both happen here.
window.blCollectSections = function (anchorIcon) {
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

        const title = (h.textContent || '').trim();
        items.push({ id, title });

        // The icon's geometry is handed in by the caller rather than written out here: it lives in
        // Shared/Icons.cs with the rest of the set, and a second copy in this file is a copy that
        // would never be updated alongside it. Without one the anchor is simply not hung - it is a
        // shortcut to an id that the heading has either way.
        if (anchorIcon && !h.querySelector('.heading-anchor')) {
            const anchor = document.createElement('a');
            anchor.className = 'heading-anchor';
            anchor.href = `#${id}`;
            anchor.setAttribute('aria-label', `Link to "${title}"`);
            anchor.innerHTML = '<svg class="icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">'
                + anchorIcon + '</svg>';
            h.prepend(anchor);
        }
    });

    return items;
};

// The caller has just collected the sections to build the rail from, so it hands the ids straight
// over rather than making this walk the same headings a second time. Collecting them here is only
// the fallback for a caller that has not.
window.blObserveSections = function (dotNetRef, ids) {
    window.blDisposeSections();

    window.blSections.dotNetRef = dotNetRef;
    window.blSections.ids = ids ?? window.blCollectSections().map(i => i.id);
    window.blSections.activeId = null;

    // A passive listener with the work deferred to the next frame: scroll fires far more often than
    // the rail can meaningfully change, and this is running alongside the animations the page
    // exists to demonstrate.
    let queued = false;
    window.blSections.onScroll = function () {
        if (queued) return;
        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            window.blSections.update();
        });
    };

    window.addEventListener('scroll', window.blSections.onScroll, { passive: true });
    window.addEventListener('resize', window.blSections.onScroll, { passive: true });
    window.blSections.update();
};

window.blDisposeSections = function () {
    if (window.blSections.onScroll) {
        window.removeEventListener('scroll', window.blSections.onScroll);
        window.removeEventListener('resize', window.blSections.onScroll);
    }

    window.blSections.onScroll = null;
    window.blSections.dotNetRef = null;
    window.blSections.ids = [];
    window.blSections.activeId = null;
};

// scrollIntoView would put the heading under the sticky header, so the position is computed instead
// and the header's height taken off it. The CSS scroll-padding covers the browser's own fragment
// navigation; this covers the rail's.
window.blScrollToSection = function (id) {
    const el = document.getElementById(id);
    if (!el) return;

    const top = el.getBoundingClientRect().top + window.scrollY - window.blMetrics.headerOffset();
    window.scrollTo({ top, behavior: 'smooth' });
};

window.blScrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

// ── Keyboard shortcuts and the command palette ───────────────────────────────
//
// One listener, on the document, for the whole site. It owns exactly two things: the shortcut that
// opens the palette, and - while the palette is open - the four keys that dialog needs. Everything
// else falls straight through, which is why this is here rather than in an @onkeydown handler: a
// Blazor handler cannot decide per event whether to preventDefault, so it would have to suppress
// either every key or none of them, and both break typing into the palette's own input.

window.blShortcuts = {
    dotNetRef: null,
    paletteRef: null,
    paletteOpen: false,
    onKeyDown: null,
    returnFocusTo: null,

    isMac() {
        const platform = navigator.userAgentData?.platform || navigator.platform || '';
        return /mac|iphone|ipad|ipod/i.test(platform);
    },

    // A key pressed while the reader is typing somewhere is theirs, not ours - with the single
    // exception of the palette shortcut, which is a chord nothing else claims.
    isTyping(target) {
        if (!target) return false;
        if (target.isContentEditable) return true;
        return /^(input|textarea|select)$/i.test(target.tagName || '');
    },
};

/// Installs the listener and returns the label for the header's keycap, so the shortcut and the
/// key it advertises are decided in the same place.
window.blRegisterShortcuts = function (dotNetRef) {
    window.blUnregisterShortcuts();

    window.blShortcuts.dotNetRef = dotNetRef;

    window.blShortcuts.onKeyDown = function (e) {
        if ((e.ctrlKey || e.metaKey) && !e.altKey && (e.key === 'k' || e.key === 'K')) {
            e.preventDefault();
            window.blShortcuts.dotNetRef?.invokeMethodAsync('OnSearchShortcut');
            return;
        }

        // The bare slash is the other search shortcut readers reach for, but only when they are
        // not already typing - otherwise it would eat the character.
        if (e.key === '/' && !e.ctrlKey && !e.metaKey && !e.altKey
            && !window.blShortcuts.paletteOpen && !window.blShortcuts.isTyping(e.target)) {
            e.preventDefault();
            window.blShortcuts.dotNetRef?.invokeMethodAsync('OnSearchShortcut');
            return;
        }

        if (!window.blShortcuts.paletteOpen) return;

        const keys = { ArrowDown: 'down', ArrowUp: 'up', Enter: 'enter', Escape: 'close' };
        const action = keys[e.key];
        if (!action) return;

        e.preventDefault();
        window.blShortcuts.paletteRef?.invokeMethodAsync('OnPaletteKey', action);
    };

    document.addEventListener('keydown', window.blShortcuts.onKeyDown);

    return window.blShortcuts.isMac() ? '⌘ K' : 'Ctrl K';
};

window.blUnregisterShortcuts = function () {
    if (window.blShortcuts.onKeyDown) {
        document.removeEventListener('keydown', window.blShortcuts.onKeyDown);
    }

    window.blShortcuts.onKeyDown = null;
    window.blShortcuts.dotNetRef = null;
};

// Called by the palette as it opens and closes. It takes the focus on the way in and gives it back
// on the way out - a dialog that leaves the reader's place behind is a dialog that costs more to
// dismiss than it did to open - and holds the page still underneath it while it is up.
window.blSetPaletteOpen = function (open, paletteRef) {
    window.blShortcuts.paletteOpen = !!open;
    window.blShortcuts.paletteRef = open ? paletteRef : null;

    if (open) {
        window.blShortcuts.returnFocusTo = document.activeElement;
        document.body.style.overflow = 'hidden';

        // The input does not exist yet: this runs from the component's parameter pass, which is
        // before the render that creates it. Two frames is enough for Blazor to have applied the
        // edit, and each attempt is cheap.
        let attempts = 0;
        const focus = () => {
            const input = document.getElementById('palette-input');
            if (input) { input.focus(); return; }
            if (attempts++ < 10) requestAnimationFrame(focus);
        };
        requestAnimationFrame(focus);
        return;
    }

    document.body.style.overflow = '';

    const back = window.blShortcuts.returnFocusTo;
    window.blShortcuts.returnFocusTo = null;
    // Not if the reader has already moved on - navigating away from the palette puts focus where
    // the new page wants it, and dragging it back to a header button would undo that.
    if (back && document.body.contains(back) && document.activeElement === document.body) {
        back.focus();
    }
};

// The selection can move past either end of the visible list, and a selection nobody can see is
// the same as no selection at all.
window.blScrollPaletteActive = function () {
    const active = document.querySelector('.palette-item.active');
    active?.scrollIntoView({ block: 'nearest' });
};

// ── Clipboard ────────────────────────────────────────────────────────────────
//
// Backs the "Copy" buttons of Shared/CodeSnippet.razor and the landing page's install line.

window.blCopyToClipboard = async function (text) {
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

// ── Triggers for the instrumentation pages ──────────────────────────────────
// Each one does the thing the browser script instruments - throws, rejects, fetches, blocks the main
// thread, writes to the console - exactly as app code would, so what the reader then sees on the
// Live telemetry feed is what Belemetry records from a real app, not a simulation of it.
window.blDemo = {
    // Thrown from a timer so it reaches window.onerror as an uncaught error rather than being
    // caught by the .NET interop call that triggered it.
    throwError: function (message) {
        setTimeout(function () { throw new TypeError(message || 'A deliberate error from the demo page'); }, 0);
    },

    rejectPromise: function (message) {
        Promise.reject(new Error(message || 'A deliberate unhandled rejection from the demo page'));
    },

    // Plain fetch, no Blazor HttpClient: the script's fetch instrumentation records a client span
    // and, for same-origin targets, adds a traceparent header the server answers with.
    fetch: async function (url) {
        const started = performance.now();
        try {
            const response = await fetch(url, { cache: 'no-store' });
            const text = await response.text();
            return { status: response.status, body: text.slice(0, 2000), ms: Math.round(performance.now() - started) };
        } catch (e) {
            return { status: 0, body: String(e), ms: Math.round(performance.now() - started) };
        }
    },

    xhr: function (url) {
        return new Promise(function (resolve) {
            const started = performance.now();
            const request = new XMLHttpRequest();
            request.open('GET', url);
            request.onloadend = function () {
                resolve({ status: request.status, body: (request.responseText || '').slice(0, 2000), ms: Math.round(performance.now() - started) });
            };
            request.send();
        });
    },

    // Blocks the main thread for longer than 50 ms, which is what the Long Tasks API reports.
    longTask: function (ms) {
        const end = performance.now() + (ms || 120);
        while (performance.now() < end) { /* busy */ }
    },

    console: function (level, message) {
        (console[level] || console.log).call(console, message);
    },

    // A layout shift for CLS: an element pushed into the flow above content the reader sees.
    shift: function (id) {
        const target = document.getElementById(id);
        if (!target) return;
        const spacer = document.createElement('div');
        spacer.style.height = '48px';
        target.prepend(spacer);
        setTimeout(function () { spacer.remove(); }, 1500);
    },

    context: function () {
        return window.BitBelemetry ? window.BitBelemetry.getContext() : null;
    },
};
