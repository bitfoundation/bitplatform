// Light / dark theme for the demo site.
//
// Loaded synchronously from <head> so the theme is applied before the first
// paint (no flash of the wrong theme). The resolved theme is written to
// <html data-theme="light|dark">, which both app.css and the Tailwind build
// key their dark palette off.
//
// An explicit choice is remembered in localStorage; until the visitor makes
// one, the OS preference (prefers-color-scheme) decides.
(function () {
    var storageKey = 'bit-bh-theme';
    var root = document.documentElement;

    function stored() {
        try {
            var value = localStorage.getItem(storageKey);
            return value === 'light' || value === 'dark' ? value : null;
        } catch {
            return null;
        }
    }

    function apply(theme) {
        root.setAttribute('data-theme', theme);
    }

    var current = stored() || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    apply(current);

    // Blazor's enhanced navigation syncs <html> attributes with the server-rendered
    // markup, which has no data-theme. Put it back whenever it gets dropped.
    new MutationObserver(function () {
        if (root.getAttribute('data-theme') !== current) apply(current);
    }).observe(root, { attributes: true, attributeFilter: ['data-theme'] });

    window.bitBhTheme = {
        get: function () {
            return current;
        },
        set: function (theme) {
            current = theme;
            try {
                localStorage.setItem(storageKey, theme);
            } catch {
                // Storage unavailable (private mode, blocked cookies): theme still applies for this page.
            }
            apply(theme);
        }
    };
})();
