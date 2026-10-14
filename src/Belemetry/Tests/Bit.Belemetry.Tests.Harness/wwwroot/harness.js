// The browser half of the harness: actions that must work without .NET interactivity (static SSR), and consent
// controlled from the URL. Loaded as a module right after <BelemetryScript />, so module order guarantees that
// window.BitBelemetry exists and has already auto-started from the rendered configuration.

const api = () => window.BitBelemetry;

// ?consent=all | none | <feature bit mask>: decide consent before anything else happens on the page, the way a
// returning user's stored choice would.
(function applyConsentFromQuery() {
    const value = new URLSearchParams(location.search).get('consent');
    if (value === null || !api()) return;
    const features = value === 'all' ? 4095 : value === 'none' ? 0 : Number(value);
    if (!isNaN(features)) api().setConsent(features);
})();

async function fetchEcho(output) {
    output.removeAttribute('data-done');
    try {
        // Same origin, relative to <base href="/">: the fetch instrumentation records a client span and adds a
        // traceparent header, which the endpoint echoes back.
        const response = await fetch('test-api/echo?from=harness');
        const body = await response.json();
        output.setAttribute('data-status', String(response.status));
        output.setAttribute('data-traceparent', (body.headers && body.headers.traceparent) || '');
    } catch (e) {
        output.setAttribute('data-error', String(e));
    }
    output.setAttribute('data-done', 'true');
}

document.addEventListener('click', e => {
    const element = e.target instanceof Element ? e.target.closest('[data-harness-action]') : null;
    if (!element) return;

    switch (element.getAttribute('data-harness-action')) {
        case 'fetch-echo':
            fetchEcho(document.getElementById('fetch-result'));
            break;
        case 'throw-error':
            // From a timer, so it is uncaught and reaches window.onerror like any real script error.
            setTimeout(() => { throw new Error('Harness JS error ' + (element.getAttribute('data-message') || '')); });
            break;
        case 'consent':
            if (api()) api().setConsent(Number(element.getAttribute('data-features')));
            break;
        case 'flush':
            if (api()) api().flush();
            break;
    }
});
