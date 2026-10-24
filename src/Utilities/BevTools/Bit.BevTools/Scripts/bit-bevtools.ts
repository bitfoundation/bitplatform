// bit BevTools browser bridge and page hub.
//
// Every BevTools runtime on the page - a Blazor Server circuit, the WebAssembly runtime, both at once in a Web App -
// attaches here as a target through its panel. The hub keeps the page-level facts only the browser knows (JS errors,
// failed resource loads, CSP violations, online/visibility, reconnect state, enhanced navigation, WebSocket/SignalR
// traffic) and reports them to every target; it routes JS -> .NET call statistics to the runtime that received the
// call; and it exposes the targets to external frontends - the companion window and the browser extension - through
// window.__BIT_BEVTOOLS__, with the protocol each target answers in .NET.
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

interface BevAttachOptions {
    shortcut?: string;
    trackJsToDotNet?: boolean;
    trackTraffic?: boolean;
    sensitivePatterns?: string[];
    sessionId?: number;
    /** The id the runtime knows this module by: over a circuit, every call .NET makes into it names that id. */
    moduleObjectId?: number | null;
    platform?: string | null;
    renderMode?: string | null;
    label?: string;
}

/** What a frontend is told about one runtime on the page. */
export interface BevTargetInfo {
    id: string;
    sessionId: number | null;
    platform: string | null;
    renderMode: string | null;
    label: string;
    attachedAt: number;
}

interface BevTarget extends BevTargetInfo {
    ref: any;
    moduleObjectId: number | null;
    jsToDotNetBatch: BevJsCallEntry[];
    flushTimer: ReturnType<typeof setTimeout> | null;
}

interface BevMessageStat {
    count: number;
    bytes: number;
    maxBytes: number;
}

/** One WebSocket the page holds, as BevTools observed it. Payloads are never kept: only sizes and message kinds. */
export interface BevSocketStats {
    id: number;
    url: string;
    kind: "blazor-circuit" | "signalr" | "websocket";
    protocol: string | null;
    openedAt: number | null;
    observedFrom: number;
    closedAt: number | null;
    closeCode: number | null;
    closeReason: string | null;
    sent: BevMessageStat;
    received: BevMessageStat;
    own: BevMessageStat;
    messages: { [key: string]: BevMessageStat };
    /** The application's render batches; batches that changed only BevTools' own UI are counted in `bevTools` and `own`. */
    renderBatches: { count: number; bytes: number; maxBytes: number; applyMs: number; maxApplyMs: number; pending: number; bevTools: number };
    largest: { direction: string; kind: string; bytes: number; at: number }[];
}

let shortcut: BevShortcut | null = null;
let sensitivePatterns: string[] = [];
let originalInvokeAsync: any = null;
let originalInvoke: any = null;
let originalObjectInvokeAsync: any = null;
let originalObjectInvoke: any = null;
let originalSend: any = null;
let originalWebSocket: any = null;
let trafficTimer: ReturnType<typeof setInterval> | null = null;
let trafficVersion = 0;
let reportedTrafficVersion = 0;
let nextTargetId = 0;
let enhancedLoadHandler: (() => void) | null = null;
let hubGeneration = 0;
const targets = new Map<string, BevTarget>();
const listeners: (() => void)[] = [];
const subscribers = new Set<(event: { type: string; [key: string]: any }) => void>();

function on(target: EventTarget, type: string, handler: (e: any) => void, options?: boolean | AddEventListenerOptions) {
    target.addEventListener(type, handler, options);
    listeners.push(() => target.removeEventListener(type, handler, options));
}

/**
 * Whether the page's Blazor circuit has no open connection: a circuit socket was seen, and every one seen is closed
 * (a circuit that ended, or one between a dropped connection and its reconnect). A call into a Server runtime then
 * goes nowhere - Blazor's send fails where nothing catches it, an unhandled rejection on the application's page, and
 * the call's own promise never settles. Pages on another transport have no socket to judge by and are never "down".
 */
function circuitDown(): boolean {
    let seen = false;
    for (const s of sockets.values()) {
        if (s.kind !== "blazor-circuit") continue;
        if (s.closedAt === null) return false;
        seen = true;
    }
    return seen;
}

const unreachable = (target: BevTarget) => target.platform === "Server" && circuitDown();

function invoke(target: BevTarget, method: string, ...args: any[]) {
    if (unreachable(target)) return;
    try {
        const p = target.ref.invokeMethodAsync(method, ...args);
        if (p && p.catch) p.catch(() => { /* circuit gone */ });
    } catch { /* ignore */ }
}

/** Page-level facts belong to the page, not to one runtime: every runtime on it hears them. */
function broadcast(method: string, ...args: any[]) {
    targets.forEach(t => invoke(t, method, ...args));
}

