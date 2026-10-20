/* Light/dark theme bootstrap - part of the site kit every Utilities demo shares (see app.css).
   Loaded synchronously in <head> so the bit-theme attribute is set before first paint
   (no flash of the wrong theme during prerendering). Defaults to the system preference;
   a toggle choice is persisted, but picking the system's theme again clears the override
   so the demo resumes following the OS preference.

   A library with a theme of its own (a color-mode parameter, a dark class) follows the site
   with bitDemoTheme.onChange - or, from .NET, bitDemoTheme.subscribe(dotNetRef, 'MethodName'),
   which calls the [JSInvokable] method with "light" or "dark" now and on every change. */
(function () {
    'use strict';

    var storageKey = 'bit-demo-theme';
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

    var listeners = [];
    var current = null;

    function apply(theme) {
        var changed = current !== theme;
        current = theme;
        document.documentElement.setAttribute('bit-theme', theme);
        var themeColor = document.querySelector('meta[name="theme-color"]');
        if (themeColor) {
            // The Fluent dark palette's primary background, so the browser chrome matches the page.
            themeColor.setAttribute('content', theme === 'dark' ? '#0F1318' : '#FFFFFF');
        }
        if (changed) {
            listeners.slice().forEach(function (listener) {
                try { listener(theme); } catch (_) { }
            });
        }
        return theme;
    }

    apply(getStoredTheme() || getSystemTheme());

    /* Blazor's enhanced navigation - a link followed before the app is interactive, or a redirect
       the router hands back to the page loader - fetches the next page and merges it into this
       document, <html> included. The server's markup carries no bit-theme, so the merge removes it
       and the page falls back to the light palette, and this script does not run again to put it
       back. Restoring it from an observer happens before the merged page is painted. */
    new MutationObserver(function () {
        if (document.documentElement.getAttribute('bit-theme') !== current) apply(current);
    }).observe(document.documentElement, { attributes: true, attributeFilter: ['bit-theme'] });

    darkQuery.addEventListener('change', function () {
        if (getStoredTheme() === null) {
            apply(getSystemTheme());
        }
    });

    window.bitDemoTheme = {
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
        },
        /** Calls back with the theme now and after every change; returns the function that stops it. */
        onChange: function (callback) {
            listeners.push(callback);
            callback(this.get());
            return function () {
                var index = listeners.indexOf(callback);
                if (index >= 0) listeners.splice(index, 1);
            };
        },
        /** The .NET side of onChange: invokes dotNetRef.method(theme). Returns a handle for unsubscribe. */
        subscribe: function (dotNetRef, method) {
            return this.onChange(function (theme) {
                dotNetRef.invokeMethodAsync(method, theme);
            });
        },
        unsubscribe: function (handle) {
            if (typeof handle === 'function') handle();
        }
    };
})();
