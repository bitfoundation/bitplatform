// The triggers the instrumentation pages press - the one script a page needs beyond the site kit
// (theme.js and site.js, shared by every Utilities demo). A plain script rather than a module: the
// pages call it by name through IJSRuntime.
//
// Each trigger does the thing the browser script instruments - throws, rejects, fetches, blocks the main
// thread, writes to the console - exactly as app code would, so what the reader then sees on the Live
// telemetry feed is what Belemetry records from a real app, not a simulation of it.

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
