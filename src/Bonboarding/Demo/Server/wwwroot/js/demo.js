// Browser-side helpers for the demo shell. They live in a plain script (not a module) because the
// components call them by name through IJSRuntime, and the file is loaded from App.razor before
// blazor.web.js so every function exists by the time the app becomes interactive.
//
// Nothing here has anything to do with tours: every tour on this site is run by Bit.Bonboarding and
// its own script. What this file does is the handful of things the site's chrome needs and a
// component cannot do on its own: keep the theme, and listen to the window.

// ── Theme ────────────────────────────────────────────────────────────────────
//
// The stylesheets key off the data-theme attribute on <html>. The same decision is inlined into the
// <head> by App.razor and runs before the first paint, so the stored theme is applied to the
// document rather than swapped in afterwards - which would show the reader a flash of the wrong one.

window.boTheme = {
    // Deliberately outside the "bit-bonboarding-demo:" prefix the tours are stored under, so the
    // Events page's "Forget every tour" button forgets tours and not the reader's theme.
    KEY: 'bo-theme',

    // The stored choice if there is one, otherwise whatever the operating system asks for. Reading
    // localStorage can throw outright (a browser with site data blocked), so a failure here falls
    // through to the system preference rather than taking the page down with it.
    preferred() {
        let stored = null;
        try { stored = localStorage.getItem(window.boTheme.KEY); } catch { /* no stored preference */ }
        if (stored === 'light' || stored === 'dark') return stored;
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    current() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    },

    apply(theme) {
        const value = theme === 'dark' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', value);

        // The colour some browsers paint their own chrome in: the --bo-clr-bg-1 of each theme in
        // theme.css, which a meta tag cannot read a variable for.
        document.querySelector('meta[name="theme-color"]')?.setAttribute('content', value === 'dark' ? '#1F1F1F' : '#FFFFFF');
    },

    set(theme) {
        window.boTheme.apply(theme);
        try { localStorage.setItem(window.boTheme.KEY, theme); } catch { /* preference is not persisted */ }
    },
};

// Both return the theme now applied, so the layout can hand it on to BonboardingRoot without asking
// a second time.
window.boGetTheme = function () {
    return window.boTheme.current();
};

window.boToggleTheme = function () {
    const next = window.boTheme.current() === 'dark' ? 'light' : 'dark';
    window.boTheme.set(next);
    return next;
};

window.boTheme.apply(window.boTheme.preferred());

// ── Header ───────────────────────────────────────────────────────────────────
//
// The header is a translucent pane. It grows a shadow the moment there is something scrolled
// underneath it to lift away from, and loses it again at the top of the page - so the elevation
// says "there is more above" rather than being decoration that is always on.

(function watchHeader() {
    const update = () => {
        const header = document.querySelector('.app-header');
        if (!header) return;
        header.setAttribute('data-scrolled', window.scrollY > 4 ? 'true' : 'false');
    };

    let queued = false;
    window.addEventListener('scroll', () => {
        if (queued) return;
        queued = true;
        requestAnimationFrame(() => {
            queued = false;
            update();
        });
    }, { passive: true });

    document.addEventListener('DOMContentLoaded', update);
    update();
})();

window.boScrollToTop = function () {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};
