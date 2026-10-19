// bit BevTools browser bridge.
// Only browser-only facts cross this bridge: JS errors, online/offline, visibility, reconnect state,
// storage inventory (on request), JS -> .NET call statistics and the toggle shortcut.
// It is loaded as an ES module by the BevTools panel; nothing runs until attach() is called.

interface BevShortcut {
    ctrl: boolean;
    shift: boolean;
    alt: boolean;
    meta: boolean;
    key: string;
}

interface BevJsCallEntry {
    identifier: string;
    args: number;
    sync: boolean;
    at: number;
    durationMs?: number;
    error?: string;
}

interface BevStorageEntry {
    key: string | null;
    size: number;
    preview: string;
    redacted: boolean;
}

let dotnetRef: any = null;
let shortcut: BevShortcut | null = null;
let jsToDotNetBatch: BevJsCallEntry[] = [];
let flushTimer: ReturnType<typeof setTimeout> | null = null;
let originalInvokeAsync: any = null;
let originalInvoke: any = null;
let originalObjectInvokeAsync: any = null;
let originalObjectInvoke: any = null;
let bridgeGeneration = 0;
const listeners: (() => void)[] = [];

function on(target: EventTarget, type: string, handler: (e: any) => void, options?: boolean | AddEventListenerOptions) {
    target.addEventListener(type, handler, options);
    listeners.push(() => target.removeEventListener(type, handler, options));
}

function report(method: string, ...args: any[]) {
    if (!dotnetRef) return;
    try {
        const p = dotnetRef.invokeMethodAsync(method, ...args);
        if (p && p.catch) p.catch(() => { /* circuit gone */ });
    } catch { /* ignore */ }
}

function parseShortcut(text: string | undefined): BevShortcut {
    const parts = (text || "Ctrl+Shift+D").split("+").map(p => p.trim().toLowerCase());
    return {
        ctrl: parts.includes("ctrl") || parts.includes("control"),
        shift: parts.includes("shift"),
        alt: parts.includes("alt"),
        meta: parts.includes("meta") || parts.includes("cmd"),
        key: parts[parts.length - 1],
    };
}

function matchesShortcut(e: KeyboardEvent) {
    if (!shortcut) return false;
    return e.ctrlKey === shortcut.ctrl && e.shiftKey === shortcut.shift && e.altKey === shortcut.alt && e.metaKey === shortcut.meta
        && (e.key || "").toLowerCase() === shortcut.key;
}

function isPaletteShortcut(e: KeyboardEvent) {
    return (e.ctrlKey || e.metaKey) && !e.shiftKey && !e.altKey && (e.key || "").toLowerCase() === "k";
}

function isPanelOpen() {
    return !!(document.querySelector && document.querySelector(".bev-root .bev-panel"));
}

function scheduleFlush() {
    if (flushTimer) return;
    flushTimer = setTimeout(() => {
        flushTimer = null;
        if (jsToDotNetBatch.length === 0) return;
        const batch = jsToDotNetBatch;
        jsToDotNetBatch = [];
        report("ReportJsToDotNet", batch);
    }, 500);
}

// Instance methods blazor.web.js itself invokes: renderer event dispatch, root components and Virtualize (which the
// BevTools panel uses too). They are framework plumbing - UI events have their own timeline entries - not application
// interop, and counting them would make every click and keystroke look like a JS -> .NET call.
const frameworkInstanceMethods = new Set(["DispatchEventAsync", "AddRootComponent", "RemoveRootComponent", "SetRootComponentParameters", "OnSpacerBeforeVisible", "OnSpacerAfterVisible"]);

// Once a Server circuit's connection is down, every report fails inside Blazor with an unhandled rejection carrying
// this SignalR message. Reporting that rejection fails the same way and raises another one: a tight loop that can
// never reach .NET anyway.
function isConnectionDown(err: any) {
    return !!(err && typeof err.message === "string" && err.message.includes("connection is not in the 'Connected' State"));
}

function isBevToolsCall(assemblyOrRef: any, method: any) {
    return assemblyOrRef === dotnetRef || assemblyOrRef === "Bit.BevTools"
        || (typeof method === "string" && method.startsWith("Bit.BevTools."));
}

function queueJsToDotNet(entry: BevJsCallEntry, generation: number) {
    if (generation !== bridgeGeneration || !dotnetRef) return;
    jsToDotNetBatch.push(entry);
    scheduleFlush();
}

function finishCall(entry: BevJsCallEntry, start: number, generation: number, err?: any, failed?: boolean) {
    entry.durationMs = performance.now() - start;
    if (failed) entry.error = String(err && err.message ? err.message : err);
    queueJsToDotNet(entry, generation);
}

function trackAsync(identifier: string, argCount: number, invoke: () => any) {
    const start = performance.now();
    const generation = bridgeGeneration;
    const entry: BevJsCallEntry = { identifier, args: argCount, sync: false, at: Date.now() };
    let promise: any;
    try {
        promise = invoke();
    } catch (err) {
        finishCall(entry, start, generation, err, true);
        throw err;
    }
    return promise.then((r: any) => {
        finishCall(entry, start, generation);
        return r;
    }, (err: any) => {
        finishCall(entry, start, generation, err, true);
        throw err;
    });
}

