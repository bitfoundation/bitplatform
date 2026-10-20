// The one piece of interop the Blex docs need beyond the site kit (site.js, theme.js): the
// persistence page shows the raw payload Blex wrote to localStorage. Everything else is C#.
window.blexDocs = {
    readStorage: function (key) {
        try {
            return localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    }
};
