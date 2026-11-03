// Tests for the page hub in bit-bevtools.js: several BevTools runtimes on one page (a Web App's circuit next to its
// WebAssembly runtime), the page-level facts every one of them hears, WebSocket/SignalR traffic decoding with BevTools'
// own reports kept apart, and the protocol external frontends (companion window, browser extension) use.
//
// Like bridge.test.js they run against the built wwwroot/bit-bevtools.js, so build Bit.BevTools first.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, it, expect, vi, afterEach } from 'vitest';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const BUNDLE = path.resolve(HERE, '../../Bit.BevTools/wwwroot/bit-bevtools.js');

async function loadHub() {
    if (!fs.existsSync(BUNDLE)) {
        throw new Error(`Missing ${BUNDLE}.\nBuild the package first: dotnet build ../../Bit.BevTools/Bit.BevTools.csproj`);
    }
    vi.resetModules();
    return import(BUNDLE);
}

class FakeSocket {
    constructor(url) {
        this.url = url;
        this.listeners = {};
        this.sent = [];
    }

    addEventListener(type, handler) {
        (this.listeners[type] ||= []).push(handler);
    }

    removeEventListener(type, handler) {
        this.listeners[type] = (this.listeners[type] || []).filter(h => h !== handler);
    }

    receive(data) {
        for (const handler of this.listeners.message || []) handler({ data });
    }

    close(code, reason = '') {
        for (const handler of this.listeners.close || []) handler({ code, reason });
    }
}
FakeSocket.prototype.send = function (data) { this.sent.push(data); };
const originalSend = FakeSocket.prototype.send;

function eventTarget() {
    const listeners = new Map();
    // The types listened to in the capture phase: the only way to hear an event that does not bubble from below.
    const captured = new Set();
    return {
        listeners,
        captured,
        addEventListener(type, handler, options) {
            if (!listeners.has(type)) listeners.set(type, []);
            listeners.get(type).push(handler);
            if (options === true || options?.capture) captured.add(type);
        },
        removeEventListener(type, handler) {
            const list = listeners.get(type) || [];
            const i = list.indexOf(handler);
            if (i >= 0) list.splice(i, 1);
        },
        dispatch(type, event) {
            for (const handler of (listeners.get(type) || []).slice()) handler(event);
        },
    };
}

function installPage() {
    const win = eventTarget();
    const doc = eventTarget();
    const posted = [];
    globalThis.window = {
        ...win,
        posted,
        WebSocket: FakeSocket,
        localStorage: { length: 1, key: () => 'theme', getItem: () => 'dark' },
        sessionStorage: { length: 0, key: () => null, getItem: () => null },
        DotNet: { invokeMethodAsync: () => Promise.resolve(null), invokeMethod: () => null },
        postMessage(data) {
            posted.push(data);
            win.dispatch('message', { source: globalThis.window, data });
        },
    };
    globalThis.document = { ...doc, visibilityState: 'visible', baseURI: 'https://app.example/', querySelector: () => null };
    globalThis.location = { href: 'https://app.example/products', pathname: '/products', search: '' };
    Object.defineProperty(globalThis, 'navigator', { value: { onLine: true, userAgent: 'node' }, configurable: true });
    FakeSocket.prototype.send = originalSend;
    return { win, doc, posted };
}

function runtime(id, protocolAnswers = {}) {
    const calls = [];
    return {
        _id: id,
        calls,
        invokeMethodAsync(method, ...args) {
            calls.push([method, ...args]);
            if (method === 'Protocol') {
                return Promise.resolve(JSON.stringify(protocolAnswers[args[0]] ?? { error: 'unknown' }));
            }
            return Promise.resolve();
        },
        reported(method) {
            return calls.filter(c => c[0] === method);
        },
    };
}

// A minimal MessagePack encoder for SignalR's binary protocol: each message is a VarInt length and an array.
const str = s => { const b = [...Buffer.from(s, 'utf8')]; return b.length < 32 ? [0xa0 | b.length, ...b] : [0xd9, b.length, ...b]; };
const int = n => (n >= 0 && n < 128 ? [n] : [0xce, (n >>> 24) & 255, (n >>> 16) & 255, (n >>> 8) & 255, n & 255]);
const nil = [0xc0];
const emptyMap = [0x80];
const arr = (...items) => [0x90 | items.length, ...items.flat()];
const bin = n => [0xc5, (n >> 8) & 255, n & 255, ...new Array(n).fill(7)];
const invocation = (target, ...args) => arr(int(1), emptyMap, nil, str(target), arr(...args));
function frame(...messages) {
    const out = [];
    for (const m of messages) {
        let length = m.length;
        do {
            let b = length & 0x7f;
            length >>= 7;
            if (length) b |= 0x80;
            out.push(b);
        } while (length);
        out.push(...m);
    }
    return new Uint8Array(out).buffer;
}

