// Test harness for the Bswup browser bundles.
//
// Bmotion's JS tests can `import` the functions they exercise because bit-bmotion.js is an ES
// module. Bswup's bundles cannot be imported: bit-bswup.js and bit-bswup.progress.js are classic
// scripts that wire up listeners the moment they are parsed, and bit-bswup.sw.js is a service
// worker that reads globals (`self.assetsManifest`, `caches`, `fetch`) at module-evaluation
// time. So instead of importing, we build a fake browser/worker global object, run the real
// shipped bundle inside a `vm` context, and drive it through the same entry points a browser
// would - the captured `install`/`fetch`/`message` listeners, or `window.BitBswup.*`.
//
// Two consequences worth knowing before adding tests:
//   1. Tests run against wwwroot/*.js, i.e. the built output, not the TypeScript. Run a
//      `dotnet build` of Bit.Bswup first (CI does). This is deliberate: it is the artifact that
//      ships in the NuGet package, so it also catches build-step regressions - for example the
//      minifier re-introducing syntax that tsc had downleveled.
//   2. `instanceof` is realm-scoped, and the worker branches on exactly that - `p instanceof
//      RegExp` in prepareRegExpArray, `value instanceof Array` in prepareRegExpArray /
//      prepareExternalAssetsArray. A RegExp or Array built out here fails those checks inside
//      the sandbox, and the failure is SILENT: a host-realm array is treated as a single
//      unrecognized entry and dropped, so the config just appears to do nothing. Always build
//      config values with the context's own `regex()` / `array()` helpers, or assign them via
//      `configure()`, which marshals arrays for you. (`Array.isArray`, which the worker uses for
//      the manifest, is realm-safe - only `instanceof` is affected.)

import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const WWWROOT = path.resolve(HERE, '../../Bit.Bswup/wwwroot');

export const ORIGIN = 'https://app.example.com';

export function readBundle(name) {
    const file = path.join(WWWROOT, name);
    if (!fs.existsSync(file)) {
        throw new Error(
            `Missing ${file}.\nBuild the package first: dotnet build ../../Bit.Bswup/Bit.Bswup.csproj`
        );
    }
    return fs.readFileSync(file, 'utf8');
}

// ---------------------------------------------------------------------------
// Minimal Request/Response/CacheStorage
// ---------------------------------------------------------------------------

class FakeRequest {
    constructor(url, init = {}) {
        this.url = new URL(String(url), ORIGIN + '/').toString();
        this.method = init.method || 'GET';
        this.mode = init.mode || 'cors';
        this.integrity = init.integrity;
        this.cache = init.cache;
        this.headers = init.headers;
    }
}

class FakeResponse {
    constructor(body, init = {}) {
        this.body = body;
        this.status = init.status === undefined ? 200 : init.status;
        this.statusText = init.statusText || '';
        this.headers = init.headers || {};
        this.ok = this.status >= 200 && this.status < 300;
    }
    clone() {
        const copy = new FakeResponse(this.body, { status: this.status, statusText: this.statusText, headers: this.headers });
        copy.cloned = true;
        return copy;
    }
}

class FakeCache {
    constructor() { this.entries = new Map(); }
    async match(url) { return this.entries.get(String(url && url.url ? url.url : url)); }
    async put(url, response) { this.entries.set(String(url && url.url ? url.url : url), response); }
    async delete(url) { return this.entries.delete(String(url && url.url ? url.url : url)); }
    // The Cache API returns Request objects from keys(); the worker only reads `.url`.
    async keys() { return [...this.entries.keys()].map(url => ({ url })); }
}

class FakeCacheStorage {
    constructor() { this.buckets = new Map(); }
    async open(name) {
        if (!this.buckets.has(name)) this.buckets.set(name, new FakeCache());
        return this.buckets.get(name);
    }
    async keys() { return [...this.buckets.keys()]; }
    async delete(name) { return this.buckets.delete(name); }
    // test helper
    snapshot() {
        const out = {};
        for (const [name, cache] of this.buckets) out[name] = [...cache.entries.keys()].sort();
        return out;
    }
}

// ---------------------------------------------------------------------------
// Service worker context
// ---------------------------------------------------------------------------

/**
 * Builds a service-worker-like global, but does NOT run the bundle yet - configuration
 * (`self.assetsManifest`, `self.mode`, ...) must be assigned first, because bit-bswup.sw.js
 * reads it during module evaluation. Call `load()` when the config is in place.
 */