function trackSync(identifier: string, argCount: number, invoke: () => any) {
    const start = performance.now();
    const generation = bridgeGeneration;
    const entry: BevJsCallEntry = { identifier, args: argCount, sync: true, at: Date.now() };
    try {
        const r = invoke();
        finishCall(entry, start, generation);
        return r;
    } catch (err) {
        finishCall(entry, start, generation, err, true);
        throw err;
    }
}

function wrapDotNet() {
    const DotNet = (window as any).DotNet;
    if (!DotNet || originalInvokeAsync) return;
    originalInvokeAsync = DotNet.invokeMethodAsync;
    originalInvoke = DotNet.invokeMethod;
    DotNet.invokeMethodAsync = function (this: any, assemblyName: any, methodIdentifier: string, ...args: any[]) {
        const call = () => originalInvokeAsync.apply(this, [assemblyName, methodIdentifier, ...args]);
        if (isBevToolsCall(assemblyName, methodIdentifier)) return call();
        const isObjectRef = typeof assemblyName !== "string";
        const target = isObjectRef ? "object#" + (assemblyName && assemblyName.__dotNetObject !== undefined ? assemblyName.__dotNetObject : "?") : assemblyName;
        return trackAsync(target + "." + methodIdentifier, args.length, call);
    };
    if (originalInvoke) {
        DotNet.invokeMethod = function (this: any, assemblyName: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalInvoke.apply(this, [assemblyName, methodIdentifier, ...args]);
            if (isBevToolsCall(assemblyName, methodIdentifier)) return call();
            return trackSync(assemblyName + "." + methodIdentifier, args.length, call);
        };
    }

    // Calls on a DotNetObjectReference (dotNetRef.invokeMethodAsync("OnTick")) go straight to the call dispatcher and
    // never pass through DotNet.invokeMethodAsync, so the instance methods are wrapped as well. The bridge's own
    // reference is a DotNetObject too; its reports are BevTools traffic, not application calls.
    const proto = DotNet.DotNetObject && DotNet.DotNetObject.prototype;
    if (proto && typeof proto.invokeMethodAsync === "function") {
        originalObjectInvokeAsync = proto.invokeMethodAsync;
        proto.invokeMethodAsync = function (this: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalObjectInvokeAsync.apply(this, [methodIdentifier, ...args]);
            if (this === dotnetRef || frameworkInstanceMethods.has(methodIdentifier)) return call();
            return trackAsync("object#" + (this._id ?? "?") + "." + methodIdentifier, args.length, call);
        };
    }
    if (proto && typeof proto.invokeMethod === "function") {
        originalObjectInvoke = proto.invokeMethod;
        proto.invokeMethod = function (this: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalObjectInvoke.apply(this, [methodIdentifier, ...args]);
            if (this === dotnetRef || frameworkInstanceMethods.has(methodIdentifier)) return call();
            return trackSync("object#" + (this._id ?? "?") + "." + methodIdentifier, args.length, call);
        };
    }
}

function unwrapDotNet() {
    const DotNet = (window as any).DotNet;
    if (!DotNet || !originalInvokeAsync) return;
    DotNet.invokeMethodAsync = originalInvokeAsync;
    if (originalInvoke) DotNet.invokeMethod = originalInvoke;
    const proto = DotNet.DotNetObject && DotNet.DotNetObject.prototype;
    if (proto && originalObjectInvokeAsync) proto.invokeMethodAsync = originalObjectInvokeAsync;
    if (proto && originalObjectInvoke) proto.invokeMethod = originalObjectInvoke;
    originalInvokeAsync = null;
    originalInvoke = null;
    originalObjectInvokeAsync = null;
    originalObjectInvoke = null;
}

function snapshot() {
    const nav = performance.getEntriesByType ? performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined : null;
    // performance.memory is Chromium-only and not part of the DOM typings.
    const mem = (performance as any).memory;
    return {
        online: navigator.onLine,
        visibility: document.visibilityState,
        userAgent: navigator.userAgent,
        jsHeapUsedMb: mem ? mem.usedJSHeapSize / 1048576 : null,
        jsHeapLimitMb: mem ? mem.jsHeapSizeLimit / 1048576 : null,
        navigationLoadMs: nav ? nav.loadEventEnd : null,
        domContentLoadedMs: nav ? nav.domContentLoadedEventEnd : null,
        resourceCount: performance.getEntriesByType ? performance.getEntriesByType("resource").length : null,
    };
}

