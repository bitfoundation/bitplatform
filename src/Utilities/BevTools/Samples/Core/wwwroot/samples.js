// The JS module of the Interop and Errors pages.

export function viewport() {
    return `${window.innerWidth} x ${window.innerHeight}`;
}

export function slowCall(ms) {
    const end = performance.now() + ms;
    while (performance.now() < end) { /* busy wait: an expensive synchronous JS function */ }
    return ms;
}

export function callBack(dotnetRef) {
    return dotnetRef.invokeMethodAsync("Receive", Date.now());
}

export function throwError() {
    throw new Error("A sample JS error, caught by the page.");
}

export function throwUnhandled() {
    setTimeout(() => { throw new Error("A sample unhandled JS error."); }, 0);
    Promise.reject(new Error("A sample unhandled promise rejection."));
}