export function createServiceWorkerContext({ fetchHandler } = {}) {
    const clients = [];
    const posted = [];      // everything the worker broadcast to clients
    const fetchLog = [];
    const handlers = {};
    const caches = new FakeCacheStorage();

    const addClient = (id = `client-${clients.length + 1}`) => {
        const client = { id, type: 'window', url: ORIGIN + '/', postMessage: m => posted.push(m) };
        clients.push(client);
        return client;
    };

    const sandbox = {
        console: { log() { }, info() { }, warn() { }, error() { }, group() { }, groupEnd() { } },
        setTimeout: (fn, ms) => setTimeout(fn, Math.min(ms || 0, 5)), // keep retry backoff fast
        clearTimeout,
        URL, URLSearchParams,
        Request: FakeRequest,
        Response: FakeResponse,
        caches,
        fetch: async (req) => {
            const url = typeof req === 'string' ? req : req.url;
            fetchLog.push(url);
            if (fetchHandler) return fetchHandler(url, req);
            return new FakeResponse('ok', { status: 200 });
        },
    };
    sandbox.self = sandbox;                 // a worker's global IS `self`
    sandbox.globalThis = sandbox;

    sandbox.location = { origin: ORIGIN, href: ORIGIN + '/' };
    sandbox.importScripts = () => { };       // tests assign self.assetsManifest directly
    sandbox.addEventListener = (type, fn) => { handlers[type] = fn; };
    sandbox.skipWaiting = async () => { };
    sandbox.clients = {
        claim: async () => { },
        matchAll: async () => clients.slice(),
    };

    vm.createContext(sandbox);

    const api = {
        self: sandbox,
        handlers,
        caches,
        clients,
        posted,
        fetchLog,
        addClient,
        /** Build a RegExp with the sandbox's own intrinsic so `instanceof RegExp` holds inside. */
        regex: (source, flags = '') =>
            vm.runInContext(`new RegExp(${JSON.stringify(source)}, ${JSON.stringify(flags)})`, sandbox),
        /** Build an Array with the sandbox's own intrinsic so `instanceof Array` holds inside. */
        array(items = []) {
            const arr = vm.runInContext('[]', sandbox);
            for (const item of items) arr.push(item);
            return arr;
        },
        /** Assign worker config onto `self`, marshalling arrays into the sandbox realm. */
        configure(config) {
            for (const [key, value] of Object.entries(config)) {
                sandbox[key] = Array.isArray(value) ? api.array(value) : value;
            }
            return api;
        },
        /**
         * Let background work finish. Under the default 'lax' tolerance handleInstall does NOT
         * await createAssetsCache - install completes immediately and the cache fills in the
         * background - so progress/error messages land after the install promise resolves.
         * Also drains the (clamped) retry backoff timers.
         */
        async settle() {
            for (let i = 0; i < 8; i++) await new Promise(r => setTimeout(r, 1));
        },
        /** Run the real bundle. Everything above must already be configured. */
        load() {
            vm.runInContext(readBundle('bit-bswup.sw.js'), sandbox);
            return api;
        },
        /** Read a top-level function declared by the bundle (they become globals in a script). */
        fn(name) {
            const f = sandbox[name];
            if (typeof f !== 'function') throw new Error(`bit-bswup.sw.js does not expose ${name}()`);
            return f;
        },
        /** Decoded status messages the worker broadcast (plain string commands are kept as-is). */
        messages() {
            return posted.map(m => { try { return JSON.parse(m); } catch { return m; } });
        },
        messagesOfType(type) {
            return api.messages().filter(m => m && m.type === type);
        },
        /** Drive a lifecycle event and await whatever it passed to waitUntil/respondWith. */
        async fire(type, event = {}) {
            const handler = handlers[type];
            if (!handler) throw new Error(`no '${type}' listener registered`);
            let awaited;
            const e = { ...event, waitUntil: p => { awaited = p; }, respondWith: p => { awaited = p; } };
            handler(e);
            return awaited;
        },
    };
    return api;
}

// ---------------------------------------------------------------------------
// Page context (bit-bswup.js and/or bit-bswup.progress.js)
// ---------------------------------------------------------------------------

function createElement(tag) {
    const el = {
        tagName: tag,
        _attrs: {},
        children: [],
        textContent: '',
        style: {
            _props: {},
            setProperty(k, v) { this._props[k] = v; },
            getPropertyValue(k) { return this._props[k]; },
        },
        getAttribute(n) { return n in this._attrs ? this._attrs[n] : null; },
        setAttribute(n, v) { this._attrs[n] = String(v); },
        append(...nodes) { this.children.push(...nodes); },
        prepend(...nodes) { this.children.unshift(...nodes); },
    };
    return el;
}

