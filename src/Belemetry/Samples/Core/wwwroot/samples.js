// Browser-side helpers of the sample pages.

// A plain fetch from the page: Belemetry records it as a client span (feature Dependencies) and, for same-origin
// targets, adds a W3C traceparent header so the server's span joins the same trace.
export async function fetchUrl(url) {
    const response = await fetch(url);
    return response.status;
}

// Thrown from a timer rather than from the interop call itself, so it is an uncaught error that reaches
// window.onerror - which is what Belemetry turns into an exception record (feature Errors).
export function throwError(message) {
    setTimeout(() => { throw new Error(message); });
}