afterEach(() => {
    vi.useRealTimers();
});

describe('the page\'s other storage', () => {
    it('reads cookie names and sizes, never their values, and lists databases, caches, service workers and the quota', async () => {
        installPage();
        const hub = await loadHub();
        document.cookie = 'theme=dark; session_id=s3cr3t-value; flag';
        window.indexedDB = { databases: async () => [{ name: 'orders-cache', version: 3 }] };
        const opened = [];
        window.caches = {
            keys: async () => ['bit-bswup-v2'],
            open: async name => { opened.push(name); return { keys: async () => [{}, {}, {}] }; },
        };
        const worker = (url, state) => ({ scriptURL: url, state });
        Object.defineProperty(globalThis, 'navigator', {
            configurable: true,
            value: {
                onLine: true,
                userAgent: 'node',
                serviceWorker: {
                    controller: worker('https://app.example/service-worker.js?v=1', 'activated'),
                    getRegistrations: async () => [{ scope: 'https://app.example/', active: worker('https://app.example/service-worker.js?v=1', 'activated'), waiting: worker('https://app.example/service-worker.js?v=2', 'installed'), installing: null }],
                },
                storage: { estimate: async () => ({ usage: 2048, quota: 4096, usageDetails: { caches: 1024 } }), persisted: async () => false },
            },
        });

        const storage = await hub.getAppStorage();
        expect(storage.cookies).toEqual([{ name: 'theme', size: 4 }, { name: 'session_id', size: 12 }, { name: 'flag', size: 0 }]);
        expect(JSON.stringify(storage)).not.toContain('s3cr3t');
        expect(storage.indexedDb).toEqual([{ name: 'orders-cache', version: 3 }]);
        expect(storage.caches).toEqual([{ name: 'bit-bswup-v2', entries: 3 }]);
        expect(storage.serviceWorkers[0].waiting).toBe('https://app.example/service-worker.js?v=2');
        expect(storage.controller).toBe('https://app.example/service-worker.js?v=1');
        expect(storage).toMatchObject({ usageBytes: 2048, quotaBytes: 4096, usageDetails: { caches: 1024 }, persisted: false, errors: [] });
    });

    it('redacts a cookie name shaped like a credential, judged before it is cut', async () => {
        installPage();
        const hub = await loadHub();
        document.cookie = 'eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.' + 'c'.repeat(300) + '=1; theme=dark';
        const storage = await hub.getAppStorage();
        expect(storage.cookies).toEqual([{ name: '«redacted»', size: 1 }, { name: 'theme', size: 4 }]);
    });

    it('opens no cache that is gone by the time it is counted, and nothing once the inventory gave up', async () => {
        installPage();
        const hub = await loadHub();
        let names = ['kept', 'deleted'];
        const opened = [];
        window.caches = {
            keys: async () => { const now = names; names = ['kept']; return now; },
            open: async name => { opened.push(name); return { keys: async () => [{}] }; },
        };
        expect((await hub.getAppStorage()).caches).toEqual([{ name: 'kept', entries: 1 }]);
        expect(opened, 'open() would have created the deleted one again').toEqual(['kept']);

        vi.useFakeTimers();
        opened.length = 0;
        window.caches = {
            keys: async () => ['a', 'b'],
            // Answers only after the inventory gave up on Cache Storage.
            has: () => new Promise(resolve => setTimeout(() => resolve(true), 4000)),
            open: async name => { opened.push(name); return { keys: async () => [] }; },
        };
        const pending = hub.getAppStorage();
        await vi.advanceTimersByTimeAsync(3500);
        const storage = await pending;
        expect(storage.caches).toBeNull();
        expect(storage.errors).toContain('Cache Storage: timed out');
        await vi.advanceTimersByTimeAsync(10000);
        expect(opened, 'nothing started after the timeout').toEqual([]);
    });

    it('says what it could not read instead of failing, and has nothing where a browser has no such API', async () => {
        installPage();
        const hub = await loadHub();
        window.indexedDB = { databases: () => Promise.reject(new Error('denied')) };
        delete window.caches;
        const storage = await hub.getAppStorage();
        expect(storage.indexedDb).toBeNull();
        expect(storage.caches).toBeNull();
        expect(storage.serviceWorkers).toBeNull();
        expect(storage.errors).toEqual(['IndexedDB: denied']);
    });
});