export function attach(ref: any, options: { shortcut?: string, trackJsToDotNet?: boolean } | null) {
    detach();
    dotnetRef = ref;
    shortcut = parseShortcut(options ? options.shortcut : undefined);

    on(window, "error", (e: ErrorEvent) => {
        const err = e.error;
        if (isConnectionDown(err)) return;
        report("OnJsError", {
            message: e.message || (err && err.message) || "Unknown error",
            source: e.filename ? e.filename + ":" + e.lineno + ":" + e.colno : null,
            stack: err && err.stack ? String(err.stack).slice(0, 4000) : null,
            type: err && err.name ? err.name : "Error",
        });
    });
    on(window, "unhandledrejection", (e: PromiseRejectionEvent) => {
        const reason = e.reason;
        if (isConnectionDown(reason)) return;
        report("OnJsError", {
            message: reason && reason.message ? reason.message : String(reason),
            source: null,
            stack: reason && reason.stack ? String(reason.stack).slice(0, 4000) : null,
            type: "UnhandledRejection",
        });
    });
    on(window, "online", () => report("OnBrowserEvent", "online", null));
    on(window, "offline", () => report("OnBrowserEvent", "offline", null));
    on(document, "visibilitychange", () => report("OnBrowserEvent", "visibility", document.visibilityState));
    on(document, "components:reconnect-state-changed", (e: CustomEvent) => {
        const d = e.detail || {};
        report("OnReconnectState", d.state || "unknown", d.currentAttempt || 0, d.secondsToNextAttempt || 0);
    });
    on(document, "keydown", (e: KeyboardEvent) => {
        if (matchesShortcut(e)) {
            e.preventDefault();
            report("OnToggleShortcut");
        } else if (isPaletteShortcut(e) && isPanelOpen()) {
            // While the panel is open Ctrl+K belongs to it wherever focus is (opening the panel from the badge leaves
            // focus on the page), and the browser must not also act on it (Chrome focuses its address bar).
            e.preventDefault();
            report("OnPaletteShortcut");
        }
    }, true);

    if (options && options.trackJsToDotNet) {
        wrapDotNet();
    }

    return snapshot();
}

export function detach() {
    bridgeGeneration++;
    while (listeners.length) listeners.pop()!();
    unwrapDotNet();
    if (flushTimer) { clearTimeout(flushTimer); flushTimer = null; }
    // Never retain calls across panel/circuit lifetimes: a later attach belongs to a different BevTools session.
    jsToDotNetBatch = [];
    dotnetRef = null;
}

export function refresh() {
    return snapshot();
}

const REDACTED = "«redacted»";

// A JSON Web Token is a credential whatever key it is stored under, and "user"/"profile"/"session" are far more
// common key names for one than "token" is.
function looksLikeSecret(value: string) {
    if (/ey[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}\./.test(value)) return true;
    if (/^Bearer\s+\S+/i.test(value)) return true;
    if (/-----BEGIN [A-Z ]*PRIVATE KEY-----/i.test(value)) return true;
    return /(?:password|passwd|pwd|api[_-]?key|access[_-]?token|id[_-]?token|refresh[_-]?token|authorization|client[_-]?secret|connectionstring|token|secret|credential|cookie|private[_ -]?key)"?\s*[:=]/i.test(value);
}

export function getStorage(kind: string, sensitivePatterns: string[] | null) {
    const storage = kind === "session" ? window.sessionStorage : window.localStorage;
    const patterns = (sensitivePatterns || []).map(p => String(p).toLowerCase());
    const result: BevStorageEntry[] = [];
    try {
        for (let i = 0; i < storage.length; i++) {
            const key = storage.key(i);
            const value = storage.getItem(key!) || "";
            const lowerKey = (key || "").toLowerCase();
            // Redaction happens before the value leaves the browser: BevTools never holds the plaintext.
            const sensitive = patterns.some(p => lowerKey.includes(p)) || looksLikeSecret(value);
            result.push({
                key,
                size: value.length,
                preview: sensitive ? REDACTED : (value.length > 120 ? value.slice(0, 120) + "…" : value),
                redacted: sensitive,
            });
        }
    } catch (err) {
        result.push({ key: "(error)", size: 0, preview: String(err), redacted: false });
    }
    return result;
}

export function loadPrefs() {
    try { return window.localStorage.getItem("bit-bevtools:prefs"); } catch { return null; }
}

export function savePrefs(json: string) {
    try { window.localStorage.setItem("bit-bevtools:prefs", json); } catch { /* ignore */ }
}

export function copyText(text: string) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        return navigator.clipboard.writeText(text).then(() => true, () => false);
    }
    return false;
}

// Offers captured data as a file. The content is built on the .NET side (already redacted and bounded); this only
// hands it to the browser's download machinery and revokes the object URL afterwards.
export function downloadText(fileName: string, content: string, contentType: string) {
    try {
        const blob = new Blob([content], { type: contentType || "text/plain" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "bit-bevtools.txt";
        a.rel = "noopener";
        a.style.display = "none";
        document.body.appendChild(a);
        a.click();
        setTimeout(() => {
            a.remove();
            URL.revokeObjectURL(url);
        }, 0);
        return true;
    } catch {
        return false;
    }
}
