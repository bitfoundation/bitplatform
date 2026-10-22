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
    renderBatches: { count: number; bytes: number; maxBytes: number; applyMs: number; maxApplyMs: number; pending: number };
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

function invoke(target: BevTarget, method: string, ...args: any[]) {
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
// SignalR invocation ids of the reports BevTools makes over a circuit, so their completions count as its own too.
const ownInvocations = new Set<string>();
const pendingRenderBatches = new Map<string, number>();

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
            renderBatches: { count: 0, bytes: 0, maxBytes: 0, applyMs: 0, maxApplyMs: 0, pending: 0 },
            largest: [],
        };
        sockets.set(socket, s);
        const stats = s;
        socket.addEventListener("message", (e: MessageEvent) => onFrame(stats, "←", e.data));
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
            if (args && isOwnObjectId(+args[4])) {
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
        }
        noteMessage(s, direction, kind, record.length);
    }
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
                r.skip(); // method
                const objectId = r.int();
                if (objectId !== null && isOwnObjectId(objectId)) {
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
            } else if (target === "JS.RenderBatch" && argCount >= 2 && direction === "←") {
                const batchId = r.int();
                const batchBytes = r.binLength() ?? size;
                const rb = s.renderBatches;
                rb.count++;
                rb.bytes += batchBytes;
                if (batchBytes > rb.maxBytes) rb.maxBytes = batchBytes;
                if (batchId !== null) pendingRenderBatches.set(s.id + ":" + batchId, performance.now());
                if (pendingRenderBatches.size > 256) pendingRenderBatches.delete(pendingRenderBatches.keys().next().value!);
                rb.pending = pendingRenderBatches.size;
            } else if (target === "OnRenderCompleted" && argCount >= 1 && direction === "→") {
                const batchId = r.int();
                const key = s.id + ":" + batchId;
                const received = pendingRenderBatches.get(key);
                if (received !== undefined) {
                    // From the batch arriving to the browser saying it applied it: the DOM work, on the browser's side.
                    pendingRenderBatches.delete(key);
                    const ms = performance.now() - received;
                    s.renderBatches.applyMs += ms;
                    if (ms > s.renderBatches.maxApplyMs) s.renderBatches.maxApplyMs = ms;
                    s.renderBatches.pending = pendingRenderBatches.size;
                }
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
    on(window, "online", () => broadcast("OnBrowserEvent", "online", null));
    on(window, "offline", () => broadcast("OnBrowserEvent", "offline", null));
    on(document, "visibilitychange", () => broadcast("OnBrowserEvent", "visibility", document.visibilityState));
    on(document, "components:reconnect-state-changed", (e: CustomEvent) => {
        const d = e.detail || {};
        broadcast("OnReconnectState", d.state || "unknown", d.currentAttempt || 0, d.secondsToNextAttempt || 0);
    });
    on(document, "keydown", (e: KeyboardEvent) => {
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
        if (trafficTimer) { clearInterval(trafficTimer); trafficTimer = null; }
        sockets.clear();
        ownInvocations.clear();
        pendingRenderBatches.clear();
        shortcut = null;
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

// ----- the protocol for external frontends -----

/** Sends one protocol request to a runtime; it answers in .NET (BevToolsProtocol) with JSON. */
export async function request(targetId: string, method: string, params?: any): Promise<any> {
    const t = targets.get(targetId);
    if (!t) throw new Error("No BevTools runtime " + targetId + " on this page.");
    const json = await t.ref.invokeMethodAsync("Protocol", method, params === undefined ? null : JSON.stringify(params));
    return typeof json === "string" ? JSON.parse(json) : json;
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
        value: Object.freeze({ protocol: 1, targets: targets_, request, subscribe, traffic, show }),
    });
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