describe('webassembly memory', () => {
    it("reads the runtime's linear memory, and nothing where there is no runtime", async () => {
        installPage();
        const hub = await loadHub();
        expect(hub.wasmMemory()).toBeNull();
        globalThis.getDotnetRuntime = () => ({ Module: { HEAPU8: { length: 84934656 } } });
        try {
            expect(hub.wasmMemory()).toBe(84934656);
            globalThis.getDotnetRuntime = () => { throw new Error('not started'); };
            expect(hub.wasmMemory()).toBeNull();
        } finally {
            delete globalThis.getDotnetRuntime;
        }
    });
});

describe('responsiveness', () => {
    function fakeObservers(types) {
        const observers = [];
        const original = globalThis.PerformanceObserver;
        class FakeObserver {
            constructor(callback) { this.callback = callback; this.disconnected = false; observers.push(this); }
            observe(options) { this.options = options; }
            disconnect() { this.disconnected = true; }
            emit(entries) { this.callback({ getEntries: () => entries }); }
        }
        FakeObserver.supportedEntryTypes = types;
        globalThis.PerformanceObserver = FakeObserver;
        return { observers, restore: () => { globalThis.PerformanceObserver = original; } };
    }

    const element = (tagName, id, inBevTools = false) => ({ nodeType: 1, tagName, id, className: '', closest: selector => (inBevTools && selector === '.bev-root' ? {} : null) });

    it('reports long animation frames and interactions with ages, each interaction across its entries, and none of BevTools\' own', async () => {
        installPage();
        const fake = fakeObservers(['long-animation-frame', 'event', 'longtask']);
        try {
            const hub = await loadHub();
            const wasm = runtime(1);
            const attached = hub.attach(wasm, { platform: 'WebAssembly' });
            expect(attached.responsiveness).toEqual({ longAnimationFrame: true, longTask: true, eventTiming: true });
            const frames = fake.observers.find(o => o.options.type === 'long-animation-frame');
            const events = fake.observers.find(o => o.options.type === 'event');
            expect(fake.observers.some(o => o.options.type === 'longtask'), 'long tasks only where there are no long animation frames').toBe(false);
            expect(events.options.durationThreshold).toBe(16);

            const now = performance.now();
            frames.emit([
                { startTime: now - 400, duration: 30, blockingDuration: 0, scripts: [] },
                { startTime: now - 300, duration: 220, blockingDuration: 170, scripts: [
                    { invoker: 'BUTTON#save.onclick', invokerType: 'event-listener', sourceURL: 'https://app.example/_framework/dotnet.native.js?v=1', sourceFunctionName: 'invoke', duration: 40 },
                    { invoker: 'Response.json.then', invokerType: 'resolve-promise', sourceURL: 'https://app.example/app.js', sourceFunctionName: 'load', duration: 150 },
                ] },
            ]);
            events.emit([
                { interactionId: 7, name: 'pointerdown', startTime: now - 250, duration: 40, processingStart: now - 248, processingEnd: now - 240, target: element('BUTTON', 'save') },
                { interactionId: 7, name: 'click', startTime: now - 250, duration: 260, processingStart: now - 245, processingEnd: now - 40, target: element('BUTTON', 'save') },
                { interactionId: 0, name: 'mousemove', startTime: now - 100, duration: 80, processingStart: now - 99, processingEnd: now - 90, target: null },
                { interactionId: 9, name: 'pointerdown', startTime: now - 90, duration: 120, processingStart: now - 89, processingEnd: now - 88, target: element('BUTTON', 'bev-close', true) },
                { interactionId: 9, name: 'click', startTime: now - 90, duration: 120, processingStart: now - 87, processingEnd: now - 10, target: element('SPAN', 'bev-icon') },
            ]);
            hub.flushResponsiveness();

            const [, reportedFrames, reportedInteractions] = wasm.reported('ReportResponsiveness')[0];
            expect(reportedFrames).toHaveLength(1);
            expect(reportedFrames[0].blockingMs).toBe(170);
            expect(reportedFrames[0].ageMs).toBeGreaterThanOrEqual(300);
            expect(reportedFrames[0].scripts.map(s => s.source)).toEqual(['app.js', 'dotnet.native.js']);
            expect(reportedInteractions).toHaveLength(1);
            // pointerdown's handlers took 8 ms, click's 205 ms: one interaction, named after its slowest event, whose
            // processing spans both, from the first input to the paint after the last.
            const click = reportedInteractions[0];
            expect(click.name).toBe('click');
            expect(click.target).toBe('<button#save>');
            expect(click.durationMs).toBeCloseTo(260);
            expect(click.inputDelayMs).toBeCloseTo(2);
            expect(click.processingMs).toBeCloseTo(208);
            expect(click.presentationMs).toBeCloseTo(50);
            expect(click.interactionId, 'what the runtime puts its parts back together by').toBe(7);

            hub.flushResponsiveness();
            expect(wasm.reported('ReportResponsiveness'), 'nothing new, nothing sent').toHaveLength(1);
            hub.detach();
            expect(fake.observers.every(o => o.disconnected)).toBe(true);
        } finally {
            fake.restore();
        }
    });

    it('falls back to long tasks, and observes nothing when asked not to', async () => {
        installPage();
        const fake = fakeObservers(['longtask']);
        try {
            const hub = await loadHub();
            hub.attach(runtime(1), { trackResponsiveness: false });
            expect(fake.observers).toHaveLength(0);
            hub.detach();
            const wasm = runtime(2);
            hub.attach(wasm, {});
            expect(fake.observers.map(o => o.options.type)).toEqual(['longtask']);
            fake.observers[0].emit([{ startTime: performance.now() - 100, duration: 90 }]);
            hub.flushResponsiveness();
            const [, frames] = wasm.reported('ReportResponsiveness')[0];
            expect(frames[0].blockingMs, 'a long task blocks for what it took beyond 50 ms').toBe(40);
            expect(frames[0].scripts).toEqual([]);
            hub.detach();
        } finally {
            fake.restore();
        }
    });

    it('stops observing once the runtimes left attached all opted out, and starts again for one that opts in', async () => {
        installPage();
        const fake = fakeObservers(['longtask']);
        try {
            const hub = await loadHub();
            const { targetId: tracking } = hub.attach(runtime(1), {});
            const { targetId: optedOut } = hub.attach(runtime(2), { trackResponsiveness: false });
            expect(fake.observers).toHaveLength(1);
            hub.detach(tracking);
            expect(fake.observers[0].disconnected, 'only an opted-out runtime is left').toBe(true);

            hub.attach(runtime(3), {});
            expect(fake.observers).toHaveLength(2);
            expect(fake.observers[1].disconnected).toBe(false);
            hub.detach(optedOut);
            expect(fake.observers[1].disconnected, 'a runtime that tracks is still attached').toBe(false);
            hub.detach();
        } finally {
            fake.restore();
        }
    });

    it('keeps the newest interactions with BevTools, and an older one\'s late entry without a target is still its own', async () => {
        installPage();
        const fake = fakeObservers(['event']);
        try {
            const hub = await loadHub();
            const wasm = runtime(1);
            hub.attach(wasm, { platform: 'WebAssembly' });
            const events = fake.observers.find(o => o.options.type === 'event');
            const now = performance.now();
            const entry = (interactionId, target) => ({ interactionId, name: 'click', startTime: now - 100, duration: 80, processingStart: now - 99, processingEnd: now - 30, target });
            // Sixty interactions with the panel and one with the application, reported.
            events.emit([...Array.from({ length: 60 }, (_, i) => entry(i + 1, element('BUTTON', 'bev-' + i, true))), entry(61, element('BUTTON', 'save'))]);
            hub.flushResponsiveness();
            expect(wasm.reported('ReportResponsiveness')[0][2].map(i => i.interactionId)).toEqual([61]);

            // Late entries whose elements left the page: two of the panel's - one still kept, one let go - and the application's.
            events.emit([entry(30, null), entry(5, null), entry(62, null)]);
            hub.flushResponsiveness();
            expect(wasm.reported('ReportResponsiveness')[1][2].map(i => i.interactionId)).toEqual([62]);
            hub.detach();
        } finally {
            fake.restore();
        }
    });
});

