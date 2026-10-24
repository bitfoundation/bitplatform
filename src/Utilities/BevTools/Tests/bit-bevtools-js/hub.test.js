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
    return {
        listeners,
        addEventListener(type, handler) {
            if (!listeners.has(type)) listeners.set(type, []);
            listeners.get(type).push(handler);
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

    it('sends no storage value at all when previews are off', async () => {
        installPage();
        const hub = await loadHub();

        const [entry] = hub.getStorage('local', [], false);

        expect(entry).toMatchObject({ key: 'theme', size: 4, preview: '«hidden»', redacted: true });
    });
});
