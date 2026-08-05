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

/**
 * Polls until `predicate()` returns true. Used where the code under test is driven by a real
 * (harness-clamped) timer rather than a promise the test can await: a fixed sleep has to guess
 * a duration that is simultaneously long enough not to flake on a loaded CI machine and short
 * enough not to pad the suite, and it never gets both. Polling returns on the first tick after
 * the timer fires and only spends the full `timeout` when the behavior is genuinely broken -
 * where it then fails with a clear message instead of a bare assertion mismatch.
 */
export async function waitFor(predicate, message = 'condition', timeout = 2000) {
    const deadline = Date.now() + timeout;
    while (!predicate()) {
        if (Date.now() >= deadline) throw new Error(`waitFor timed out after ${timeout}ms waiting for: ${message}`);
        await new Promise(r => setTimeout(r, 1));
    }
}

// ---------------------------------------------------------------------------
// Minimal Request/Response/CacheStorage
// ---------------------------------------------------------------------------

class FakeRequest {
    constructor(url, init = {}) {
        // Real Request rejects integrity on a no-cors request - the exact combination
        // createNewAssetRequest carefully avoids; regressing that must fail here too.
        if (init.integrity && init.mode === 'no-cors') {
            throw new TypeError("Failed to construct 'Request': The integrity attribute is unsupported in no-cors mode.");
        }
        this.url = new URL(String(url), ORIGIN + '/').toString();
        this.method = init.method || 'GET';
        this.mode = init.mode || 'cors';
        this.integrity = init.integrity;
        this.cache = init.cache;
        this.headers = init.headers;
    }
}

// Minimal Blob stand-in for Range-slicing tests: carries its bytes openly (`.bytes`) so a
// test can decode what a sliced 206 body contains. slice() is lazy-ish like the real thing.
export class FakeBlob {
    constructor(bytes) {
        this.bytes = bytes instanceof Uint8Array ? bytes : new TextEncoder().encode(String(bytes));
        this.size = this.bytes.length;
    }
    slice(start = 0, end = this.size) { return new FakeBlob(this.bytes.slice(start, end)); }
}

/** Decodes a response body produced by the worker (string, BufferSource or FakeBlob). */
export function decodeBody(response) {
    const body = response && response.body;
    if (body instanceof FakeBlob) return new TextDecoder().decode(body.bytes);
    if (typeof body === 'string') return body;
    return new TextDecoder().decode(body);
}

export class FakeResponse {
    constructor(body, init = {}) {
        // Real Response rejects a statusText that is not a valid HTTP reason-phrase - the
        // historical prohibited-URL crash came from interpolating a URL into statusText.
        if (init.statusText !== undefined && /[^\t\x20-\x7E\x80-\xFF]/.test(String(init.statusText))) {
            throw new TypeError("Failed to construct 'Response': Invalid statusText");
        }
        this.body = body;
        this.status = init.status === undefined ? 200 : init.status;
        this.statusText = init.statusText || '';
        this.headers = init.headers || {};
        this.ok = this.status >= 200 && this.status < 300;
        this.bodyUsed = false;
        // A followed redirect sets response.redirected. The worker must strip it before replaying
        // the response to a navigation (redirect mode 'manual'), or the browser throws - see
        // cleanRedirect in bit-bswup.sw.ts. A freshly constructed Response is never redirected,
        // so a rebuilt response correctly reports false unless a test opts one in.
        this.redirected = init.redirected || false;
    }
    clone() {
        // Real Response.clone() throws once the body is consumed - keeping that here is what
        // lets the harness catch a missing clone() before a cache.put.
        if (this.bodyUsed) throw new TypeError("Failed to execute 'clone': Response body is already used");
        const copy = new FakeResponse(this.body, { status: this.status, statusText: this.statusText, headers: this.headers, redirected: this.redirected });
        copy.cloned = true;
        return copy;
    }
    _consume() {
        if (this.bodyUsed) throw new TypeError('Response body is already used');
        this.bodyUsed = true;
    }
    async text() { this._consume(); return String(this.body ?? ''); }
    async arrayBuffer() { this._consume(); return new TextEncoder().encode(String(this.body ?? '')).buffer; }
    async blob() { this._consume(); return new FakeBlob(String(this.body ?? '')); }
    // Response.error() is what a worker returns from respondWith to signal "network error"
    // without rejecting. Marked so tests can tell it apart from a real response.
    static error() {
        const r = new FakeResponse(null, { status: 0 });
        r.type = 'error';
        return r;
    }
}