describe('several runtimes on one page', () => {
    it('attach as separate targets, all hear page-level facts, and detach one at a time', async () => {
        const { win } = installPage();
        const hub = await loadHub();
        const server = runtime(1);
        const wasm = runtime(2);

        const a = hub.attach(server, { platform: 'Server', sessionId: 3, label: 'Server circuit' });
        const b = hub.attach(wasm, { platform: 'WebAssembly', sessionId: 1 });

        expect(a.targetId).not.toBe(b.targetId);
        expect(window.__BIT_BEVTOOLS__.targets().map(t => t.platform)).toEqual(['Server', 'WebAssembly']);
        expect(server.reported('OnTargetsChanged').at(-1)[1]).toHaveLength(2);

        win.dispatch('unhandledrejection', { reason: new Error('page failure') });
        expect(server.reported('OnJsError')).toHaveLength(1);
        expect(wasm.reported('OnJsError')).toHaveLength(1);

        hub.detach(a.targetId);
        win.dispatch('unhandledrejection', { reason: new Error('later failure') });
        expect(server.reported('OnJsError')).toHaveLength(1);
        expect(wasm.reported('OnJsError')).toHaveLength(2);
        expect(window.__BIT_BEVTOOLS__.targets().map(t => t.id)).toEqual([b.targetId]);
        hub.detach();
    });

    it('routes the panel switcher to the runtime it names', async () => {
        installPage();
        const hub = await loadHub();
        const server = runtime(1);
        const wasm = runtime(2);
        hub.attach(server, {});
        const b = hub.attach(wasm, {});

        hub.show(b.targetId);

        expect(wasm.reported('OnShowRequested')).toHaveLength(1);
        expect(server.reported('OnHideRequested')).toHaveLength(1);
        hub.detach();
    });
});

