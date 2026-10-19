// Minimal interop for the documentation site: clipboard and theme persistence.
// Everything else on the site is C#.
window.blexDocs = {
    copy: function (text) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(text);
        }

        // Fallback for non-secure contexts (e.g. plain http during local testing).
        var area = document.createElement('textarea');
        area.value = text;
        area.setAttribute('readonly', '');
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();
        try { document.execCommand('copy'); } finally { document.body.removeChild(area); }
        return Promise.resolve();
    },

    getTheme: function (key) {
        try {
            var stored = localStorage.getItem(key);
            if (stored) return stored;
            return window.matchMedia && window.matchMedia('(prefers-color-scheme: light)').matches
                ? 'light' : 'dark';
        } catch (e) {
            return 'dark';
        }
    },

    // Used by the persistence page to show the raw payload Blex wrote.
    readStorage: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    },

    setTheme: function (key, theme) {
        document.documentElement.setAttribute('data-theme', theme);
        try { localStorage.setItem(key, theme); } catch (e) { /* private mode */ }
    }
};