class FakeCache {
    constructor() { this.entries = new Map(); }
    // The real Cache API hands back a COPY on every match - reading or consuming a matched
    // response never affects the stored entry. Reviving through the stored object's prototype
    // keeps both FakeResponse instances (prototype methods) and the plain-object fixtures
    // tests build (own-property clone()/blob()) working.
    async match(url) {
        const stored = this.entries.get(String(url && url.url ? url.url : url));
        if (!stored || typeof stored !== 'object') return stored;
        const fresh = Object.assign(Object.create(Object.getPrototypeOf(stored)), stored);
        fresh.bodyUsed = false;
        return fresh;
    }
    async put(url, response) {
        // Spec fidelity (Cache.put): a 206 Partial Content response and a consumed body are
        // both rejections in every browser. Enforcing them here means a regression that drops
        // the worker's own guards (lazyFill's 206 check, the clone-before-put discipline)
        // fails tests instead of only failing live.
        if (response && response.status === 206) throw new TypeError("Failed to execute 'put': Partial response (status code 206) is unsupported");
        if (response && response.bodyUsed === true) throw new TypeError("Failed to execute 'put': Response body is already used");
        if (response && typeof response === 'object') response.bodyUsed = true;
        const key = String(url && url.url ? url.url : url);
        // Re-putting an existing key moves it to the END in real browsers (Batch Cache
        // Operations: delete-then-append) - ordering the generation-cap logic relies on.
        this.entries.delete(key);
        this.entries.set(key, response);
    }
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
export function createServiceWorkerContext({ fetchHandler, cacheStorageError } = {}) {
    const clients = [];
    const posted = [];      // everything the worker broadcast to clients
    const fetchLog = [];
    const handlers = {};
    const caches = new FakeCacheStorage();
    // Simulates CacheStorage being unusable (quota exhausted, private mode, storage pressure).
    if (cacheStorageError) {
        caches.open = async () => { throw cacheStorageError; };
    }

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
        Headers, // used by the worker's Range slicing (applyRangeHeader)
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
    // Real event targets run EVERY listener registered for a type; storing a single function
    // silently dropped earlier ones, so a refactor that splits a handler in two behaved
    // differently in tests than live. handlers[type] stays directly callable (a dispatcher),
    // so existing `sw.handlers.message(e)` call sites keep working.
    const handlerLists = {};
    sandbox.addEventListener = (type, fn) => {
        (handlerLists[type] ||= []).push(fn);
        handlers[type] = e => handlerLists[type].forEach(f => f(e));
    };
    sandbox.skipWaiting = async () => { };
    sandbox.clients = {
        claim: async () => { },
        // Honors the type filter like a browser would: the worker relies on
        // { type: 'window' } to keep non-window clients (dedicated/shared workers) out of
        // its lifecycle broadcasts.
        matchAll: async (opts) => clients.filter(c => !opts || !opts.type || c.type === opts.type),
    };

    vm.createContext(sandbox);

    let loadedBundle = 'bit-bswup.sw.js';
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
         * Let background work finish. The install promise now covers the whole cache build in
         * both tolerance modes (lax awaits it under waitUntil since v-10-6-0), but message
         * delivery still goes through clients.matchAll() and lands a microtask later, and the
         * lazy-fill/waitUntil work in fetch events remains genuinely backgrounded. Also drains
         * the (clamped) retry backoff timers.
         */
        async settle() {
            for (let i = 0; i < 8; i++) await new Promise(r => setTimeout(r, 1));
        },
        /** Run the real bundle. Everything above must already be configured. */
        load(bundle = 'bit-bswup.sw.js') {
            loadedBundle = bundle;
            vm.runInContext(readBundle(bundle), sandbox);
            return api;
        },
        /** Read a top-level function declared by the bundle (they become globals in a script). */
        fn(name) {
            const f = sandbox[name];
            if (typeof f !== 'function') throw new Error(`${loadedBundle} does not expose ${name}()`);
            return f;
        },
        /** Decoded status messages the worker broadcast (plain string commands are kept as-is). */
        messages() {
            return posted.map(m => { try { return JSON.parse(m); } catch { return m; } });
        },
        messagesOfType(type) {
            return api.messages().filter(m => m && m.type === type);
        },
        /**
         * Dispatch a fetch event. `handled` reports whether the worker called respondWith at
         * all - when it does not, the browser performs its own default request and the worker
         * is entirely out of the failure path, which is the point of the sync router.
         */
        async fetchEvent({ url, method = 'GET', mode = 'cors', headers } = {}) {
            const request = new FakeRequest(url, { method, mode });
            // Real Headers (host realm is fine - only .get() is called) so getRangeHeader and
            // friends behave as in a browser; plain-object headers stay for RequestInit tests.
            if (headers) request.headers = new Headers(headers);
            let responded;
            let dispatchDone = false;
            const waits = [];
            const e = {
                request,
                // Real browsers throw InvalidStateError when respondWith is called after the
                // event finished dispatching. The whole fetch-path design rests on the router
                // deciding SYNCHRONOUSLY (an async handleFetch means every request on the
                // origin hard-fails through the worker), so this must be enforceable here.
                respondWith: p => {
                    if (dispatchDone) throw new Error("InvalidStateError: respondWith called after the fetch event finished dispatching (handleFetch must decide synchronously)");
                    responded = p;
                },
                // Collect EVERY waitUntil (real events accept any number); awaiting only the
                // last one let earlier background work escape the assertion window.
                waitUntil: p => { waits.push(p); },
            };
            handlers.fetch(e);
            dispatchDone = true;
            const response = responded === undefined ? undefined : await responded;
            // Rejections propagate: background work (lazy-fill writes) is required to handle
            // its own failures - a leaked rejected extended event is a worker bug.
            for (const w of waits) await w;
            // A response whose body was consumed before it reached the page is always a bug
            // (a cache.put without clone()); in a browser the page gets a dead stream. Fail
            // loudly here so every fetch test guards it by default.
            if (response && response.bodyUsed === true) {
                throw new Error(`fetchEvent served a response whose body was already consumed (missing clone() before a cache write?): ${request.url}`);
            }
            return { handled: responded !== undefined, response };
        },
        /** Drive a lifecycle event and await whatever it passed to waitUntil/respondWith. */
        async fire(type, event = {}) {
            const handler = handlers[type];
            if (!handler) throw new Error(`no '${type}' listener registered`);
            let responded;
            const waits = [];
            const e = { ...event, waitUntil: p => { waits.push(p); }, respondWith: p => { responded = p; } };
            handler(e);
            // Preserve the old contract: undefined when the handler committed to nothing.
            if (responded === undefined && waits.length === 0) return undefined;
            let result = responded === undefined ? undefined : await responded;
            for (const w of waits) {
                const value = await w; // rejections propagate, like a failed extended event
                if (result === undefined) result = value;
            }
            return result;
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
        parentElement: null,
        textContent: '',
        // Real nodes report isConnected; the progress script's element cache re-resolves
        // replaced (disconnected) nodes, so the fake must model the transition (see
        // addElement, which disconnects the node it replaces).
        isConnected: true,
        style: {
            _props: {},
            setProperty(k, v) { this._props[k] = v; },
            getPropertyValue(k) { return this._props[k]; },
        },
        getAttribute(n) { return n in this._attrs ? this._attrs[n] : null; },
        setAttribute(n, v) { this._attrs[n] = String(v); },
        // parentElement is modelled because the progress script hides the bar through its
        // wrapper rather than on the bar itself. Like the real API these accept plain strings
        // (a browser wraps them in a Text node), which have no parent link to set.
        append(...nodes) { this.children.push(...nodes); this._adopt(nodes); },
        prepend(...nodes) { this.children.unshift(...nodes); this._adopt(nodes); },
        _adopt(nodes) { for (const n of nodes) if (n && typeof n === 'object') n.parentElement = this; },
    };
    return el;
}

/**
 * Builds a browser-like global for the page bundles. `elements` maps element id -> attributes,
 * so a test can stand up just the parts of the splash markup it cares about.
 */
export function createPageContext({ elements = {}, appContainer = null, readyState = 'complete', clampLongTimers = false } = {}) {
    const byId = {};
    for (const [id, attrs] of Object.entries(elements)) {
        byId[id] = createElement('div');
        for (const [k, v] of Object.entries(attrs || {})) byId[id].setAttribute(k, v);
    }

    const listeners = {};           // window-level registrations
    const documentListeners = {};   // document-level registrations, kept separate so tests can tell the targets apart
    const observers = [];
    const scripts = [];
    const reloads = { count: 0 };
    const registrations = [];
    const intervals = [];

    const sandbox = {
        console: { log() { }, info() { }, warn() { }, error() { }, debug() { } },
        // Short delays are clamped so retries/yields stay fast. Long delays (>= 1s) keep their
        // real duration and are unref'd: the page script arms a stall watchdog with a 60s
        // default, which must neither fire mid-test (it force-starts Blazor and would break
        // every negative assertion) nor hold the node process open after the run. Watchdog
        // tests see it fire by configuring a sub-second stallTimeout; tests that instead need
        // a long-delay timer to fire (e.g. the progress script's MutationObserver timeout,
        // where no watchdog is in play) opt back into clamping via clampLongTimers.
        setTimeout: (fn, ms) => {
            const clamp = clampLongTimers || !(ms >= 1000);
            const t = setTimeout(fn, clamp ? Math.min(ms || 0, 5) : ms);
            if (!clamp && typeof t.unref === 'function') t.unref();
            return t;
        },
        clearTimeout,
        // Intervals are recorded, never scheduled: tests drive ticks explicitly via
        // api.tickIntervals(). A real setInterval would make the update-polling feature
        // untestable (and flaky); a () => 0 stub made it structurally untestable instead -
        // a cleanup() regression leaving the timer running was undetectable.
        setInterval: (fn, ms) => { intervals.push({ fn, ms, cleared: false }); return intervals.length; },
        clearInterval: id => { if (intervals[id - 1]) intervals[id - 1].cleared = true; },
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
        // Browsers throw a SyntaxError for a malformed selector ('#', '.', '') - code reading
        // an app-provided selector must survive that, so the fake throws the same way.
        querySelector: sel => {
            if (sel === '' || sel === '#' || sel === '.') {
                throw Object.assign(new Error(`'${sel}' is not a valid selector`), { name: 'SyntaxError' });
            }
            return appContainer && sel === appContainer.selector ? appContainer.el : null;
        },
        createElement,
        addEventListener: (t, fn) => { (documentListeners[t] ||= []).push(fn); },
        removeEventListener: (t, fn) => {
            const list = documentListeners[t] || [];
            const index = list.indexOf(fn);
            if (index !== -1) list.splice(index, 1);
        },
    };
    sandbox.addEventListener = (t, fn) => { (listeners[t] ||= []).push(fn); };
    sandbox.removeEventListener = (t, fn) => {
        const list = listeners[t] || [];
        const index = list.indexOf(fn);
        if (index !== -1) list.splice(index, 1);
    };
    sandbox.caches = new FakeCacheStorage();

    vm.createContext(sandbox);

    const api = {
        window: sandbox,
        elements: byId,
        listeners,
        documentListeners,
        observers,
        reloads,
        registrations,
        intervals,
        /** Fire every live interval once, as one timer tick would. */
        tickIntervals() { intervals.filter(i => !i.cleared).forEach(i => i.fn()); },
        caches: sandbox.caches,
        regex: (source, flags = '') =>
            vm.runInContext(`new RegExp(${JSON.stringify(source)}, ${JSON.stringify(flags)})`, sandbox),
        /**
         * Add an element after load, as an interactive Blazor render would. Replacing an
         * existing id disconnects the old node, exactly like a re-render swapping the
         * subtree - which is what the progress script's element cache must survive.
         */
        addElement(id, attrs = {}) {
            if (byId[id]) byId[id].isConnected = false;
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
        /**
         * Install a fake navigator.serviceWorker. `register` resolves to the given registration.
         * The listener store is SHARED across calls: in a browser there is exactly one
         * navigator.serviceWorker event target, so listeners the bundle registered at load time
         * must keep firing after a test swaps in a different registration/controller (the old
         * per-call store silently orphaned them - a test could dispatch into a listener set the
         * bundle never registered on and assert nothing).
         */
        setServiceWorker({ registration = null, registerError = null, controller = null } = {}) {
            const swListeners = api.swListeners || {};
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
        /** Dispatch an event to both targets, like a bubbling browser event reaching each. */
        fire(type) { [...(listeners[type] || []), ...(documentListeners[type] || [])].forEach(fn => fn()); },
        /** Deliver a message from the service worker to the page. */
        message(data, source) { (api.swListeners?.message || []).forEach(fn => fn({ data, source: source || { postMessage() { } } })); },
        load(bundle) {
            vm.runInContext(readBundle(bundle), sandbox);
            // document.currentScript is only set while a classic script executes during parse;
            // browsers null it afterwards. Keeping it set let code read it from callbacks and
            // work in tests while returning null live.
            sandbox.document.currentScript = null;
            return api;
        },
        /** Let queued promise jobs settle. */
        settle: () => new Promise(r => setImmediate(() => setImmediate(r))),
    };

    if (!sandbox.navigator) api.setServiceWorker();
    return api;
}

/** A fake ServiceWorker whose lifecycle the test drives by hand (via fireStateChange). */
export function fakeWorker(state) {
    const listeners = [];
    return {
        state,
        posted: [],
        postMessage(message) { this.posted.push(message); },
        addEventListener(type, fn) { if (type === 'statechange') listeners.push(fn); },
        removeEventListener(type, fn) {
            const index = listeners.indexOf(fn);
            if (index !== -1) listeners.splice(index, 1);
        },
        // Snapshot before dispatch: whenStaged/whenActive remove themselves mid-iteration.
        fireStateChange() { listeners.slice().forEach(fn => fn({ currentTarget: this })); },
    };
}

/** The 100% progress message the service worker sends just before its install promise resolves. */
export const progress100 = JSON.stringify({ type: 'progress', data: { percent: 100, index: 1, asset: { url: 'a.js' } } });