describe('browser failures', () => {
    it('reports resources that failed to load and CSP violations, with their URLs redacted', async () => {
        const { win, doc } = installPage();
        const hub = await loadHub();
        const ref = runtime(1);
        hub.attach(ref, { sensitivePatterns: ['token'] });

        win.dispatch('error', { target: { tagName: 'SCRIPT', src: 'https://cdn.example/_framework/app.wasm?token=abc123' } });
        doc.dispatch('securitypolicyviolation', { blockedURI: 'https://cdn.example/lib.js?sig=xyz', effectiveDirective: 'script-src', sourceFile: 'https://app.example/', lineNumber: 1, columnNumber: 2 });

        const [resource, csp] = ref.reported('OnJsError').map(c => c[1]);
        expect(resource.type).toBe('ResourceLoadError');
        expect(resource.message).toContain('<script>');
        expect(resource.message).not.toContain('abc123');
        expect(csp.type).toBe('CspViolation');
        expect(csp.message).toContain('script-src');
        expect(csp.message).not.toContain('xyz');
        hub.detach();
    });

    it('sends no more of a script error than BevTools accepts', async () => {
        const { win } = installPage();
        const hub = await loadHub();
        const ref = runtime(1);
        hub.attach(ref, null);

        const error = new Error('m'.repeat(5000));
        error.name = 'N'.repeat(500);
        win.dispatch('error', { error, message: error.message });

        const [report] = ref.reported('OnJsError').map(c => c[1]);
        expect(report.message).toHaveLength(4000);
        expect(report.type).toHaveLength(200);
        expect(report.stack.length).toBeLessThanOrEqual(4000);
        hub.detach();
    });

    it('never cuts a surrogate pair in half nor sends a lone surrogate, which .NET cannot read', async () => {
        const { win } = installPage();
        const hub = await loadHub();
        const ref = runtime(1);
        hub.attach(ref, null);

        const error = new Error('m'.repeat(3999) + '\u{1F600}');
        error.name = 'N'.repeat(199) + '\u{1F600}';
        error.stack = 'at a \uD800 b';
        win.dispatch('error', { error, message: error.message });

        const [report] = ref.reported('OnJsError').map(c => c[1]);
        expect(report.message).toBe('m'.repeat(3999));
        expect(report.type).toBe('N'.repeat(199));
        expect(report.stack).toBe('at a \uFFFD b');
        expect(JSON.stringify(report)).not.toMatch(/\\ud[89ab]/i);
        hub.detach();
    });
});