function notify(event: { type: string; [key: string]: any }) {
    subscribers.forEach(s => { try { s(event); } catch { /* a frontend's own failure */ } });
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

/** The runtime a keystroke belongs to: the one whose panel is open, else the one attached last. */
function focusedTarget(): BevTarget | null {
    let last: BevTarget | null = null;
    let open: BevTarget | null = null;
    targets.forEach(t => {
        last = t;
        const root = document.querySelector ? document.querySelector(`.bev-root[data-bev-target="${t.id}"] .bev-panel`) : null;
        if (root) open = t;
    });
    return open || last;
}

// ----- redaction (kept in step with BevRedactor) -----

const REDACTED = "«redacted»";

// A JSON Web Token is a credential whatever key it is stored under, and "user"/"profile"/"session" are far more
// common key names for one than "token" is.
function looksLikeSecret(value: string) {
    if (/ey[A-Za-z0-9_-]{6,}\.[A-Za-z0-9_-]{6,}\./.test(value)) return true;
    if (/\bBearer\s+\S+/i.test(value)) return true;
    if (/-----BEGIN [A-Z ]*PRIVATE KEY-----/i.test(value)) return true;
    if (/\b(?:AKIA[0-9A-Z]{16}|gh[pousr]_[A-Za-z0-9]{30,}|xox[abposr]-[A-Za-z0-9-]{10,}|[rs]k_(?:live|test)_[A-Za-z0-9]{16,})\b/.test(value)) return true;
    return /(?:password|passwd|pwd|api[_-]?key|access[_-]?key|account[_-]?key|access[_-]?token|id[_-]?token|refresh[_-]?token|authorization|client[_-]?secret|connectionstring|token|secret|credential|cookie|private[_ -]?key)"?\s*[:=]/i.test(value);
}

function isSensitiveName(name: string) {
    const lower = name.toLowerCase();
    return sensitivePatterns.some(p => lower.includes(p));
}

/** Query and fragment values whose key looks sensitive, or whose value looks like a token, never leave the hub. */
export function redactUrl(url: string) {
    if (!url) return url;
    const at = url.indexOf("://");
    if (at >= 0) {
        const authorityEnd = url.indexOf("/", at + 3);
        const authority = authorityEnd < 0 ? url.slice(at + 3) : url.slice(at + 3, authorityEnd);
        const userInfo = authority.lastIndexOf("@");
        if (userInfo >= 0) url = url.slice(0, at + 3) + REDACTED + url.slice(at + 3 + userInfo);
    }
    const redactParams = (params: string) => params.split("&").map(part => {
        const eq = part.indexOf("=");
        const key = eq < 0 ? part : part.slice(0, eq);
        let value = "";
        try { value = eq < 0 ? "" : decodeURIComponent(part.slice(eq + 1).replace(/\+/g, " ")); } catch { value = part.slice(eq + 1); }
        let decodedKey = key;
        try { decodedKey = decodeURIComponent(key); } catch { /* keep */ }
        // "id" is SignalR's connection token; "sig" an Azure SAS signature.
        return isSensitiveName(decodedKey) || /^(?:id|sig)$/i.test(decodedKey) || looksLikeSecret(value) ? key + "=" + REDACTED : part;
    }).join("&");
    const hash = url.indexOf("#");
    const beforeHash = hash < 0 ? url : url.slice(0, hash);
    const q = beforeHash.indexOf("?");
    let result = q < 0 ? beforeHash : beforeHash.slice(0, q + 1) + redactParams(beforeHash.slice(q + 1));
    if (hash >= 0) {
        const fragment = url.slice(hash + 1);
        result += "#" + (fragment.indexOf("=") >= 0 ? redactParams(fragment) : fragment);
    }
    return result;
}

// ----- JS -> .NET call tracking -----

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

function isOwnRef(ref: any) {
    let own = false;
    targets.forEach(t => { if (t.ref === ref) own = true; });
    return own;
}

function isBevToolsCall(assemblyOrRef: any, method: any) {
    return isOwnRef(assemblyOrRef) || assemblyOrRef === "Bit.BevTools"
        || (typeof method === "string" && method.startsWith("Bit.BevTools."));
}

/**
 * The runtime that receives a call: a DotNetObject knows its call dispatcher, and so does each target's own reference.
 * A static call goes to the default dispatcher, which a page with two runtimes does not have; there it is the only target.
 */
function targetForDispatcher(dispatcher: any): BevTarget | null {
    let match: BevTarget | null = null;
    let only: BevTarget | null = null;
    let count = 0;
    targets.forEach(t => {
        count++;
        only = t;
        if (dispatcher && t.ref && t.ref._callDispatcher === dispatcher) match = t;
    });
    return match || (count === 1 ? only : null);
}

function queueJsToDotNet(target: BevTarget | null, entry: BevJsCallEntry, generation: number) {
    if (!target || generation !== hubGeneration || !targets.has(target.id)) return;
    target.jsToDotNetBatch.push(entry);
    if (target.flushTimer) return;
    target.flushTimer = setTimeout(() => {
        target.flushTimer = null;
        if (target.jsToDotNetBatch.length === 0) return;
        const batch = target.jsToDotNetBatch;
        target.jsToDotNetBatch = [];
        invoke(target, "ReportJsToDotNet", batch);
    }, 500);
}

function finishCall(target: BevTarget | null, entry: BevJsCallEntry, start: number, generation: number, err?: any, failed?: boolean) {
    entry.durationMs = performance.now() - start;
    if (failed) entry.error = String(err && err.message ? err.message : err);
    queueJsToDotNet(target, entry, generation);
}

function trackAsync(target: BevTarget | null, identifier: string, argCount: number, call: () => any) {
    const start = performance.now();
    const generation = hubGeneration;
    const entry: BevJsCallEntry = { identifier, args: argCount, sync: false, at: Date.now() };
    let promise: any;
    try {
        promise = call();
    } catch (err) {
        finishCall(target, entry, start, generation, err, true);
        throw err;
    }
    return promise.then((r: any) => {
        finishCall(target, entry, start, generation);
        return r;
    }, (err: any) => {
        finishCall(target, entry, start, generation, err, true);
        throw err;
    });
}

function trackSync(target: BevTarget | null, identifier: string, argCount: number, call: () => any) {
    const start = performance.now();
    const generation = hubGeneration;
    const entry: BevJsCallEntry = { identifier, args: argCount, sync: true, at: Date.now() };
    try {
        const r = call();
        finishCall(target, entry, start, generation);
        return r;
    } catch (err) {
        finishCall(target, entry, start, generation, err, true);
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
        const id = isObjectRef ? "object#" + (assemblyName && assemblyName.__dotNetObject !== undefined ? assemblyName.__dotNetObject : "?") : assemblyName;
        return trackAsync(targetForDispatcher(isObjectRef && assemblyName ? assemblyName._callDispatcher : null), id + "." + methodIdentifier, args.length, call);
    };
    if (originalInvoke) {
        DotNet.invokeMethod = function (this: any, assemblyName: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalInvoke.apply(this, [assemblyName, methodIdentifier, ...args]);
            if (isBevToolsCall(assemblyName, methodIdentifier)) return call();
            return trackSync(targetForDispatcher(null), assemblyName + "." + methodIdentifier, args.length, call);
        };
    }

    // Calls on a DotNetObjectReference (dotNetRef.invokeMethodAsync("OnTick")) go straight to the call dispatcher and
    // never pass through DotNet.invokeMethodAsync, so the instance methods are wrapped as well. The targets' own
    // references are DotNetObjects too; their reports are BevTools traffic, not application calls.
    const proto = DotNet.DotNetObject && DotNet.DotNetObject.prototype;
    if (proto && typeof proto.invokeMethodAsync === "function") {
        originalObjectInvokeAsync = proto.invokeMethodAsync;
        proto.invokeMethodAsync = function (this: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalObjectInvokeAsync.apply(this, [methodIdentifier, ...args]);
            if (isOwnRef(this) || frameworkInstanceMethods.has(methodIdentifier)) return call();
            return trackAsync(targetForDispatcher(this._callDispatcher), "object#" + (this._id ?? "?") + "." + methodIdentifier, args.length, call);
        };
    }
    if (proto && typeof proto.invokeMethod === "function") {
        originalObjectInvoke = proto.invokeMethod;
        proto.invokeMethod = function (this: any, methodIdentifier: string, ...args: any[]) {
            const call = () => originalObjectInvoke.apply(this, [methodIdentifier, ...args]);
            if (isOwnRef(this) || frameworkInstanceMethods.has(methodIdentifier)) return call();
            return trackSync(targetForDispatcher(this._callDispatcher), "object#" + (this._id ?? "?") + "." + methodIdentifier, args.length, call);
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

// ----- WebSocket / SignalR traffic -----

const RECORD_SEPARATOR = "\x1e";
const LARGE_MESSAGE = 64 * 1024;
const sockets = new Map<any, BevSocketStats>();
let nextSocketId = 0;
// A Blazor event raised in BevTools' own UI (a click on a tab, typing in a filter) reaches a circuit as an ordinary
// DispatchEventAsync message. It is marked while the browser dispatches the event; Blazor sends it before any timer
// runs, so the message sent meanwhile is the one that carries it.
let ownEventPending = false;
let ownEventTimer: ReturnType<typeof setTimeout> | null = null;
const OWN_EVENT_TYPES = ["click", "dblclick", "input", "change", "keydown", "keyup", "keypress", "mousedown", "mouseup", "pointerdown", "pointerup", "focusin", "focusout", "submit", "wheel"];

function markOwnEvent(e: Event) {
    if (!inBevTools(e.target as Node) || inOverlay(e.target as Node)) return;
    ownEventPending = true;
    ownEventTimer ??= setTimeout(() => { ownEventPending = false; ownEventTimer = null; }, 0);
}

// SignalR invocation ids of the reports BevTools makes over a circuit, so their completions count as its own too.
const ownInvocations = new Set<string>();
// The async handles of the calls a circuit made into this module (an attach, a download, a storage read), so the
// browser's reply to each counts as BevTools' own too.
const ownJsCalls = new Set<number>();
const pendingRenderBatches = new Map<string, { at: number; bytes: number; size: number }>();
// Acknowledgements decoded before their batch: SignalR applies a batch and the browser acknowledges it while the frame
// is still being dispatched, before this module's listener for that frame runs. The DOM changes are classified then,
// when they are the batch's, and the batch is settled once its frame is decoded.
const acknowledgedEarly = new Map<string, { at: number; dom: "bevtools" | "app" | "none"; size: number }>();
let frameArrivedAt: number | null = null;

/** A render batch and its acknowledgement, once both are known: BevTools' own when it changed only the panel. */
function settleRenderBatch(s: BevSocketStats, dom: "bevtools" | "app" | "none", batchBytes: number, batchSize: number, ackSize: number, applyMs: number) {
    if (dom === "bevtools") {
        s.renderBatches.bevTools++;
        add(s.own, batchSize);
        add(s.own, ackSize);
        return;
    }

    noteRenderBatch(s, batchBytes, batchSize, applyMs >= 0 ? applyMs : null);
    noteMessage(s, "→", "OnRenderCompleted", ackSize);
}

/** An application render batch: its size, the time the browser took to apply it, and its message. */
function noteRenderBatch(s: BevSocketStats, batchBytes: number, messageSize: number, applyMs: number | null) {
    const rb = s.renderBatches;
    rb.count++;
    rb.bytes += batchBytes;
    if (batchBytes > rb.maxBytes) rb.maxBytes = batchBytes;
    if (applyMs !== null) {
        rb.applyMs += applyMs;
        if (applyMs > rb.maxApplyMs) rb.maxApplyMs = applyMs;
    }
    noteMessage(s, "←", "JS.RenderBatch", messageSize);
}

// Which part of the page the DOM changed in since the last render batch was decoded: BevTools' panel, the app, or
// both. A render batch is applied while its frame is being dispatched, before this module decodes that frame, so a
// batch whose changes stayed inside .bev-root rendered nothing but BevTools' own UI. Anything that touched the app's
// DOM - or any change between frames that is not BevTools' - counts as the app's, which errs towards reporting.
let domObserver: MutationObserver | null = null;
let bevToolsDomChanged = false;
let appDomChanged = false;

function inBevTools(node: Node | null): boolean {
    const element = node && (node.nodeType === 1 ? node as Element : node.parentElement);
    return !!(element && typeof element.closest === "function" && element.closest(".bev-root"));
}

/**
 * A write that left the value as it was: page scripts that react to every render (a MutationObserver re-applying a
 * heading's anchor, a theme script re-setting a class) do this to the app's DOM whenever BevTools' panel renders,
 * and it changes nothing. Found in a real browser, where it made every panel render look like the app's.
 */
function isNoOp(record: MutationRecord) {
    if (record.type === "attributes" && record.attributeName) {
        return (record.target as Element).getAttribute(record.attributeName) === record.oldValue;
    }
    if (record.type === "characterData") {
        return (record.target as CharacterData).data === record.oldValue;
    }
    return false;
}

/** The page overlay: outlines and render flashes, drawn over the app and changed by neither the app nor a batch. */
function inOverlay(node: Node | null): boolean {
    const element = node && (node.nodeType === 1 ? node as Element : node.parentElement);
    return !!(element && typeof element.closest === "function" && element.closest(".bev-overlay"));
}

function noteMutations(records: MutationRecord[]) {
    for (const record of records) {
        // Outlines appear and fade while batches arrive. Counting them as BevTools' DOM would make an app batch that
        // changed nothing - the wasted render flashing exists to show - look like the panel's.
        if (inOverlay(record.target)) continue;
        if (record.type === "childList") {
            const changed = [...Array.from(record.addedNodes), ...Array.from(record.removedNodes)];
            if (changed.length > 0 && changed.every(n => n.nodeType === 1 && (n as Element).classList && (n as Element).classList.contains("bev-overlay"))) continue;
        }
        if (inBevTools(record.target)) {
            bevToolsDomChanged = true;
            continue;
        }
        if (isNoOp(record)) {
            continue;
        }
        // The panel itself appearing or going away: its parent is the app's, the nodes are BevTools'.
        const nodes = [...Array.from(record.addedNodes), ...Array.from(record.removedNodes)];
        if (record.type === "childList" && nodes.length > 0 && nodes.every(n => n.nodeType === 1 && (n as Element).classList && (n as Element).classList.contains("bev-root"))) {
            bevToolsDomChanged = true;
        } else {
            appDomChanged = true;
        }
    }
}

function takeDomChanges(): "bevtools" | "app" | "none" {
    if (domObserver) noteMutations(domObserver.takeRecords());
    const result = appDomChanged ? "app" : bevToolsDomChanged ? "bevtools" : "none";
    appDomChanged = false;
    bevToolsDomChanged = false;
    return result;
}

function observeDom() {
    if (domObserver || typeof MutationObserver === "undefined" || !document.documentElement) return;
    domObserver = new MutationObserver(noteMutations);
    domObserver.observe(document.documentElement, { subtree: true, childList: true, attributes: true, characterData: true, attributeOldValue: true, characterDataOldValue: true });
}

function stopObservingDom() {
    domObserver?.disconnect();
    domObserver = null;
    appDomChanged = false;
    bevToolsDomChanged = false;
}

function stat(): BevMessageStat {
    return { count: 0, bytes: 0, maxBytes: 0 };
}

function add(s: BevMessageStat, bytes: number) {
    s.count++;
    s.bytes += bytes;
    if (bytes > s.maxBytes) s.maxBytes = bytes;
}

function classify(url: string): BevSocketStats["kind"] {
    try {
        const path = new URL(url, location.href).pathname;
        if (/\/_blazor\/?$/i.test(path)) return "blazor-circuit";
    } catch { /* relative or invalid: fall through */ }
    return /[?&]id=/.test(url) ? "signalr" : "websocket";
}

function socketStats(socket: any, observedFromOpen: boolean): BevSocketStats {
    let s = sockets.get(socket);
    if (!s) {
        const url = String(socket.url || "");
        s = {
            id: ++nextSocketId,
            url: redactUrl(url),
            kind: classify(url),
            protocol: null,
            openedAt: observedFromOpen ? Date.now() : null,
            observedFrom: Date.now(),
            closedAt: null,
            closeCode: null,
            closeReason: null,
            sent: stat(),
            received: stat(),
            own: stat(),
            messages: {},
            renderBatches: { count: 0, bytes: 0, maxBytes: 0, applyMs: 0, maxApplyMs: 0, pending: 0, bevTools: 0 },
            largest: [],
        };
        sockets.set(socket, s);
        const stats = s;
        socket.addEventListener("message", (e: MessageEvent) => {
            // When the frame arrived: an existing socket's own handler (SignalR's) runs before this listener and has
            // already applied a render batch by now, so the time the batch took is measured from the event's time.
            frameArrivedAt = typeof e.timeStamp === "number" && e.timeStamp > 0 ? e.timeStamp : performance.now();
            try {
                onFrame(stats, "←", e.data);
            } finally {
                frameArrivedAt = null;
            }
        });
        socket.addEventListener("close", (e: CloseEvent) => {
            stats.closedAt = Date.now();
            stats.closeCode = e.code;
            stats.closeReason = e.reason ? String(e.reason).slice(0, 200) : null;
            trafficVersion++;
        });
        // Bounded: a page that opens sockets in a loop keeps the most recent ones.
        if (sockets.size > 50) {
            const oldest = sockets.keys().next().value;
            sockets.delete(oldest);
        }
        trafficVersion++;
    }
    return s;
}

function byteLength(data: any): number {
    if (typeof data === "string") return data.length;
    if (data && typeof data.byteLength === "number") return data.byteLength;
    if (data && typeof data.size === "number") return data.size;
    return 0;
}

function noteMessage(s: BevSocketStats, direction: string, kind: string, bytes: number) {
    // Only application traffic counts as a change worth reporting: BevTools' own reports travel on the same circuit,
    // and counting them as news would make every report schedule the next one.
    trafficVersion++;
    add(s.messages[direction + " " + kind] ||= stat(), bytes);
    if (bytes >= LARGE_MESSAGE) {
        s.largest.push({ direction, kind, bytes, at: Date.now() });
        s.largest.sort((a, b) => b.bytes - a.bytes);
        if (s.largest.length > 5) s.largest.length = 5;
    }
}

function onFrame(s: BevSocketStats, direction: "→" | "←", data: any) {
    const bytes = byteLength(data);
    add(direction === "→" ? s.sent : s.received, bytes);
    try {
        if (typeof data === "string") {
            decodeText(s, direction, data);
        } else if (s.kind !== "websocket" && (data instanceof ArrayBuffer || ArrayBuffer.isView(data))) {
            const view = data instanceof ArrayBuffer ? new Uint8Array(data) : new Uint8Array(data.buffer, data.byteOffset, data.byteLength);
            decodeBinary(s, direction, view);
        } else {
            noteMessage(s, direction, "frame", bytes);
        }
    } catch {
        noteMessage(s, direction, "frame", bytes);
    }
}

function decodeText(s: BevSocketStats, direction: string, text: string) {
    if (s.kind === "websocket" && text.indexOf(RECORD_SEPARATOR) < 0) {
        noteMessage(s, direction, "text", text.length);
        return;
    }
    for (const record of text.split(RECORD_SEPARATOR)) {
        if (!record) continue;
        if (s.protocol === null && record.indexOf('"protocol"') >= 0) {
            const m = /"protocol"\s*:\s*"([^"]+)"/.exec(record);
            s.protocol = m ? m[1] : null;
            noteMessage(s, direction, "handshake", record.length);
            continue;
        }
        // Only the envelope is read: type and target. Arguments - application data - are skipped, never kept.
        const type = /"type"\s*:\s*(\d+)/.exec(record);
        const target = /"target"\s*:\s*"([^"]+)"/.exec(record);
        const kind = describe(type ? +type[1] : -1, target ? target[1] : null);
        if (kind === "BeginInvokeDotNetFromJS" && direction === "→") {
            const args = /"arguments"\s*:\s*\[\s*"([^"]*)"\s*,\s*(null|"[^"]*")\s*,\s*"([^"]*)"\s*,\s*(\d+)/.exec(record);
            if (args && (isOwnObjectId(+args[4]) || (ownEventPending && args[3] === "DispatchEventAsync"))) {
                ownInvocations.add(args[1]);
                add(s.own, record.length);
                continue;
            }
        } else if (kind === "JS.EndInvokeDotNet" && direction === "←") {
            const args = /"arguments"\s*:\s*\[\s*"([^"]*)"/.exec(record);
            if (args && ownInvocations.delete(args[1])) {
                add(s.own, record.length);
                continue;
            }
        } else if (kind === "JS.BeginInvokeJS" && direction === "←") {
            // [asyncHandle, identifier, argsJson, resultType, targetInstanceId]
            const args = /"arguments"\s*:\s*\[\s*(\d+)\s*,\s*"(?:[^"\\]|\\.)*"\s*,\s*"((?:[^"\\]|\\.)*)"\s*,\s*\d+\s*,\s*(\d+)/.exec(record);
            if (args && isOwnModuleCall(+args[3], args[2].slice(0, 256))) {
                rememberOwnJsCall(+args[1]);
                add(s.own, record.length);
                continue;
            }
        } else if (kind === "EndInvokeJSFromDotNet" && direction === "→") {
            const args = /"arguments"\s*:\s*\[\s*(\d+)/.exec(record);
            if (args && ownJsCalls.delete(+args[1])) {
                add(s.own, record.length);
                continue;
            }
        }
        noteMessage(s, direction, kind, record.length);
    }
}

/**
 * A .NET -> JS call over a circuit into BevTools' own module: it names the module's JS object id, which the runtime
 * handed over on attach (or, for the import itself, this module's URL). Only Server runtimes count: a WebAssembly
 * runtime's calls never travel over a socket, and its object ids are numbered apart from a circuit's.
 */
function isOwnModuleCall(targetInstanceId: number | null, argsJson: string | null) {
    if (argsJson && argsJson.indexOf("_content/Bit.BevTools/bit-bevtools.js") >= 0) return true;
    let own = false;
    targets.forEach(t => { if (t.platform === "Server" && t.moduleObjectId !== null && t.moduleObjectId === targetInstanceId) own = true; });
    return own;
}

function rememberOwnJsCall(asyncHandle: number | null) {
    if (asyncHandle === null) return;
    ownJsCalls.add(asyncHandle);
    if (ownJsCalls.size > 256) ownJsCalls.delete(ownJsCalls.values().next().value!);
}

function isOwnObjectId(id: number) {
    let own = false;
    targets.forEach(t => { if (t.ref && t.ref._id === id) own = true; });
    return own;
}

function describe(type: number, target: string | null) {
    switch (type) {
        case 1: return target || "invocation";
        case 2: return "stream item";
        case 3: return "completion";
        case 4: return "stream invocation";
        case 5: return "cancel invocation";
        case 6: return "ping";
        case 7: return "close";
        case 8: return "ack";
        case 9: return "sequence";
        default: return target || "message";
    }
}

// A minimal MessagePack reader, enough for the SignalR envelope (blazorpack is SignalR's MessagePack protocol). It never
// materializes argument values: it skips them, reading only the message type, the target and the ids that tell
// BevTools' own messages and render batches apart.
class Pack {
    constructor(private readonly b: Uint8Array, public o: number, private readonly end: number) { }

    private byte() { if (this.o >= this.end) throw new Error("eof"); return this.b[this.o++]; }

    private uint(n: number) { let v = 0; for (let i = 0; i < n; i++) v = v * 256 + this.byte(); return v; }

    arrayLength(): number {
        const t = this.byte();
        if (t >= 0x90 && t <= 0x9f) return t & 0x0f;
        if (t === 0xdc) return this.uint(2);
        if (t === 0xdd) return this.uint(4);
        throw new Error("not an array");
    }

    int(): number | null {
        const t = this.byte();
        if (t <= 0x7f) return t;
        if (t >= 0xe0) return t - 0x100;
        switch (t) {
            case 0xc0: return null;
            case 0xcc: return this.uint(1);
            case 0xcd: return this.uint(2);
            case 0xce: return this.uint(4);
            case 0xcf: return this.uint(8);
            case 0xd0: { const v = this.uint(1); return v > 0x7f ? v - 0x100 : v; }
            case 0xd1: { const v = this.uint(2); return v > 0x7fff ? v - 0x10000 : v; }
            case 0xd2: { const v = this.uint(4); return v > 0x7fffffff ? v - 0x100000000 : v; }
            case 0xd3: return this.uint(8);
            default: this.o--; this.skip(); return null;
        }
    }

    str(): string | null {
        const t = this.byte();
        let n: number;
        if (t >= 0xa0 && t <= 0xbf) n = t & 0x1f;
        else if (t === 0xd9) n = this.uint(1);
        else if (t === 0xda) n = this.uint(2);
        else if (t === 0xdb) n = this.uint(4);
        else if (t === 0xc0) return null;
        else { this.o--; this.skip(); return null; }
        const start = this.o;
        this.o += n;
        if (this.o > this.end) throw new Error("eof");
        let text = "";
        // Targets and ids are ASCII; anything longer than this is a payload and is not wanted.
        for (let i = start; i < this.o && i - start < 256; i++) text += String.fromCharCode(this.b[i]);
        return text;
    }

    /** The size of a binary value, without reading it: a render batch's size is the size of its bin argument. */
    binLength(): number | null {
        const t = this.byte();
        let n: number;
        if (t === 0xc4) n = this.uint(1);
        else if (t === 0xc5) n = this.uint(2);
        else if (t === 0xc6) n = this.uint(4);
        else { this.o--; this.skip(); return null; }
        this.o += n;
        return n;
    }

    skip(): void {
        const t = this.byte();
        if (t <= 0x7f || t >= 0xe0 || t === 0xc0 || t === 0xc2 || t === 0xc3) return;
        if (t >= 0x80 && t <= 0x8f) { for (let i = 0; i < (t & 0x0f) * 2; i++) this.skip(); return; }
        if (t >= 0x90 && t <= 0x9f) { for (let i = 0; i < (t & 0x0f); i++) this.skip(); return; }
        if (t >= 0xa0 && t <= 0xbf) { this.o += t & 0x1f; return; }
        switch (t) {
            case 0xc4: this.o += this.uint(1); return;
            case 0xc5: this.o += this.uint(2); return;
            case 0xc6: this.o += this.uint(4); return;
            case 0xc7: this.o += this.uint(1) + 1; return;
            case 0xc8: this.o += this.uint(2) + 1; return;
            case 0xc9: this.o += this.uint(4) + 1; return;
            case 0xca: this.o += 4; return;
            case 0xcb: this.o += 8; return;
            case 0xcc: case 0xd0: this.o += 1; return;
            case 0xcd: case 0xd1: this.o += 2; return;
            case 0xce: case 0xd2: this.o += 4; return;
            case 0xcf: case 0xd3: this.o += 8; return;
            case 0xd4: this.o += 2; return;
            case 0xd5: this.o += 3; return;
            case 0xd6: this.o += 5; return;
            case 0xd7: this.o += 9; return;
            case 0xd8: this.o += 17; return;
            case 0xd9: this.o += this.uint(1); return;
            case 0xda: this.o += this.uint(2); return;
            case 0xdb: this.o += this.uint(4); return;
            case 0xdc: { const n = this.uint(2); for (let i = 0; i < n; i++) this.skip(); return; }
            case 0xdd: { const n = this.uint(4); for (let i = 0; i < n; i++) this.skip(); return; }
            case 0xde: { const n = this.uint(2); for (let i = 0; i < n * 2; i++) this.skip(); return; }
            case 0xdf: { const n = this.uint(4); for (let i = 0; i < n * 2; i++) this.skip(); return; }
            default: throw new Error("unknown type " + t);
        }
    }
}

function decodeBinary(s: BevSocketStats, direction: string, bytes: Uint8Array) {
    let offset = 0;
    while (offset < bytes.length) {
        // Each SignalR binary message is prefixed with its length as a VarInt.
        let length = 0, shift = 0, b = 0, n = 0;
        do {
            b = bytes[offset + n];
            length |= (b & 0x7f) << shift;
            shift += 7;
            n++;
        } while ((b & 0x80) !== 0 && n < 5 && offset + n < bytes.length);
        const start = offset + n;
        const end = Math.min(start + length, bytes.length);
        // Seen mid-stream, after the handshake that names it: a circuit speaks blazorpack, an app's hub MessagePack.
        if (s.protocol === null) s.protocol = s.kind === "blazor-circuit" ? "blazorpack" : "messagepack";
        decodePacked(s, direction, bytes, start, end);
        offset = end;
        if (length === 0) break;
    }
}

function decodePacked(s: BevSocketStats, direction: string, bytes: Uint8Array, start: number, end: number) {
    const size = end - start;
    const r = new Pack(bytes, start, end);
    let kind = "message";
    try {
        const items = r.arrayLength();
        const type = r.int() ?? -1;
        if (type === 1 && items >= 5) {
            r.skip(); // headers
            const invocationId = r.str();
            const target = r.str();
            kind = describe(type, target);
            const argCount = r.arrayLength();
            if (target === "BeginInvokeDotNetFromJS" && argCount >= 4 && direction === "→") {
                const callId = r.str();
                r.skip(); // assembly name
                const method = r.str();
                const objectId = r.int();
                if ((objectId !== null && isOwnObjectId(objectId)) || (ownEventPending && method === "DispatchEventAsync")) {
                    if (callId) ownInvocations.add(callId);
                    add(s.own, size);
                    return;
                }
            } else if (target === "JS.EndInvokeDotNet" && argCount >= 1 && direction === "←") {
                const callId = r.str();
                if (callId && ownInvocations.delete(callId)) {
                    add(s.own, size);
                    return;
                }
            } else if (target === "JS.BeginInvokeJS" && argCount >= 5 && direction === "←") {
                const asyncHandle = r.int();
                r.skip(); // identifier
                const argsJson = r.str();
                r.skip(); // result type
                if (isOwnModuleCall(r.int(), argsJson)) {
                    rememberOwnJsCall(asyncHandle);
                    add(s.own, size);
                    return;
                }
            } else if (target === "EndInvokeJSFromDotNet" && argCount >= 1 && direction === "→") {
                const asyncHandle = r.int();
                if (asyncHandle !== null && ownJsCalls.delete(asyncHandle)) {
                    add(s.own, size);
                    return;
                }
            } else if (target === "JS.RenderBatch" && argCount >= 2 && direction === "←") {
                // Counted once the browser acknowledges it: only then is it known whose DOM it changed.
                const batchId = r.int();
                const batchBytes = r.binLength() ?? size;
                const arrived = frameArrivedAt ?? performance.now();
                if (batchId === null) {
                    noteRenderBatch(s, batchBytes, size, null);
                    return;
                }
                const key = s.id + ":" + batchId;
                const early = acknowledgedEarly.get(key);
                if (early !== undefined) {
                    acknowledgedEarly.delete(key);
                    settleRenderBatch(s, early.dom, batchBytes, size, early.size, early.at - arrived);
                    return;
                }
                pendingRenderBatches.set(key, { at: arrived, bytes: batchBytes, size });
                if (pendingRenderBatches.size > 256) pendingRenderBatches.delete(pendingRenderBatches.keys().next().value!);
                s.renderBatches.pending = pendingRenderBatches.size;
                return;
            } else if (target === "OnRenderCompleted" && argCount >= 1 && direction === "→") {
                const key = s.id + ":" + r.int();
                // The browser sends this right after applying the batch, so the DOM changes since the previous
                // acknowledgement are that batch's. One that changed only BevTools' panel was the panel rendering.
                const dom = takeDomChanges();
                const batch = pendingRenderBatches.get(key);
                if (batch === undefined) {
                    acknowledgedEarly.set(key, { at: performance.now(), dom, size });
                    if (acknowledgedEarly.size > 256) acknowledgedEarly.delete(acknowledgedEarly.keys().next().value!);
                    return;
                }
                pendingRenderBatches.delete(key);
                s.renderBatches.pending = pendingRenderBatches.size;
                settleRenderBatch(s, dom, batch.bytes, batch.size, size, performance.now() - batch.at);
                return;
            }
            void invocationId;
        } else {
            kind = describe(type, null);
        }
    } catch {
        kind = "message";
    }
    noteMessage(s, direction, kind, size);
}

function wrapWebSockets(): boolean {
    const WS = (window as any).WebSocket;
    if (!WS) return false;
    if (originalSend) return true;
    // The sockets that exist already (a circuit, an app's hub connection) are found on their first send: SignalR
    // pings every 15 s, so even a quiet connection shows up within that time.
    originalSend = WS.prototype.send;
    WS.prototype.send = function (this: any, data: any) {
        if (targets.size > 0) {
            try { onFrame(socketStats(this, false), "→", data); } catch { /* never break the app's socket */ }
        }
        return originalSend.call(this, data);
    };
    // Sockets created from now on are seen from their first frame.
    originalWebSocket = WS;
    const Wrapped = function (this: any, url: string, protocols?: any) {
        const socket = protocols === undefined ? new originalWebSocket(url) : new originalWebSocket(url, protocols);
        if (targets.size > 0) {
            try { socketStats(socket, true); } catch { /* ignore */ }
        }
        return socket;
    } as any;
    Wrapped.prototype = WS.prototype;
    for (const key of ["CONNECTING", "OPEN", "CLOSING", "CLOSED"]) Wrapped[key] = WS[key];
    (window as any).WebSocket = Wrapped;
    return true;
}

function unwrapWebSockets() {
    const WS = originalWebSocket || (window as any).WebSocket;
    if (originalSend && WS) WS.prototype.send = originalSend;
    if (originalWebSocket) (window as any).WebSocket = originalWebSocket;
    originalSend = null;
    originalWebSocket = null;
}

/** The traffic the page's WebSockets carried since BevTools attached; sizes and message kinds only. */
export function traffic(): BevSocketStats[] {
    return Array.from(sockets.values()).map(s => ({ ...s, messages: { ...s.messages }, largest: s.largest.slice() }));
}

function reportTraffic() {
    if (reportedTrafficVersion === trafficVersion || targets.size === 0) return;
    reportedTrafficVersion = trafficVersion;
    const snapshot = traffic();
    broadcast("ReportTraffic", snapshot);
    notify({ type: "traffic" });
}

// ----- browser facts -----

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

function installPageListeners() {
    on(window, "error", (e: ErrorEvent) => {
        const element = e.target as any;
        if (element && element !== window && element.tagName) {
            // A <script>, <link>, <img> that failed to load: with Blazor typically a _framework file, a stylesheet of a
            // component library, or a module import. Errors from elements do not bubble, hence the capture listener.
            const url = element.src || element.href || "";
            broadcast("OnJsError", {
                message: "Failed to load <" + String(element.tagName).toLowerCase() + "> " + redactUrl(String(url)),
                source: redactUrl(String(url)) || null,
                stack: null,
                type: "ResourceLoadError",
            });
            return;
        }
        const err = e.error;
        if (isConnectionDown(err)) return;
        broadcast("OnJsError", {
            message: e.message || (err && err.message) || "Unknown error",
            source: e.filename ? redactUrl(e.filename) + ":" + e.lineno + ":" + e.colno : null,
            stack: err && err.stack ? String(err.stack).slice(0, 4000) : null,
            type: err && err.name ? err.name : "Error",
        });
    }, true);
    on(window, "unhandledrejection", (e: PromiseRejectionEvent) => {
        const reason = e.reason;
        if (isConnectionDown(reason)) return;
        broadcast("OnJsError", {
            message: reason && reason.message ? reason.message : String(reason),
            source: null,
            stack: reason && reason.stack ? String(reason.stack).slice(0, 4000) : null,
            type: "UnhandledRejection",
        });
    });
    on(document, "securitypolicyviolation", (e: SecurityPolicyViolationEvent) => {
        // Blazor WebAssembly needs 'wasm-unsafe-eval' in script-src; a policy without it stops the runtime here.
        broadcast("OnJsError", {
            message: "Content Security Policy blocked " + (e.blockedURI ? redactUrl(e.blockedURI) : "an inline resource") + " (" + (e.effectiveDirective || e.violatedDirective) + ")",
            source: e.sourceFile ? redactUrl(e.sourceFile) + ":" + e.lineNumber + ":" + e.columnNumber : null,
            stack: null,
            type: "CspViolation",
        });
    });
    on(document, "mouseover", onTreeHover, true);
    on(document, "mouseout", onTreeLeave, true);
    for (const type of OWN_EVENT_TYPES) on(document, type, markOwnEvent, true);
    on(window, "online", () => broadcast("OnBrowserEvent", "online", null));
    on(window, "offline", () => broadcast("OnBrowserEvent", "offline", null));
    on(document, "visibilitychange", () => broadcast("OnBrowserEvent", "visibility", document.visibilityState));
    on(document, "components:reconnect-state-changed", (e: CustomEvent) => {
        const d = e.detail || {};
        broadcast("OnReconnectState", d.state || "unknown", d.currentAttempt || 0, d.secondsToNextAttempt || 0);
    });
    on(document, "keydown", (e: KeyboardEvent) => {
        // The panel's tab list moves with the arrow keys (handled by the panel); the page must not scroll as well.
        const element = e.target as Element | null;
        if (element && typeof element.closest === "function" && element.closest(".bev-root [role=tablist]") && /^(Arrow(Left|Right|Up|Down)|Home|End)$/.test(e.key)) {
            e.preventDefault();
        }
        const target = focusedTarget();
        if (!target) return;
        if (matchesShortcut(e)) {
            e.preventDefault();
            invoke(target, "OnToggleShortcut");
        } else if (isPaletteShortcut(e) && isPanelOpen()) {
            // While the panel is open Ctrl+K belongs to it wherever focus is (opening the panel from the badge leaves
            // focus on the page), and the browser must not also act on it (Chrome focuses its address bar).
            e.preventDefault();
            invoke(target, "OnPaletteShortcut");
        }
    }, true);

    // Blazor Web Apps update the page without a reload ("enhanced navigation"); the browser has no other trace of it.
    const blazor = (window as any).Blazor;
    if (blazor && typeof blazor.addEventListener === "function") {
        enhancedLoadHandler = () => broadcast("OnBrowserEvent", "enhancedload", redactUrl(location.pathname + location.search));
        try { blazor.addEventListener("enhancedload", enhancedLoadHandler); } catch { enhancedLoadHandler = null; }
    }
}

function removePageListeners() {
    while (listeners.length) listeners.pop()!();
    const blazor = (window as any).Blazor;
    if (enhancedLoadHandler && blazor && typeof blazor.removeEventListener === "function") {
        try { blazor.removeEventListener("enhancedload", enhancedLoadHandler); } catch { /* ignore */ }
    }
    enhancedLoadHandler = null;
}

// ----- components in the page -----
//
// Blazor's browser renderer keeps a logical element tree on the DOM nodes it creates: under Symbol() keys, the array of
// a node's logical children and a pointer to its logical parent, and a `true` marker on the container of every
// interactive root component while it is attached. The keys carry no description, so they are learned from a node
// known to be Blazor's - a BevTools panel's element - by the one relation that defines them: the parent's children
// array holds the node. Paths through the tree come from .NET (BevDomMap), computed from the components' render
// trees, and every step is checked against the node found here: a mismatch is reported, never guessed around.

export interface BevDomStep {
    index: number;
    kind: string;
}

export interface BevDomLocation {
    instanceId: number;
    name: string;
    anchored: boolean;
    rootSignature: string;
    rootOrdinal: number;
    /** How many roots of this runtime share the signature: the ones `rootOrdinal` counts. */
    rootCount?: number;
    rootAmbiguous?: boolean;
    path: BevDomStep[];
}

export interface BevHighlightOptions {
    /** Scroll the component's first element into view first. */
    scroll?: boolean;
    /** Remove the outline after this long; it otherwise stays until the next highlight, a scroll, or clearHighlight. */
    durationMs?: number;
}

interface BevResolved {
    targetId: string;
    instanceId: number;
    name: string;
    renderedBy: string | null;
    element: string | null;
    ambiguous: boolean;
    container: Node;
}

interface BevPickResult {
    targetId?: string;
    instanceId?: number;
    name?: string;
    renderedBy?: string | null;
    element?: string | null;
    error?: string;
    at: number;
}

const NO_TREE = "This page's Blazor renderer does not expose the logical element tree BevTools maps components with (an unsupported Blazor version, or no BevTools panel rendered yet).";
const STALE = "The page does not match the component's latest render: a render is still on its way to the browser, or script changed the DOM. Try again.";

let childrenKey: symbol | null = null;
let parentKey: symbol | null = null;
// Boolean keys ordinary Blazor nodes carry too, so they cannot be what marks a root container. Empty in browsers; a page
// whose other libraries (or DOM implementation) keep flags on nodes under symbols would otherwise make every node a root.
let commonKeys = new Set<symbol>();

function learnKeys(): boolean {
    if (childrenKey && parentKey) return true;
    if (typeof document === "undefined" || !document.querySelectorAll) return false;
    // The panel's element is the first node of the panel component, so its logical parent is that component's
    // container: a comment. No physical parent is a comment, which keeps any other symbol-keyed tree on the nodes (a
    // DOM implementation's own, another library's) from being mistaken for Blazor's. Only when no panel has one is
    // the panel a root of its own (RootComponents.Add<BevToolsPanel>), whose container is an element carrying a marker
    // the panel's element does not.
    const panels = Array.from(document.querySelectorAll(".bev-root[data-bev-target]"));
    for (const commentParent of [true, false]) {
        for (const el of panels) {
            if (learnKeysFrom(el, commentParent)) return true;
        }
    }
    return false;
}

function learnKeysFrom(el: Element, commentParent: boolean): boolean {
    for (const key of Object.getOwnPropertySymbols(el)) {
        const parent = (el as any)[key];
        if (!parent || typeof parent !== "object" || typeof parent.nodeType !== "number") continue;
        if (commentParent ? parent.nodeType !== 8 : parent.nodeType !== 1 || !booleanKeys(parent, true).some(k => (el as any)[k] !== true)) continue;
        for (const listKey of Object.getOwnPropertySymbols(parent)) {
            const list = parent[listKey];
            if (Array.isArray(list) && list.indexOf(el) >= 0 && Array.isArray((el as any)[listKey])) {
                parentKey = key;
                childrenKey = listKey;
                // Learned from what the panel itself rendered - its element and its child components' containers -
                // which are never root containers, whatever the panel's own parent is.
                commonKeys = new Set();
                for (const ordinary of panelNodes(el)) booleanKeys(ordinary).forEach(k => commonKeys.add(k));
                return true;
            }
        }
    }
    return false;
}

function booleanKeys(node: any, value?: boolean): symbol[] {
    return Object.getOwnPropertySymbols(node).filter(key => typeof node[key] === "boolean" && (value === undefined || node[key] === value));
}

function panelNodes(panel: Node): Node[] {
    const nodes: Node[] = [panel];
    for (let i = 0; i < nodes.length && nodes.length < 64; i++) {
        for (const child of logicalChildren(nodes[i]) || []) if (child.nodeType !== 3) nodes.push(child);
    }
    return nodes;
}

function logicalChildren(node: any): Node[] | null {
    const list = childrenKey && node ? node[childrenKey] : null;
    return Array.isArray(list) ? list : null;
}

function logicalParent(node: any): Node | null {
    const parent = parentKey && node ? node[parentKey] : null;
    return parent && typeof parent.nodeType === "number" ? parent : null;
}

/**
 * An attached interactive root's container: Blazor marks it `true` when the root attaches and sets that mark `false`
 * when it is disposed - while a Web App root's second mark (preserve content) stays `true`. So a container is a live
 * root while it carries a `true` mark and no `false` one.
 */
function isRootContainer(node: any): boolean {
    if (!logicalChildren(node)) return false;
    let marked = false;
    for (const key of Object.getOwnPropertySymbols(node)) {
        if (key === childrenKey || key === parentKey || commonKeys.has(key)) continue;
        const value = node[key];
        if (value === false) return false;
        if (value === true) marked = true;
    }
    return marked;
}

function kindOf(node: Node): string {
    switch (node.nodeType) {
        case 1: return (node as Element).tagName.toLowerCase();
        case 3: return "#text";
        case 8: return "#c";
        default: return "#" + node.nodeType;
    }
}

function rootOf(node: Node | null): Node | null {
    for (let n = node, hops = 0; n && hops < 10000; n = logicalParent(n), hops++) {
        if (isRootContainer(n)) return n;
    }
    return null;
}

/** The root container a runtime's BevTools panel renders in: the anchor of every path that runtime calls anchored. */
function anchorRoot(targetId: string): Node | null {
    const panel = document.querySelector(".bev-root[data-bev-target=\"" + String(targetId).replace(/["\\]/g, "") + "\"]");
    return panel ? rootOf(logicalParent(panel)) : null;
}

let rootsCache: { at: number; roots: Node[] } | null = null;

/**
 * The interactive roots no BevTools panel renders in, in document order: the ones matched by signature and position.
 * Every runtime's anchor root is left out here and in .NET alike, so positions count the same roots on both sides. A
 * walk over the whole document, so it is reused for a second: picking and render flashes ask often.
 */
function unanchoredRoots(fresh = false): Node[] {
    const now = Date.now();
    if (fresh || !rootsCache || now - rootsCache.at > 1000) {
        const anchors = new Set<Node>();
        targets.forEach(t => { const anchor = anchorRoot(t.id); if (anchor) anchors.add(anchor); });
        const roots: Node[] = [];
        const walker = document.createTreeWalker(document.documentElement, 0x1 | 0x80 /* SHOW_ELEMENT | SHOW_COMMENT */);
        for (let n: Node | null = walker.currentNode; n; n = walker.nextNode()) {
            if (!anchors.has(n) && isRootContainer(n)) roots.push(n);
        }
        rootsCache = { at: now, roots };
    }
    return rootsCache.roots;
}

/** "3|div,#text,#c", as BevDomMap.Signature computes it from the root's frames. */
function signatureOf(container: Node): string {
    const list = logicalChildren(container) || [];
    return list.length + "|" + list.slice(0, 12).map(kindOf).join(",");
}

function walkPath(from: Node, steps: BevDomStep[]): Node | null {
    let node: Node | null = from;
    for (const step of steps) {
        const list = logicalChildren(node);
        const next: Node | undefined = list ? list[step.index] : undefined;
        if (!next || kindOf(next) !== step.kind) return null;
        node = next;
    }
    return node;
}

const AMBIGUOUS = "Several identical interactive roots on the page render this, and BevTools cannot tell which one it is.";

function findContainer(targetId: string, location: BevDomLocation): { node: Node; positional?: boolean } | { error: string } {
    if (!learnKeys()) return { error: NO_TREE };
    if (location.anchored) {
        const root = anchorRoot(targetId);
        if (!root) return { error: "The BevTools panel of this runtime is not in the page." };
        const node = walkPath(root, location.path || []);
        return node ? { node } : { error: STALE };
    }
    for (const fresh of [false, true]) {
        // A position means something only against the current count of identical roots: never from the cache.
        if (!fresh && (location.rootCount || 1) > 1) continue;
        const same = unanchoredRoots(fresh).filter(r => r.isConnected && signatureOf(r) === location.rootSignature);
        if (same.length === 0) continue;
        // Exactly the roots the runtime counted: the position it gives is one of them. Otherwise another runtime's
        // identical roots are among them, and only a path that fits a single one of them identifies it.
        if (same.length === (location.rootCount || 1)) {
            const node = walkPath(same[Math.min(location.rootOrdinal, same.length - 1)], location.path || []);
            if (node) return { node, positional: same.length > 1 };
            continue;
        }
        const fitting = same.map(r => walkPath(r, location.path || [])).filter(n => n !== null) as Node[];
        if (fitting.length === 1) return { node: fitting[0] };
        if (fitting.length > 1) return { error: AMBIGUOUS };
    }
    return { error: "The root component of " + location.name + " is not in the page, or no longer matches its latest render." };
}

/** The boxes a component's output occupies: its elements and text, through nested component and markup containers. */
function rectsOf(container: Node, out: DOMRect[], budget: { n: number }) {
    for (const child of logicalChildren(container) || []) {
        if (budget.n-- <= 0) return;
        if (child.nodeType === 1) {
            const rect = (child as Element).getBoundingClientRect();
            if (rect.width > 0 || rect.height > 0) out.push(rect);
            else rectsOf(child, out, budget); // display: contents, or an empty element whose children still show
        } else if (child.nodeType === 3) {
            if (!(child.textContent || "").trim()) continue;
            const range = document.createRange();
            range.selectNodeContents(child);
            const rect = range.getBoundingClientRect();
            if (rect.width > 0 || rect.height > 0) out.push(rect);
        } else if (child.nodeType === 8) {
            rectsOf(child, out, budget);
        }
    }
}

function boundsOf(container: Node): { left: number; top: number; width: number; height: number } | null {
    const rects: DOMRect[] = [];
    if (container.nodeType === 1 && !isRootContainer(container)) {
        rects.push((container as Element).getBoundingClientRect());
    } else {
        rectsOf(container, rects, { n: 5000 });
    }
    if (rects.length === 0) return null;
    let left = Infinity, top = Infinity, right = -Infinity, bottom = -Infinity;
    for (const r of rects) {
        left = Math.min(left, r.left);
        top = Math.min(top, r.top);
        right = Math.max(right, r.right);
        bottom = Math.max(bottom, r.bottom);
    }
    return { left, top, width: right - left, height: bottom - top };
}

function firstElement(container: Node): Element | null {
    for (const child of logicalChildren(container) || []) {
        if (child.nodeType === 1) return child as Element;
        if (child.nodeType === 8) {
            const nested = firstElement(child);
            if (nested) return nested;
        }
    }
    return null;
}

// The overlay lives outside every element Blazor manages and carries .bev-root, so its DOM changes count as BevTools'
// own wherever the hub classifies mutations, and picking ignores it.
let overlay: HTMLElement | null = null;
let highlightBox: HTMLElement | null = null;
let highlightTimer: ReturnType<typeof setTimeout> | null = null;
let followListener: (() => void) | null = null;
let outlined: { node: Node; label: string; note: string | null } | null = null;

function overlayRoot(): HTMLElement {
    if (overlay && overlay.isConnected) return overlay;
    overlay = document.createElement("div");
    overlay.className = "bev-root bev-overlay";
    overlay.setAttribute("aria-hidden", "true");
    document.body.appendChild(overlay);
    return overlay;
}

function place(el: HTMLElement, bounds: { left: number; top: number; width: number; height: number }) {
    el.style.left = bounds.left + "px";
    el.style.top = bounds.top + "px";
    el.style.width = Math.max(1, bounds.width) + "px";
    el.style.height = Math.max(1, bounds.height) + "px";
}

function drawBox(bounds: { left: number; top: number; width: number; height: number } | null, label: string, note?: string | null) {
    const root = overlayRoot();
    if (!highlightBox || !highlightBox.isConnected) {
        highlightBox = document.createElement("div");
        highlightBox.className = "bev-hl";
        const tag = document.createElement("div");
        tag.className = "bev-hl-label";
        highlightBox.appendChild(tag);
        root.appendChild(highlightBox);
    }
    const tag = highlightBox.firstChild as HTMLElement;
    if (bounds) {
        place(highlightBox, bounds);
        highlightBox.classList.remove("bev-hl-empty");
        tag.textContent = label + " · " + Math.round(bounds.width) + "×" + Math.round(bounds.height) + (note ? " · " + note : "");
        // Inside the viewport even for a component at its top edge.
        tag.classList.toggle("bev-hl-label-inside", bounds.top < 22);
    } else {
        highlightBox.classList.add("bev-hl-empty");
        tag.textContent = label + (note ? " · " + note : "");
    }
}

/**
 * Outlines a component's container and keeps the outline on it while the page scrolls or resizes - scrolling to show a
 * component is the first thing Show in page does - until it is cleared or the component's nodes leave the page.
 */
function outline(node: Node | null, label: string, note?: string | null) {
    outlined = node ? { node, label, note: note || null } : null;
    const bounds = node ? boundsOf(node) : null;
    drawBox(bounds, label, bounds || !node ? note : (note ? note + " · " : "") + "renders nothing visible");
    if (!followListener) {
        let scheduled = false;
        followListener = () => {
            if (scheduled) return;
            scheduled = true;
            requestAnimationFrame(() => {
                scheduled = false;
                const current = outlined;
                if (!current) return;
                if (!current.node.isConnected) { clearHighlight(); return; }
                drawBox(boundsOf(current.node), current.label, current.note);
            });
        };
        window.addEventListener("scroll", followListener, { capture: true, passive: true });
        window.addEventListener("resize", followListener, { passive: true });
    }
    return !!bounds;
}

/** Outlines the component at a location the runtime computed; the panel calls this, the protocol path goes through highlightComponent. */
export function highlight(targetId: string, location: BevDomLocation, options?: BevHighlightOptions | null) {
    clearHighlight();
    const found = findContainer(targetId, location);
    if ("error" in found) return { ok: false, error: found.error };
    if (options && options.scroll) {
        const first = firstElement(found.node);
        if (first && typeof first.scrollIntoView === "function") first.scrollIntoView({ block: "nearest", inline: "nearest" });
    }
    const visible = outline(found.node, location.name, found.positional ? "one of several identical roots, matched by position" : null);
    if (options && options.durationMs) highlightTimer = setTimeout(clearHighlight, options.durationMs);
    return { ok: true, visible };
}

/** Outlines a component of a runtime by its instance id: the companion window and the extension use this. */
export async function highlightComponent(targetId: string, instanceId: number, options?: BevHighlightOptions | null) {
    try {
        const location = await request(targetId, "locate", { instanceId });
        if (!location || location.error) return { ok: false, error: location ? location.error : "No answer." };
        return highlight(targetId, location, options);
    } catch (err: any) {
        return { ok: false, error: String(err && err.message || err) };
    }
}

export function clearHighlight() {
    if (highlightTimer) { clearTimeout(highlightTimer); highlightTimer = null; }
    if (followListener) {
        window.removeEventListener("scroll", followListener, { capture: true } as any);
        window.removeEventListener("resize", followListener);
        followListener = null;
    }
    outlined = null;
    if (highlightBox) { highlightBox.remove(); highlightBox = null; }
}

const reducedMotion = () => typeof matchMedia === "function" && matchMedia("(prefers-reduced-motion: reduce)").matches;

/** Outlines components that just rendered, coloured by how many times since the last flash (React DevTools' "highlight updates"). */
export function flash(targetId: string, items: { location: BevDomLocation; count: number }[]) {
    if (!items || items.length === 0 || !learnKeys()) return 0;
    const root = overlayRoot();
    let drawn = 0;
    for (const item of items.slice(0, 150)) {
        const found = findContainer(targetId, item.location);
        if ("error" in found) continue;
        const bounds = boundsOf(found.node);
        if (!bounds) continue;
        const box = document.createElement("div");
        const count = item.count || 1;
        box.className = "bev-flash-box " + (count >= 10 ? "bev-flash-4" : count >= 4 ? "bev-flash-3" : count >= 2 ? "bev-flash-2" : "bev-flash-1");
        box.title = item.location.name;
        place(box, bounds);
        root.appendChild(box);
        drawn++;
        if (!reducedMotion()) requestAnimationFrame(() => box.classList.add("bev-flash-out"));
        setTimeout(() => box.remove(), 800);
    }
    return drawn;
}

// Hovering a row of a BevTools component tree outlines its component in the page. Handled here rather than by a
// Blazor event on each row: over a circuit every Blazor event is a message to the server, and hovering a tree is many.
let hoveredRow: Element | null = null;

function onTreeHover(e: MouseEvent) {
    const element = e.target as Element | null;
    const row = element && typeof element.closest === "function" ? element.closest(".bev-root[data-bev-target] .bev-tree [data-bev-id]") : null;
    if (!row || row === hoveredRow) return;
    hoveredRow = row;
    const panel = row.closest(".bev-root[data-bev-target]")!;
    const id = Number(row.getAttribute("data-bev-id"));
    if (Number.isFinite(id)) highlightComponent(panel.getAttribute("data-bev-target")!, id);
}

function onTreeLeave(e: MouseEvent) {
    if (!hoveredRow) return;
    const tree = hoveredRow.closest(".bev-tree");
    const to = e.relatedTarget as Node | null;
    if (tree && to && tree.contains(to)) return;
    hoveredRow = null;
    clearHighlight();
}

/** The component that rendered a node: resolved by the runtime the node belongs to, which verifies the path. */
async function resolveNode(node: Node | null, includeHidden: boolean): Promise<BevResolved | { error: string }> {
    if (!learnKeys()) return { error: NO_TREE };
    let n: Node | null = node;
    while (n && !logicalParent(n) && !isRootContainer(n)) n = n.parentNode;
    if (!n) return { error: "Blazor did not render this part of the page (static markup, or added by script)." };
    const steps: BevDomStep[] = [];
    let root: Node | null = null;
    for (let hops = 0; n && hops < 10000; hops++) {
        if (isRootContainer(n)) { root = n; break; }
        const parent = logicalParent(n);
        if (!parent) break;
        const list = logicalChildren(parent);
        const index = list ? list.indexOf(n) : -1;
        if (index < 0) return { error: STALE };
        steps.unshift({ index, kind: kindOf(n) });
        n = parent;
    }
    if (!root) return { error: "This part of the page is not inside an interactive component: it was rendered statically on the server (or is still prerendered)." };

    let anchoredTo: BevTarget | null = null;
    for (const t of Array.from(targets.values())) {
        if (anchorRoot(t.id) === root) { anchoredTo = t; break; }
    }
    const signature = signatureOf(root);
    const same = anchoredTo ? [root] : unanchoredRoots(true).filter(r => signatureOf(r) === signature);
    const ordinal = Math.max(0, same.indexOf(root));
    // A root no panel renders in could be any runtime's: every one is asked, and an answer counts only if it is the
    // only one - two runtimes both finding the path is two identical roots, and picking either would be a guess.
    const order: BevTarget[] = anchoredTo ? [anchoredTo] : Array.from(targets.values());
    let error = "No BevTools runtime on this page rendered it.";
    const found: BevResolved[] = [];
    for (const t of order) {
        try {
            const answer = await request(t.id, "resolve", { path: steps, anchored: t === anchoredTo, signature, ordinal, sameCount: same.length, includeHidden });
            if (!answer || answer.error) { error = answer ? answer.error : error; continue; }
            const container = answer.depth < 0 ? root : walkPath(root, steps.slice(0, answer.depth));
            if (!container) return { error: STALE };
            found.push({ targetId: t.id, instanceId: answer.instanceId, name: answer.name, renderedBy: answer.renderedBy || null, element: answer.element || null, ambiguous: !!answer.ambiguous, container });
        } catch (err: any) {
            error = String(err && err.message || err);
        }
    }
    if (found.length > 1) return { error: AMBIGUOUS };
    return found[0] || { error };
}

/** What owns a node, without the node itself: what the extension's Elements panel selection resolves to. */
export async function componentAt(node: Node | null, includeHidden?: boolean): Promise<BevPickResult> {
    const resolved = await resolveNode(node, !!includeHidden);
    if ("error" in resolved) return { error: resolved.error, at: Date.now() };
    return { targetId: resolved.targetId, instanceId: resolved.instanceId, name: resolved.name, renderedBy: resolved.renderedBy, element: resolved.element, at: Date.now() };
}

interface PickState {
    targetId: string | null;
    includeHidden: boolean;
    tellPanel: boolean;
    hovered: Node | null;
    seq: number;
}

let picking: PickState | null = null;
// Between the picking click and its answer: picking has ended in the page, the outcome is not known yet.
let resolvingPick = false;
let lastPicked: BevPickResult | null = null;
const pickListeners: (() => void)[] = [];

// Not passive: a touch must be able to cancel the click it would become (Chromium makes window touch listeners passive by default).
const PICK_LISTENER: AddEventListenerOptions = { capture: true, passive: false };

function onPick(type: string, handler: (e: any) => void) {
    window.addEventListener(type, handler, PICK_LISTENER);
    pickListeners.push(() => window.removeEventListener(type, handler, PICK_LISTENER));
}

/** The panel's own Pick button: a click on it stops picking through the panel, so the hub lets it through untouched. */
function isPickToggle(node: Node | null) {
    const element = node && (node.nodeType === 1 ? node as Element : node.parentElement);
    return !!(element && typeof element.closest === "function" && element.closest("[data-bev-pick-toggle]"));
}

function showNote(text: string, x: number, y: number) {
    const note = document.createElement("div");
    note.className = "bev-hl-note";
    note.textContent = text;
    note.style.left = Math.max(4, Math.min(x + 12, window.innerWidth - 320)) + "px";
    note.style.top = Math.max(4, Math.min(y + 12, window.innerHeight - 60)) + "px";
    overlayRoot().appendChild(note);
    setTimeout(() => note.remove(), 3500);
}

/**
 * Starts picking a component in the page: hovering outlines the component that rendered the element under the
 * pointer, a click selects it, Escape cancels. The click never reaches the application. A click in BevTools' own UI
 * ends picking and goes through.
 */
export function startPick(targetId?: string | null, options?: { includeHidden?: boolean; tellPanel?: boolean } | null): boolean {
    stopPick();
    if (!learnKeys()) {
        lastPicked = { error: NO_TREE, at: Date.now() };
        notify({ type: "pick", state: "unavailable", error: NO_TREE });
        return false;
    }
    picking = { targetId: targetId || null, includeHidden: !!(options && options.includeHidden), tellPanel: !options || options.tellPanel !== false, hovered: null, seq: 0 };
    overlayRoot().setAttribute("data-picking", "");
    const swallow = (e: Event) => {
        if (inBevTools(e.target as Node)) return;
        if (e.cancelable) e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
    };
    for (const type of ["pointerdown", "pointerup", "mousedown", "mouseup", "dblclick", "auxclick", "contextmenu", "touchstart", "submit"]) onPick(type, swallow);
    onPick("mousemove", (e: MouseEvent) => {
        const state = picking;
        const node = e.target as Node;
        if (!state || node === state.hovered) return;
        state.hovered = node;
        if (inBevTools(node)) { clearHighlight(); return; }
        const seq = ++state.seq;
        resolveNode(node, state.includeHidden).then(result => {
            if (picking !== state || seq !== state.seq) return;
            if ("error" in result) outline(null, "No component", result.error);
            else outline(result.container, result.name, result.renderedBy ? "through " + result.renderedBy : null);
        });
    });
    const pickAt = (node: Node | null, x: number, y: number) => {
        const state = picking;
        if (!state) return;
        stopPick("picking");
        resolvingPick = true;
        resolveNode(node, state.includeHidden).then(result => {
            resolvingPick = false;
            if ("error" in result) {
                lastPicked = { error: result.error, at: Date.now() };
                showNote(result.error, x, y);
                tellPickEnded(state, result.error);
                notify({ type: "pick", state: "failed", error: result.error });
                return;
            }
            lastPicked = { targetId: result.targetId, instanceId: result.instanceId, name: result.name, renderedBy: result.renderedBy, element: result.element, at: Date.now() };
            outline(result.container, result.name, result.ambiguous ? "one of several identical roots, matched by position" : result.element);
            highlightTimer = setTimeout(clearHighlight, 1500);
            if (state.tellPanel) {
                // Picked in another runtime than the panel that started: that panel stops picking, and the runtime that
                // rendered it shows its own panel, selected there - one panel at a time, as the runtime switcher does.
                if (state.targetId && state.targetId !== result.targetId) {
                    tellPickEnded(state, null);
                    show(result.targetId);
                }
                const t = targets.get(result.targetId);
                if (t) invoke(t, "OnComponentPicked", result.instanceId);
            }
            notify({ type: "picked", ...lastPicked });
        });
    };
    onPick("click", (e: MouseEvent) => {
        if (!picking) return;
        if (isPickToggle(e.target as Node)) return;
        if (inBevTools(e.target as Node)) { stopPick("cancelled"); return; }
        swallow(e);
        pickAt(e.target as Node, e.clientX, e.clientY);
    });
    onPick("touchend", (e: TouchEvent) => {
        const touch = e.changedTouches && e.changedTouches[0];
        if (!picking || !touch) return;
        const node = document.elementFromPoint(touch.clientX, touch.clientY);
        if (isPickToggle(node)) return;
        if (inBevTools(node)) { stopPick("cancelled"); return; }
        // Cancelling the touch's end cancels the click it would have become: the tap is the pick.
        swallow(e);
        pickAt(node, touch.clientX, touch.clientY);
    });
    onPick("keydown", (e: KeyboardEvent) => {
        if (e.key !== "Escape") return;
        e.preventDefault();
        e.stopPropagation();
        stopPick("cancelled");
    });
    notify({ type: "pick", state: "started" });
    return true;
}

export function stopPick(reason?: string) {
    const state = picking;
    if (!state) return;
    picking = null;
    while (pickListeners.length) pickListeners.pop()!();
    if (overlay) overlay.removeAttribute("data-picking");
    clearHighlight();
    if (reason === "cancelled") {
        notify({ type: "pick", state: "cancelled" });
        tellPickEnded(state, null);
    }
}

/** A panel that started picking shows that it is picking; it hears when that ends without a component. */
function tellPickEnded(state: PickState, error: string | null) {
    const t = state.targetId ? targets.get(state.targetId) : null;
    if (t && state.tellPanel) invoke(t, "OnPickEnded", error);
}

/** True while picking, and while the picked node is being resolved: the outcome is not known until lastPick says. */
export function isPicking() {
    return !!picking || resolvingPick;
}

/** The outcome of the last pick, for a frontend that polls (the extension cannot subscribe across inspectedWindow.eval). */
export function lastPick() {
    return lastPicked;
}

// ----- targets -----

function targetInfo(t: BevTarget): BevTargetInfo {
    return { id: t.id, sessionId: t.sessionId, platform: t.platform, renderMode: t.renderMode, label: t.label, attachedAt: t.attachedAt };
}

/** The runtimes on this page that BevTools runs in. */
export function targets_(): BevTargetInfo[] {
    return Array.from(targets.values()).map(targetInfo);
}

function targetsChanged() {
    const list = targets_();
    targets.forEach(t => invoke(t, "OnTargetsChanged", list));
    notify({ type: "targets", targets: list });
}

export function attach(ref: any, options: BevAttachOptions | null) {
    const first = targets.size === 0;
    if (first) {
        hubGeneration++;
        installPageListeners();
    }

    // The last runtime to attach decides the shortcut and the patterns; every runtime gets them from the same options.
    shortcut = parseShortcut(options ? options.shortcut : undefined);
    sensitivePatterns = (options && options.sensitivePatterns || []).map(p => String(p).toLowerCase());
    const id = "t" + (++nextTargetId);
    const target: BevTarget = {
        id,
        ref,
        sessionId: options && typeof options.sessionId === "number" ? options.sessionId : null,
        moduleObjectId: options && typeof options.moduleObjectId === "number" ? options.moduleObjectId : null,
        platform: options && options.platform || null,
        renderMode: options && options.renderMode || null,
        label: options && options.label || (options && options.platform) || "Blazor",
        attachedAt: Date.now(),
        jsToDotNetBatch: [],
        flushTimer: null,
    };
    targets.set(id, target);

    if (options && options.trackJsToDotNet) wrapDotNet();
    if ((!options || options.trackTraffic !== false) && wrapWebSockets()) {
        observeDom();
        trafficTimer ??= setInterval(reportTraffic, 2000);
    }

    exposeHook();
    targetsChanged();
    return { ...snapshot(), targetId: id };
}

/** Detaches one runtime, or every runtime when no id is given; the page is left as it was once none is left. */
export function detach(targetId?: string) {
    const ids = targetId ? [targetId] : Array.from(targets.keys());
    for (const id of ids) {
        const t = targets.get(id);
        if (!t) continue;
        if (t.flushTimer) clearTimeout(t.flushTimer);
        // Never retain calls across panel/circuit lifetimes: a later attach belongs to a different BevTools session.
        t.jsToDotNetBatch = [];
        targets.delete(id);
    }

    if (targets.size === 0) {
        hubGeneration++;
        removePageListeners();
        unwrapDotNet();
        unwrapWebSockets();
        stopObservingDom();
        if (trafficTimer) { clearInterval(trafficTimer); trafficTimer = null; }
        sockets.clear();
        ownInvocations.clear();
        ownJsCalls.clear();
        pendingRenderBatches.clear();
        acknowledgedEarly.clear();
        shortcut = null;
        stopPick();
        clearHighlight();
        overlay?.remove();
        overlay = null;
        lastPicked = null;
        auditedElements = [];
        childrenKey = null;
        parentKey = null;
        commonKeys = new Set();
        rootsCache = null;
        hoveredRow = null;
        resolvingPick = false;
        ownEventPending = false;
        if (ownEventTimer) { clearTimeout(ownEventTimer); ownEventTimer = null; }
    }

    targetsChanged();
}

/** Shows one runtime's panel and hides the others' (the panel header's runtime switcher). */
export function show(targetId: string) {
    targets.forEach(t => invoke(t, t.id === targetId ? "OnShowRequested" : "OnHideRequested"));
}

export function refresh() {
    return snapshot();
}

/**
 * Focuses an element of one runtime's panel. Moving to the badge on close happens only when focus was in the panel:
 * closing with the shortcut from the app must leave focus in the app.
 */
export function focusIn(targetId: string, selector: string, onlyFromPanel?: boolean) {
    const root = document.querySelector(".bev-root[data-bev-target=\"" + String(targetId).replace(/["\\]/g, "") + "\"]");
    if (!root) return false;
    // Focus is moved within the panel, or back to it when it was lost (the focused element was re-rendered away); never
    // taken from the app.
    if ((selector === ".bev-badge" || onlyFromPanel) && document.activeElement && document.activeElement !== document.body && !root.contains(document.activeElement)) return false;
    const element = root.querySelector(selector) as HTMLElement | null;
    if (!element || typeof element.focus !== "function") return false;
    element.focus();
    return true;
}

// ----- the protocol for external frontends -----

/** How long a frontend waits for a runtime's answer; one that never comes would otherwise hold the frontend forever. */
const REQUEST_TIMEOUT_MS = 30_000;

/** Sends one protocol request to a runtime; it answers in .NET (BevToolsProtocol) with JSON. */
export async function request(targetId: string, method: string, params?: any): Promise<any> {
    const t = targets.get(targetId);
    if (!t) throw new Error("No BevTools runtime " + targetId + " on this page.");
    if (unreachable(t)) throw new Error("The circuit of " + t.label + " is disconnected: it cannot answer until it reconnects.");
    let timer: ReturnType<typeof setTimeout> | undefined;
    const timeout = new Promise<never>((_, reject) => {
        timer = setTimeout(() => reject(new Error(t.label + " did not answer within " + REQUEST_TIMEOUT_MS / 1000 + " s.")), REQUEST_TIMEOUT_MS);
    });
    try {
        const json = await Promise.race([t.ref.invokeMethodAsync("Protocol", method, params === undefined ? null : JSON.stringify(params)), timeout]);
        return typeof json === "string" ? JSON.parse(json) : json;
    } finally {
        clearTimeout(timer);
    }
}

export function subscribe(listener: (event: { type: string; [key: string]: any }) => void) {
    subscribers.add(listener);
    return () => subscribers.delete(listener);
}

/** The companion window: the same protocol in a window of its own, which can show every runtime of the page at once. */
export function openCompanion(url?: string) {
    try {
        const href = url || new URL("_content/Bit.BevTools/companion.html", document.baseURI).href;
        const w = window.open(href, "bit-bevtools-companion", "popup,width=1280,height=820");
        return !!w;
    } catch {
        return false;
    }
}

/**
 * The one way in for a frontend outside the panel: the companion window reads it from its opener, the browser
 * extension through the DevTools' own inspectedWindow.eval, which runs only while DevTools is open on the tab. No
 * message channel is opened on the page: anything that can reach this hook could already run script in the page.
 */
function exposeHook() {
    const w = window as any;
    if (w.__BIT_BEVTOOLS__ && w.__BIT_BEVTOOLS__.protocol === 1) return;
    Object.defineProperty(w, "__BIT_BEVTOOLS__", {
        configurable: true,
        value: Object.freeze({
            protocol: 1, targets: targets_, request, subscribe, traffic, show,
            // Components in the page (added within protocol 1: a frontend checks for them before offering picking).
            startPick, stopPick, isPicking, lastPick, highlightComponent, clearHighlight, componentAt,
        }),
    });
}

// ----- accessibility of the page -----
//
// A small set of checks that an automated tool can make without false alarms, each tied to a WCAG success criterion:
// what they find is a fact about the markup. What they cannot judge - contrast against images, focus order, whether a
// name makes sense, how a screen reader announces it - is left to a person, and the panel says so. Each finding names
// the component that rendered the element, through the same path mapping as picking. BevTools' own UI is checked only
// when asked (the self-check its tests run).

export interface BevA11yFinding {
    rule: string;
    wcag: string;
    message: string;
    element: string;
    targetId?: string;
    instanceId?: number;
    component?: string;
    /** Why the component could not be found, when it is not that Blazor did not render the element. */
    ownerUnknown?: string;
}

let auditedElements: Element[] = [];

/** The lookups that do say the element is not a component's: markup Blazor did not render, or rendered statically. */
const STATIC_MARKUP = /^(Blazor did not render this part|This part of the page is not inside an interactive component)/;

function describeElement(element: Element): string {
    const id = element.id ? "#" + element.id : "";
    const cls = !id && typeof element.className === "string" && element.className.trim() ? "." + element.className.trim().split(/\s+/)[0] : "";
    return "<" + element.tagName.toLowerCase() + id + cls + ">";
}

function isHidden(element: Element): boolean {
    for (let e: Element | null = element; e; e = e.parentElement) {
        if (e.getAttribute("aria-hidden") === "true" || isStyleHidden(e)) return true;
    }
    return false;
}

/** Not rendered, or not shown: hidden, display:none, visibility:hidden. */
function isStyleHidden(element: Element): boolean {
    if ((element as HTMLElement).hidden) return true;
    if (typeof getComputedStyle !== "function") return false;
    const style = getComputedStyle(element);
    return style.display === "none" || style.visibility === "hidden";
}

/** Whether a keyboard can reach the element: not tabindex=-1, shown, and not inside an inert part of the page. */
function isKeyboardReachable(element: Element): boolean {
    if (element.getAttribute("tabindex") === "-1" || element.closest("[inert]")) return false;
    for (let e: Element | null = element; e; e = e.parentElement) {
        if (isStyleHidden(e)) return false;
    }
    return true;
}

/**
 * The text a screen reader reads for an element's content: without aria-hidden or hidden parts, and through shadow
 * roots (a custom element's button text), with an image's alternative in its place.
 */
function readableText(node: Node, depth = 0): string {
    if (depth > 50) return "";
    if (node.nodeType === 3) return node.textContent || "";
    if (node.nodeType !== 1) return "";
    const element = node as Element;
    if (element.getAttribute("aria-hidden") === "true" || isStyleHidden(element)) return "";
    if (depth > 0) {
        const label = (element.getAttribute("aria-label") || "").trim();
        if (label) return " " + label + " ";
        if (element.tagName === "IMG") return " " + (element.getAttribute("alt") || "") + " ";
    }
    const children = element.shadowRoot ? element.shadowRoot.childNodes : element.childNodes;
    let text = "";
    for (const child of Array.from(children)) text += readableText(child, depth + 1);
    return text;
}

function textOf(ids: string | null): string {
    if (!ids) return "";
    // A referenced element's own name: its aria-label when it has one, else what it reads.
    return ids.split(/\s+/).map(id => {
        const target = document.getElementById(id);
        return target ? ((target.getAttribute("aria-label") || "").trim() || readableText(target)) : "";
    }).join(" ").trim();
}

/** The accessible name of a control, as far as markup decides it: enough to tell "none" from "some". */
function accessibleName(element: Element): string {
    const labelledBy = textOf(element.getAttribute("aria-labelledby"));
    if (labelledBy) return labelledBy;
    const label = (element.getAttribute("aria-label") || "").trim();
    if (label) return label;
    const tag = element.tagName.toLowerCase();
    if (tag === "input") {
        const type = (element.getAttribute("type") || "text").toLowerCase();
        if (type === "image") return (element.getAttribute("alt") || "").trim();
        if (type === "submit" || type === "reset" || type === "button") return ((element as HTMLInputElement).value || (type === "button" ? "" : type)).trim();
    }
    if (tag === "img") return (element.getAttribute("alt") || "").trim();
    if (tag === "input" || tag === "select" || tag === "textarea") {
        const id = element.id;
        const forLabel = id ? Array.from(document.querySelectorAll("label")).find(l => l.htmlFor === id) : null;
        const wrapping = element.closest("label");
        const text = ((forLabel && readableText(forLabel)) || (wrapping && readableText(wrapping)) || "").trim();
        if (text) return text;
    } else {
        // What is read, not what is in the markup: an aria-hidden glyph is no name.
        const text = readableText(element).trim();
        if (text) return text;
    }
    return (element.getAttribute("title") || "").trim();
}

const FOCUSABLE = "a[href], button:not([disabled]), input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex='-1']), [contenteditable=true]";

function check(includeBevTools: boolean): { element: Element | null; rule: string; wcag: string; message: string }[] {
    const found: { element: Element | null; rule: string; wcag: string; message: string }[] = [];
    const add = (element: Element | null, rule: string, wcag: string, message: string) => {
        if (element && !includeBevTools && inBevTools(element)) return;
        if (found.length < 200) found.push({ element, rule, wcag, message });
    };
    if (!document.documentElement.getAttribute("lang")) {
        add(null, "page-lang", "3.1.1", "The page does not declare its language (<html lang>): screen readers guess how to pronounce it.");
    }
    for (const img of Array.from(document.querySelectorAll("img:not([alt])"))) {
        const role = img.getAttribute("role");
        // Named another way (aria-label, aria-labelledby): a text alternative all the same.
        if (role === "presentation" || role === "none" || isHidden(img) || textOf(img.getAttribute("aria-labelledby")) || (img.getAttribute("aria-label") || "").trim()) continue;
        add(img, "image-alt", "1.1.1", "An image without an alt attribute: add a text alternative, or alt=\"\" when it is decorative.");
    }
    for (const control of Array.from(document.querySelectorAll("button, a[href], [role=button], [role=link], input[type=button], input[type=submit], input[type=reset], input[type=image]"))) {
        if (isHidden(control) || accessibleName(control)) continue;
        add(control, "control-name", "4.1.2", "A button or link with no accessible name: a screen reader announces it as \"button\" or reads its URL.");
    }
    for (const field of Array.from(document.querySelectorAll("input, select, textarea"))) {
        const type = (field.getAttribute("type") || "text").toLowerCase();
        if (field.tagName === "INPUT" && ["hidden", "button", "submit", "reset", "image"].includes(type)) continue;
        if (isHidden(field) || accessibleName(field)) continue;
        add(field, "field-label", "1.3.1, 4.1.2", "A form field with no label" + (field.getAttribute("placeholder") ? " (a placeholder is not one: it disappears as soon as something is typed)" : "") + ".");
    }
    for (const hidden of Array.from(document.querySelectorAll("[aria-hidden=true]"))) {
        // Only what a keyboard reaches: tabindex=-1 is the usual remedy, and a hidden or inert part is reached by nobody.
        const candidates = hidden.matches(FOCUSABLE) ? [hidden] : Array.from(hidden.querySelectorAll(FOCUSABLE));
        const focusable = candidates.find(isKeyboardReachable);
        if (focusable) add(focusable, "hidden-focusable", "4.1.2", "Focusable inside aria-hidden=\"true\": keyboard users reach something screen readers are told is not there.");
    }
    for (const positive of Array.from(document.querySelectorAll("[tabindex]"))) {
        if (Number(positive.getAttribute("tabindex")) > 0) add(positive, "tabindex-positive", "2.4.3", "tabindex greater than 0 moves this ahead of the page's natural order for every keyboard user.");
    }
    const ids = new Map<string, number>();
    for (const element of Array.from(document.querySelectorAll("[id]"))) ids.set(element.id, (ids.get(element.id) || 0) + 1);
    for (const referrer of Array.from(document.querySelectorAll("label[for], [aria-labelledby], [aria-describedby], [aria-controls]"))) {
        const refs = [referrer.getAttribute("for"), referrer.getAttribute("aria-labelledby"), referrer.getAttribute("aria-describedby"), referrer.getAttribute("aria-controls")]
            .filter(Boolean).join(" ").split(/\s+/).filter(Boolean);
        const duplicated = refs.find(id => (ids.get(id) || 0) > 1);
        if (duplicated) add(referrer, "duplicate-id", "4.1.2", "It refers to id \"" + duplicated + "\", which " + ids.get(duplicated) + " elements carry: the reference reaches the first one only.");
    }
    for (const heading of Array.from(document.querySelectorAll("h1, h2, h3, h4, h5, h6"))) {
        if (!isHidden(heading) && !accessibleName(heading)) add(heading, "heading-empty", "2.4.6", "An empty heading: screen-reader users navigating by headings land on nothing.");
    }
    return found;
}

/**
 * Checks the page and names, for each finding, the component that rendered the element (when Blazor did). Elements are
 * kept for highlightFinding until the next check.
 */
export async function auditPage(options?: { includeBevTools?: boolean } | null): Promise<BevA11yFinding[]> {
    const found = check(!!(options && options.includeBevTools));
    auditedElements = found.map(f => f.element as Element);
    const owners = learnKeys()
        ? await Promise.all(found.map(f => f.element ? resolveNode(f.element, false).catch(() => ({ error: "" })) : Promise.resolve({ error: "" })))
        : found.map(() => ({ error: "" }));
    return found.map((f, i) => {
        const owner = owners[i] as any;
        const result: BevA11yFinding = { rule: f.rule, wcag: f.wcag, message: f.message, element: f.element ? describeElement(f.element) : "<html>" };
        if (owner && !owner.error) {
            result.targetId = owner.targetId;
            result.instanceId = owner.instanceId;
            result.component = owner.name;
        } else if (f.element && owner && owner.error && !STATIC_MARKUP.test(owner.error)) {
            // Not "static markup": the lookup itself did not succeed (no tree yet, a stale page, a runtime that did not answer).
            result.ownerUnknown = owner.error;
        }
        return result;
    });
}

/** Outlines the element of a finding from the last check, scrolled into view. */
export function highlightFinding(index: number, label: string) {
    const element = auditedElements[index];
    clearHighlight();
    if (!element || !element.isConnected) return false;
    if (typeof element.scrollIntoView === "function") element.scrollIntoView({ block: "nearest", inline: "nearest" });
    outline(element, label);
    highlightTimer = setTimeout(clearHighlight, 2500);
    return true;
}

// ----- symbols -----

/**
 * The PDB of one assembly, for a runtime that has no files to read it from (WebAssembly): the one the runtime's own
 * resource list names for this build (fingerprinted names on .NET 9+, plain ones on .NET 8), so it matches the code
 * that runs. Null when the app serves none (a Release build, or one published without symbols).
 */
/**
 * The PDB the runtime's resource list names for this build, fetched with the integrity hash the list gives (so a PDB
 * of an earlier build, which a cache or an unfingerprinted name can serve, is refused) and past the HTTP cache. Null
 * when the app lists no PDB for the assembly; a failed download throws, so it is not taken for "no symbols".
 */
export async function fetchPdb(assemblyName: string): Promise<Uint8Array | null> {
    const runtime = (window as any).Blazor && (window as any).Blazor.runtime;
    const pdb = runtime && typeof runtime.getConfig === "function" ? runtime.getConfig()?.resources?.pdb : null;
    const file = assemblyName + ".pdb";
    let url: string | null = null;
    let integrity: string | undefined;
    if (Array.isArray(pdb)) {
        const entry = pdb.find((e: any) => e && (e.virtualPath === file || e.name === file));
        url = entry ? entry.resolvedUrl || "_framework/" + entry.name : null;
        integrity = entry && typeof entry.integrity === "string" ? entry.integrity : undefined;
    } else if (pdb && typeof pdb === "object" && Object.prototype.hasOwnProperty.call(pdb, file)) {
        url = "_framework/" + file;
        integrity = typeof pdb[file] === "string" && pdb[file].startsWith("sha") ? pdb[file] : undefined;
    }
    if (!url) return null;
    const response = await fetch(new URL(url, document.baseURI).href, { integrity, cache: "no-cache" });
    if (!response.ok) throw new Error("The PDB for " + assemblyName + " could not be downloaded: HTTP " + response.status);
    return new Uint8Array(await response.arrayBuffer());
}

// ----- storage, preferences, downloads -----

export function getStorage(kind: string, patterns: string[] | null, previews?: boolean) {
    const storage = kind === "session" ? window.sessionStorage : window.localStorage;
    const lowered = (patterns || []).map(p => String(p).toLowerCase());
    const result: BevStorageEntry[] = [];
    try {
        for (let i = 0; i < storage.length; i++) {
            const key = storage.key(i);
            const value = storage.getItem(key!) || "";
            const lowerKey = (key || "").toLowerCase();
            // Redaction happens before the value leaves the browser: BevTools never holds the plaintext. Without
            // previews (ValueAccess None) no value leaves it at all.
            const sensitive = lowered.some(p => lowerKey.includes(p)) || looksLikeSecret(value);
            result.push({
                key,
                size: value.length,
                preview: previews === false ? "«hidden»" : sensitive ? REDACTED : (value.length > 120 ? value.slice(0, 120) + "…" : value),
                redacted: sensitive || previews === false,
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

export { targets_ as targets };
