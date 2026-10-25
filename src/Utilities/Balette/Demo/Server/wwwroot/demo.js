/* Balette demo - the glue between the site kit (theme.js, site.js) and this demo's own palette.

   1. The site theme the way the demo's Theme command and its Theming page know it - "auto", "light" or
      "dark" - on top of theme.js, which knows the light/dark it applied and whether the reader chose it.
   2. A truce between the two palettes on this site. Balette's own (Ctrl+K, plus the demo's global
      shortcuts) listens at the document in the capture phase, so it hears every key first; the site's
      docs search (the kit's CommandPalette) answers to "/". Whichever of the two is open owns the keys.
   3. The clipboard write of the "Copy share link" command, which has to know whether the browser allowed it. */
(function () {
    'use strict';

    // The key theme.js keeps the reader's choice under; absent means "follow the system".
    var THEME_KEY = 'bit-demo-theme';
    var darkQuery = window.matchMedia('(prefers-color-scheme: dark)');

    function storedTheme() {
        try {
            var value = localStorage.getItem(THEME_KEY);
            return value === 'light' || value === 'dark' ? value : null;
        } catch (_) {
            return null;
        }
    }

    function themeMode() {
        return storedTheme() || 'auto';
    }

    function setThemeMode(mode) {
        var system = darkQuery.matches ? 'dark' : 'light';
        var target = mode === 'light' || mode === 'dark' ? mode : system;

        // toggle() repaints the page and tells every listener; it also stores the new theme, unless it is the
        // system's - so the reader's own choice is written after it.
        if (window.bitDemoTheme.get() !== target) window.bitDemoTheme.toggle();
        try {
            if (target === mode) {
                localStorage.setItem(THEME_KEY, mode);
            } else {
                localStorage.removeItem(THEME_KEY);
            }
        } catch (_) { }

        return themeMode();
    }

    var stopWatching = null;

    function isEditable(element) {
        if (!(element instanceof Element)) return false;
        var name = element.tagName;
        return name === 'INPUT' || name === 'TEXTAREA' || name === 'SELECT' || element.isContentEditable;
    }

    /* While the site's docs search is open, its keys are its own. Balette leaves alone a key whose default
       was prevented (its guide's Troubleshooting says as much), but preventing it would also take what the
       key does in the search box - typing, the editing chords, the browser's own shortcuts. So the event is
       marked as claimed for the listeners after this one instead, and keeps its default: Ctrl+K pressed in
       the docs search does not open a second palette on top of it, and G then S does not navigate behind it. */
    function claimForSitePalette(e) {
        if (!(e.target instanceof Element) || e.target.closest('.bb-palette') === null) return;
        Object.defineProperty(e, 'defaultPrevented', { configurable: true, value: true });
    }

    /* The other way round: "/" opens the docs search from anywhere but a text field - which includes a button
       inside Balette's open dialog, where the search would open behind the modal and wait there. */
    function keepSlashFromSitePalette(e) {
        if (e.key !== '/' || e.ctrlKey || e.metaKey || e.altKey) return;
        if (document.querySelector('dialog[data-balette="dialog"][open]') === null) return;
        if (isEditable(document.activeElement)) return;
        e.preventDefault();
        e.stopPropagation();
    }

    // On the window, in the capture phase: the one place that hears a key before the document does.
    window.addEventListener('keydown', function (e) {
        claimForSitePalette(e);
        keepSlashFromSitePalette(e);
    }, true);

    window.baletteDemo = {
        themeMode: themeMode,

        /** Applies "auto", "light" or "dark" to the site and answers with the mode in effect. */
        setThemeMode: setThemeMode,

        /** Writes text to the clipboard, and answers whether the browser let it. */
        copyText: function (text) {
            if (!navigator.clipboard || !window.isSecureContext) return Promise.resolve(false);
            return navigator.clipboard.writeText(text).then(function () { return true; }, function () { return false; });
        },

        /** Calls dotNetRef.OnSiteThemeChanged(mode) now and after every change of the site theme. */
        watchTheme: function (dotNetRef) {
            if (stopWatching) stopWatching();
            stopWatching = window.bitDemoTheme.onChange(function () {
                // Deferred: a change made through setThemeMode stores the reader's choice only after the
                // repaint that announced it, and the mode reported has to be the final one.
                setTimeout(function () { dotNetRef.invokeMethodAsync('OnSiteThemeChanged', themeMode()); }, 0);
            });
        },

        unwatchTheme: function () {
            if (stopWatching) stopWatching();
            stopWatching = null;
        }
    };
})();