describe('reconnects', () => {
    // Blazor's own name, dispatched on the app's reconnect dialog without bubbling. The hub once listened for
    // "components:reconnect-state-changed", which no Blazor version dispatches, so no reconnect was ever reported.
    it('hears Blazor\'s reconnect state on the dialog, in the capture phase', async () => {
        const { doc } = installPage();
        const hub = await loadHub();
        const ref = runtime(1);
        hub.attach(ref, null);

        expect(doc.captured.has('components-reconnect-state-changed')).toBe(true);
        doc.dispatch('components-reconnect-state-changed', { detail: { state: 'retrying', currentAttempt: 2, secondsToNextAttempt: 5 } });

        expect(ref.reported('OnReconnectState')).toEqual([['OnReconnectState', 'retrying', 2, 5, expect.any(Number)]]);
        hub.detach();
    });

    // "show" and "retrying" happen while the connection is down, when a circuit cannot be called: they once never
    // arrived, so the panel showed only "hide" and counted no reconnect.
    it('tells a circuit the states of its dropped connection once it is back, in order and with their age', async () => {
        installPage();
        const hub = await loadHub();
        const ref = runtime(1);
        hub.attach(ref, { platform: 'Server' });
        const first = new globalThis.window.WebSocket('wss://app.example/_blazor?id=abc');
        first.send(frame(arr(int(6))));
        first.close(1006, '');

        globalThis.document.dispatch('components-reconnect-state-changed', { detail: { state: 'show' } });
        globalThis.document.dispatch('components-reconnect-state-changed', { detail: { state: 'retrying', currentAttempt: 1 } });
        expect(ref.reported('OnReconnectState')).toEqual([]);

        new globalThis.window.WebSocket('wss://app.example/_blazor?id=abc');
        await new Promise(r => setTimeout(r, 20));
        globalThis.document.dispatch('components-reconnect-state-changed', { detail: { state: 'hide' } });

        const states = ref.reported('OnReconnectState');
        expect(states.map(c => c[1])).toEqual(['show', 'retrying', 'hide']);
        expect(states[0][4]).toBeGreaterThanOrEqual(states[2][4]);
        expect(states[0][4]).toBeGreaterThanOrEqual(20);
        hub.detach();
    });

    // Found in a real browser: SignalR's close handler (onclose, set when the socket opened) can run before the hub's
    // listener on a socket that existed before BevTools attached, and Blazor raises "show" from it - while the hub
    // still counts the socket open. The state itself says the connection is down.
    it('holds "show" for a circuit even before the hub hears its socket close', async () => {
        installPage();
        const hub = await loadHub();
        const server = runtime(1);
        const wasm = runtime(2);
        hub.attach(server, { platform: 'Server' });
        hub.attach(wasm, { platform: 'WebAssembly' });
        const socket = new globalThis.window.WebSocket('wss://app.example/_blazor?id=abc');
        socket.send(frame(arr(int(6))));

        globalThis.document.dispatch('components-reconnect-state-changed', { detail: { state: 'show' } });
        expect(server.reported('OnReconnectState'), 'not into a connection that is down').toEqual([]);
        expect(wasm.reported('OnReconnectState').map(c => c[1]), 'a WebAssembly runtime hears it at once').toEqual(['show']);

        globalThis.document.dispatch('components-reconnect-state-changed', { detail: { state: 'hide' } });
        expect(server.reported('OnReconnectState').map(c => c[1])).toEqual(['show', 'hide']);
        hub.detach();
    });
});