/**
 * Builds a browser-like global for the page bundles. `elements` maps element id -> attributes,
 * so a test can stand up just the parts of the splash markup it cares about.
 */
export function createPageContext({ elements = {}, appContainer = null, readyState = 'complete' } = {}) {
    const byId = {};
    for (const [id, attrs] of Object.entries(elements)) {
        byId[id] = createElement('div');
        for (const [k, v] of Object.entries(attrs || {})) byId[id].setAttribute(k, v);
    }

    const listeners = {};
    const observers = [];
    const scripts = [];
    const reloads = { count: 0 };
    const registrations = [];

    const sandbox = {
        console: { log() { }, info() { }, warn() { }, error() { }, debug() { } },
        setTimeout: (fn, ms) => setTimeout(fn, Math.min(ms || 0, 5)),
        clearTimeout, setInterval: () => 0, clearInterval() { },
        URL, URLSearchParams,
        MutationObserver: class {
            constructor(cb) { this.cb = cb; this.observing = false; observers.push(this); }
            observe() { this.observing = true; }
            disconnect() { this.observing = false; }
        },
    };
    sandbox.window = sandbox;
    sandbox.globalThis = sandbox;

    sandbox.location = { origin: ORIGIN, href: ORIGIN + '/', reload: () => { reloads.count++; } };
    sandbox.document = {
        readyState,
        currentScript: null,
        scripts,
        documentElement: createElement('html'),
        visibilityState: 'visible',
        getElementById: id => byId[id] || null,
        querySelector: sel => (appContainer && sel === appContainer.selector ? appContainer.el : null),
        createElement,
        addEventListener: (t, fn) => { (listeners[t] ||= []).push(fn); },
    };
    sandbox.addEventListener = (t, fn) => { (listeners[t] ||= []).push(fn); };
    sandbox.caches = new FakeCacheStorage();

    vm.createContext(sandbox);

    const api = {
        window: sandbox,
        elements: byId,
        listeners,
        observers,
        reloads,
        registrations,
        caches: sandbox.caches,
        regex: (source, flags = '') =>
            vm.runInContext(`new RegExp(${JSON.stringify(source)}, ${JSON.stringify(flags)})`, sandbox),
        /** Add an element after load, as an interactive Blazor render would. */
        addElement(id, attrs = {}) {
            byId[id] = createElement('div');
            for (const [k, v] of Object.entries(attrs)) byId[id].setAttribute(k, v);
            observers.filter(o => o.observing).forEach(o => o.cb());
            return byId[id];
        },
        /** Declare the <script src="...bit-bswup.js" ...> tag that extract() reads options from. */
        addBswupScriptTag(attrs = {}) {
            const attributes = {};
            for (const [k, v] of Object.entries(attrs)) attributes[k] = { value: String(v) };
            const tag = { src: `${ORIGIN}/_content/Bit.Bswup/bit-bswup.js`, attributes };
            scripts.push(tag);
            sandbox.document.currentScript = tag;
            return tag;
        },
        addBlazorScriptTag({ src = '_framework/blazor.web.js', autostart = 'false' } = {}) {
            scripts.push({ src: `${ORIGIN}/${src}`, attributes: { autostart: { value: autostart } } });
        },
        /** Install a fake navigator.serviceWorker. `register` resolves to the given registration. */
        setServiceWorker({ registration = null, registerError = null, controller = null } = {}) {
            const swListeners = {};
            sandbox.navigator = {
                serviceWorker: {
                    controller,
                    addEventListener: (t, fn) => { (swListeners[t] ||= []).push(fn); },
                    register: (sw, opts) => {
                        registrations.push(opts && 'scope' in opts ? opts.scope : '<omitted>');
                        if (registerError && registrations.length === 1) return Promise.reject(registerError);
                        return Promise.resolve(registration || { active: null, installing: null, waiting: null, addEventListener() { }, update: async () => { } });
                    },
                    getRegistration: async () => registration,
                },
            };
            api.swListeners = swListeners;
        },
        fire(type) { (listeners[type] || []).forEach(fn => fn()); },
        /** Deliver a message from the service worker to the page. */
        message(data) { (api.swListeners?.message || []).forEach(fn => fn({ data, source: { postMessage() { } } })); },
        load(bundle) { vm.runInContext(readBundle(bundle), sandbox); return api; },
        /** Let queued promise jobs settle. */
        settle: () => new Promise(r => setImmediate(() => setImmediate(r))),
    };

    if (!sandbox.navigator) api.setServiceWorker();
    return api;
}
