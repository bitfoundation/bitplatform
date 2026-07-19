/* Light/dark theme bootstrap for the Bit.Brouter demos.
   Loaded synchronously in <head> so the bit-theme attribute is set before first paint
   (no flash of the wrong theme during prerendering). Defaults to the system preference;
   a toggle choice is persisted, but picking the system's theme again clears the override
   so the demo resumes following the OS preference. */
(function () {
    'use strict';

    var storageKey = 'bit-brouter-theme';
    var darkQuery = window.matchMedia('(prefers-color-scheme: dark)');

    function getStoredTheme() {
        try {
            var value = localStorage.getItem(storageKey);
            return (value === 'light' || value === 'dark') ? value : null;
        } catch (_) {
            return null;
        }
    }

    function getSystemTheme() {
        return darkQuery.matches ? 'dark' : 'light';
    }

    function apply(theme) {
        document.documentElement.setAttribute('bit-theme', theme);
        var themeColor = document.querySelector('meta[name="theme-color"]');
        if (themeColor) {
            themeColor.setAttribute('content', theme === 'dark' ? '#010409' : '#FFFFFF');
        }
        return theme;
    }

    apply(getStoredTheme() || getSystemTheme());

    darkQuery.addEventListener('change', function () {
        if (getStoredTheme() === null) {
            apply(getSystemTheme());
        }
    });

    window.bitBrouterTheme = {
        get: function () {
            return document.documentElement.getAttribute('bit-theme') || getSystemTheme();
        },
        toggle: function () {
            var next = this.get() === 'dark' ? 'light' : 'dark';
            try {
                if (next === getSystemTheme()) {
                    localStorage.removeItem(storageKey);
                } else {
                    localStorage.setItem(storageKey, next);
                }
            } catch (_) { }
            return apply(next);
        }
    };
})();