describe('WebSocket traffic', () => {
    it('counts circuit messages by kind, sizes and times render batches, and keeps BevTools reports apart', async () => {
        vi.useFakeTimers();
        installPage();
        const hub = await loadHub();
        const ref = runtime(5);
        hub.attach(ref, { sensitivePatterns: ['token'] });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=connection-token');

        socket.send(frame(invocation('BeginInvokeDotNetFromJS', str('1'), nil, str('ReportTraffic'), int(5), str('[]'))));
        socket.send(frame(invocation('BeginInvokeDotNetFromJS', str('2'), nil, str('OnTick'), int(9), str('[]'))));
        socket.receive(frame(invocation('JS.RenderBatch', int(3), bin(1000))));
        socket.send(frame(invocation('OnRenderCompleted', int(3), nil)));
        socket.receive(frame(invocation('JS.EndInvokeDotNet', str('1'), [0xc3], str('null')), arr(int(6))));

        const [stats] = hub.traffic();
        expect(stats.kind).toBe('blazor-circuit');
        expect(stats.protocol).toBe('blazorpack');
        expect(stats.url).not.toContain('connection-token');
        expect(stats.own.count, 'the report and its completion').toBe(2);
        expect(stats.messages['→ BeginInvokeDotNetFromJS'].count, "the application's call").toBe(1);
        expect(stats.messages['← JS.RenderBatch'].count).toBe(1);
        expect(stats.messages['← ping'].count).toBe(1);
        expect(stats.renderBatches.count).toBe(1);
        expect(stats.renderBatches.bytes).toBe(1000);
        expect(stats.renderBatches.pending, 'acknowledged').toBe(0);
        expect(stats.sent.count).toBe(3);
        expect(stats.received.count).toBe(2);

        vi.advanceTimersByTime(2100);
        const reports = ref.reported('ReportTraffic');
        expect(reports).toHaveLength(1);
        expect(reports[0][1][0].renderBatches.bytes).toBe(1000);

        vi.advanceTimersByTime(2100);
        expect(ref.reported('ReportTraffic'), 'nothing new: no report').toHaveLength(1);
        hub.detach();
    });

    it("counts the circuit's calls into BevTools' own module, and their replies, as BevTools' own", async () => {
        // Found in a real browser: an export handed to the browser for download is a JS.BeginInvokeJS of hundreds of KB.
        installPage();
        const hub = await loadHub();
        const ref = runtime(5);
        hub.attach(ref, { platform: 'Server', moduleObjectId: 3 });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');

        // [asyncHandle, identifier, argsJson, resultType, targetInstanceId]
        socket.receive(frame(invocation('JS.BeginInvokeJS', int(41), str('downloadText'), str('["trace.json","{}"]'), int(0), int(3))));
        socket.send(frame(invocation('EndInvokeJSFromDotNet', int(41), [0xc3], str('[41,true,true]'))));
        socket.receive(frame(invocation('JS.BeginInvokeJS', int(42), str('import'), str('["./_content/Bit.BevTools/bit-bevtools.js"]'), int(1), int(0))));
        socket.receive(frame(invocation('JS.BeginInvokeJS', int(43), str('app.highlight'), str('[]'), int(0), int(9))));
        socket.send(frame(invocation('EndInvokeJSFromDotNet', int(43), [0xc3], str('[43,true,null]'))));

        const [stats] = hub.traffic();
        expect(stats.own.count, 'the download call, its reply and the module import').toBe(3);
        expect(stats.messages['← JS.BeginInvokeJS'].count, "the app's own call").toBe(1);
        expect(stats.messages['→ EndInvokeJSFromDotNet'].count, "the reply to the app's call").toBe(1);
        hub.detach();
    });

    it('reads the JSON hub protocol envelope without keeping arguments', async () => {
        installPage();
        const hub = await loadHub();
        hub.attach(runtime(1), {});
        const socket = new window.WebSocket('wss://app.example/chathub?id=abc');

        socket.send('{"protocol":"json","version":1}\x1e');
        socket.receive('{"type":1,"target":"ReceiveMessage","arguments":["ada","secret message"]}\x1e{"type":6}\x1e');

        const [stats] = hub.traffic();
        expect(stats.kind).toBe('signalr');
        expect(stats.protocol).toBe('json');
        expect(stats.messages['← ReceiveMessage'].count).toBe(1);
        expect(stats.messages['← ping'].count).toBe(1);
        expect(JSON.stringify(stats)).not.toContain('secret message');
        hub.detach();
    });

    it('sees sockets that existed before it attached from their first send, and restores WebSocket on detach', async () => {
        installPage();
        const existing = new FakeSocket('wss://app.example/_blazor?id=x');
        const hub = await loadHub();
        hub.attach(runtime(1), {});

        existing.send(frame(arr(int(6))));
        existing.close(1006, 'gone');

        const [stats] = hub.traffic();
        expect(stats.openedAt, 'opened before BevTools looked').toBeNull();
        expect(stats.closeCode).toBe(1006);
        hub.detach();
        expect(window.WebSocket).toBe(FakeSocket);
        expect(FakeSocket.prototype.send).toBe(originalSend);
        expect(hub.traffic()).toHaveLength(0);
    });

    it('stops listening to a socket it no longer tracks: on detach, and when the bound evicts it', async () => {
        installPage();
        const hub = await loadHub();
        hub.attach(runtime(1), {});
        const first = new window.WebSocket('wss://app.example/_blazor?id=first');
        expect(first.listeners.message).toHaveLength(1);

        const later = Array.from({ length: 50 }, (_, i) => new window.WebSocket('wss://app.example/hub?id=' + i));

        expect(hub.traffic()).toHaveLength(50);
        expect(first.listeners.message, 'evicted by the bound').toHaveLength(0);
        expect(first.listeners.close).toHaveLength(0);
        hub.detach();
        expect(later.every(s => s.listeners.message.length === 0 && s.listeners.close.length === 0), 'a socket outliving the session is left as it was').toBe(true);
    });
});

