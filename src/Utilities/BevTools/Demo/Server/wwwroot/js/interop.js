// Demo JS module exercised by the JS Interop page.
export function measure(selector) {
    const el = document.querySelector(selector);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    return { width: Math.round(r.width), height: Math.round(r.height), top: Math.round(r.top) };
}

export function slowCall(ms) {
    const end = performance.now() + ms;
    while (performance.now() < end) { /* busy wait to simulate an expensive synchronous JS function */ }
    return ms;
}

export function ping() {
    return performance.now();
}

export function throwError() {
    throw new Error("Simulated JavaScript failure from interop.js");
}

export function triggerUnhandledError() {
    setTimeout(() => { throw new Error("Unhandled JS error (setTimeout) from the demo"); }, 0);
    Promise.reject(new Error("Unhandled promise rejection from the demo"));
}

let callbackRef = null;
let callbackTimer = null;

export function startCallbacks(dotnetRef, intervalMs) {
    stopCallbacks();
    callbackRef = dotnetRef;
    callbackTimer = setInterval(() => {
        if (callbackRef) callbackRef.invokeMethodAsync("OnTick", Date.now()).catch(() => stopCallbacks());
    }, intervalMs);
}

export function stopCallbacks() {
    if (callbackTimer) clearInterval(callbackTimer);
    callbackTimer = null;
    callbackRef = null;
}

export function writeStorage() {
    localStorage.setItem("demo:theme", "light");
    localStorage.setItem("demo:auth_token", "should-be-redacted-by-bevtools");
    sessionStorage.setItem("demo:cart", JSON.stringify({ items: 3 }));
}