describe('external frontends', () => {
    it('reach a runtime through the hook, and the page gets no message channel', async () => {
        const { win, posted } = installPage();
        const hub = await loadHub();
        const ref = runtime(1, { hello: { protocol: 1, sessionId: 7 } });
        const { targetId } = hub.attach(ref, {});
        const events = [];
        window.__BIT_BEVTOOLS__.subscribe(e => events.push(e.type));

        expect(await window.__BIT_BEVTOOLS__.request(targetId, 'hello')).toEqual({ protocol: 1, sessionId: 7 });
        expect(ref.reported('Protocol')[0]).toEqual(['Protocol', 'hello', null]);
        await expect(window.__BIT_BEVTOOLS__.request('nope', 'hello')).rejects.toThrow('No BevTools runtime');
        expect(win.listeners.get('message') ?? [], 'nothing listens for page messages').toHaveLength(0);
        expect(posted).toHaveLength(0);
        expect(Object.isFrozen(window.__BIT_BEVTOOLS__)).toBe(true);

        hub.attach(runtime(2), {});
        expect(events).toContain('targets');
        hub.detach();
    });

    it("call nothing in a Server runtime while its circuit's connection is closed, and are told so at once", async () => {
        vi.useFakeTimers();
        installPage();
        const hub = await loadHub();
        const server = runtime(1, { hello: { protocol: 1 } });
        const wasm = runtime(2, { hello: { protocol: 1 } });
        const { targetId } = hub.attach(server, { platform: 'Server', label: 'Server circuit' });
        const { targetId: wasmId } = hub.attach(wasm, { platform: 'WebAssembly' });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');
        socket.send(frame(arr(int(6))));
        socket.close(1006, '');
        vi.advanceTimersByTime(2100);

        // Blazor's send would reject where nothing catches it: an unhandled rejection on the application's page.
        expect(server.reported('ReportTraffic'), 'no call into the closed circuit').toHaveLength(0);
        expect(wasm.reported('ReportTraffic'), 'the WebAssembly runtime still hears it').toHaveLength(1);
        await expect(window.__BIT_BEVTOOLS__.request(targetId, 'hello')).rejects.toThrow('is disconnected');
        expect(await window.__BIT_BEVTOOLS__.request(wasmId, 'hello')).toEqual({ protocol: 1 });

        new window.WebSocket('wss://app.example/_blazor?id=y');
        expect(await window.__BIT_BEVTOOLS__.request(targetId, 'hello'), 'reconnected').toEqual({ protocol: 1 });
        hub.detach();
    });

    it('give up on a runtime that never answers instead of waiting forever', async () => {
        vi.useFakeTimers();
        installPage();
        const hub = await loadHub();
        const { targetId } = hub.attach({ invokeMethodAsync: () => new Promise(() => { }) }, { label: 'Silent runtime' });
        const answer = window.__BIT_BEVTOOLS__.request(targetId, 'hello');
        const settled = expect(answer).rejects.toThrow('Silent runtime did not answer within 30 s');
        vi.advanceTimersByTime(30_000);
        await settled;
        hub.detach();
    });
});

describe('redaction in the hub', () => {
    it('redacts credentials, sensitive and token-shaped query values, and SignalR connection ids', async () => {
        installPage();
        const hub = await loadHub();
        hub.attach(runtime(1), { sensitivePatterns: ['token'] });

        const url = hub.redactUrl('https://user:pass@app.example/_blazor?id=abc&page=2&x=eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln#access_token=t');

        expect(url).toBe('https://«redacted»@app.example/_blazor?id=«redacted»&page=2&x=«redacted»#access_token=«redacted»');
        hub.detach();
    });

    it('takes a token right after an escape for one, and reads a long run of base64url text in linear time', async () => {
        installPage();
        const hub = await loadHub();
        hub.attach(runtime(1), null);
        const run = 'ey'.repeat(50_000);

        const escaped = hub.redactUrl('https://app.example/?note=line%5Cn' + 'eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.c2ln');
        const start = performance.now();
        const long = hub.redactUrl('https://app.example/?q=' + run);
        const elapsed = performance.now() - start;

        expect(escaped).toBe('https://app.example/?note=«redacted»');
        expect(long).toBe('https://app.example/?q=' + run);
        expect(elapsed).toBeLessThan(1000);
        hub.detach();
    });

    it('leaves dotted text with "ey" inside a word alone', async () => {
        installPage();
        const hub = await loadHub();
        hub.attach(runtime(1), null);

        for (const value of ['survey_results.annual_report.pdf', 'monkey-business.example-site.com', 'Bit.Demo.Keyboard_Shortcuts.Handlers.Register']) {
            expect(hub.redactUrl('https://app.example/?v=' + value)).toBe('https://app.example/?v=' + value);
        }
        hub.detach();
    });

    it('sends no storage value at all when previews are off', async () => {
        installPage();
        const hub = await loadHub();

        const [entry] = hub.getStorage('local', [], false);

        expect(entry).toMatchObject({ key: 'theme', size: 4, preview: '«hidden»', redacted: true });
    });
});
