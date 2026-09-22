// Tests BitBlazorUI.Extras.initScripts / initStylesheets - the loader that decides whether a script or a
// stylesheet an app asks for has to be injected, or is already on the page and only has to be waited for.
//
// It runs the SHIPPED bundle (wwwroot/scripts/bit.blazorui.extras.js, what the TypeScript compiles to)
// inside a node VM with a hand-written DOM of just the surface the loader touches. A hand-written DOM
// rather than jsdom because the tests are all about timing - a tag that is still in flight, one whose load
// event already fired, one that fails halfway - and that is exactly what a real DOM will not let you drive.
// It also keeps the library free of a test-only npm dependency; node itself is already required to build it.
//
// Run: node extras-resource-loader.test.js <path to bit.blazorui.extras.js>
// Exits 0 when every test passed, 1 otherwise, printing one line per test.

'use strict';

const fs = require('fs');
const vm = require('vm');

const bundlePath = process.argv[2];
if (!bundlePath) {
    console.error('usage: node extras-resource-loader.test.js <bundle.js>');
    process.exit(2);
}

const bundleSource = fs.readFileSync(bundlePath, 'utf8');


// ---------------------------------------------------------------------------------------------------
// A DOM with only what the loader reads, and with every load/error event under the test's control.
// ---------------------------------------------------------------------------------------------------

const BASE_URI = 'https://example.test/app/';

function createDom() {
    const noop = () => { };

    class FakeElement {
        constructor(tagName) {
            this.tagName = tagName.toUpperCase();
            this.attributes = {};
            this.listeners = { load: [], error: [] };
            this.parent = null;
            // Only the properties the loader sets or reads.
            this.src = '';
            this.href = '';
            this.rel = '';
            this.type = '';
            this.async = true;
            this.noModule = false;
            this.sheet = null; // a stylesheet that has not been applied
            // The bundle holds more than the loader, and some of it touches the document on load.
            this.style = { setProperty: noop, removeProperty: noop, getPropertyValue: () => '' };
            this.classList = { add: noop, remove: noop, contains: () => false, toggle: noop };
        }

        addEventListener(name, handler) { (this.listeners[name] ??= []).push(handler); }

        removeEventListener(name, handler) {
            const list = this.listeners[name];
            if (!list) return;
            const index = list.indexOf(handler);
            if (index >= 0) list.splice(index, 1);
        }

        setAttribute(name, value) { this.attributes[name] = String(value); }

        getAttribute(name) { return Object.hasOwn(this.attributes, name) ? this.attributes[name] : null; }

        hasAttribute(name) { return Object.hasOwn(this.attributes, name); }

        remove() { this.parent?.remove(this); this.parent = null; }

        // What a browser would do; here the test decides when.
        fireLoad() { [...(this.listeners.load ?? [])].forEach(h => h()); }

        fireError() { [...(this.listeners.error ?? [])].forEach(h => h()); }
    }

    class FakeContainer {
        constructor(dom) { this.dom = dom; this.children = []; }

        appendChild(element) {
            element.parent = this;
            this.children.push(element);
            this.dom.appended.push(element);
            return element;
        }

        remove(element) {
            const index = this.children.indexOf(element);
            if (index >= 0) this.children.splice(index, 1);
        }
    }

    const dom = {
        appended: [],       // every tag the loader injected, in order
        timingEntries: {},  // url -> { responseStatus } | null, i.e. what Resource Timing knows
        windowLoadHandlers: [],
    };

    dom.head = new FakeContainer(dom);
    dom.body = new FakeContainer(dom);

    const document = {
        baseURI: BASE_URI,
        readyState: 'loading',
        head: dom.head,
        body: dom.body,
        createElement: tagName => new FakeElement(tagName),
        // Every script tag on the page: the ones the host wrote plus the ones the loader injected.
        get scripts() {
            return [...dom.hostScripts, ...dom.body.children.filter(e => e.tagName === 'SCRIPT')];
        },
        querySelectorAll: selector => {
            if (selector !== 'link[rel="stylesheet"]') return [];
            return [...dom.hostLinks, ...dom.head.children.filter(e => e.tagName === 'LINK' && e.rel === 'stylesheet')];
        },
        addEventListener: noop,
        removeEventListener: noop,
        getElementById: () => null,
        querySelector: () => null,
        documentElement: new FakeElement('html'),
    };

    dom.hostScripts = [];
    dom.hostLinks = [];
    dom.document = document;

    dom.addHostScript = properties => {
        const script = new FakeElement('script');
        Object.assign(script, properties);
        dom.hostScripts.push(script);
        return script;
    };

    dom.addHostLink = properties => {
        const link = new FakeElement('link');
        link.rel = 'stylesheet';
        Object.assign(link, properties);
        dom.hostLinks.push(link);
        return link;
    };

    dom.fireWindowLoad = () => { [...dom.windowLoadHandlers].forEach(h => h()); };

    dom.injected = kind => dom.appended.filter(e => e.tagName === (kind === 'script' ? 'SCRIPT' : 'LINK'));

    const sandbox = {
        console,
        document,
        performance: {
            getEntriesByName: name => {
                const entry = dom.timingEntries[name];
                return entry === undefined ? [] : [entry];
            },
            now: () => 0,
        },
        URL,
        setTimeout, clearTimeout, setInterval, clearInterval, queueMicrotask,
        navigator: { userAgent: 'node' },
        location: { href: BASE_URI },
        matchMedia: () => ({ matches: false, addEventListener: noop, removeEventListener: noop }),
        addEventListener: (name, handler) => { if (name === 'load') dom.windowLoadHandlers.push(handler); },
        removeEventListener: (name, handler) => {
            if (name !== 'load') return;
            const index = dom.windowLoadHandlers.indexOf(handler);
            if (index >= 0) dom.windowLoadHandlers.splice(index, 1);
        },
    };
    sandbox.window = sandbox;
    sandbox.globalThis = sandbox;
    sandbox.self = sandbox;

    vm.createContext(sandbox);
    vm.runInContext(bundleSource, sandbox, { filename: 'bit.blazorui.extras.js' });

    // The bundle registers window handlers of its own while it loads; only the loader's are this test's
    // business, so they are counted from here on.
    dom.baselineWindowLoadHandlers = dom.windowLoadHandlers.length;
    dom.loaderWindowLoadHandlers = () => dom.windowLoadHandlers.length - dom.baselineWindowLoadHandlers;

    dom.Extras = sandbox.BitBlazorUI.Extras;

    return dom;
}


// ---------------------------------------------------------------------------------------------------
// A pocket test runner: no dependencies, one line per test, exit code says whether they all passed.
// ---------------------------------------------------------------------------------------------------

const tests = [];
const test = (name, body) => tests.push({ name, body });

function assert(condition, message) {
    if (!condition) throw new Error(message);
}

function assertEqual(actual, expected, message) {
    if (actual !== expected) throw new Error(`${message} (expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)})`);
}

// Lets every already-resolvable promise settle, so "did it settle?" can be asked meaningfully.
const drain = () => new Promise(resolve => setTimeout(resolve, 0));

async function settled(promise) {
    let state = 'pending';
    promise.then(() => { state = 'fulfilled'; }, () => { state = 'rejected'; });
    await drain();
    return state;
}


// ---------------------------------------------------------------------------------------------------
// Injecting
// ---------------------------------------------------------------------------------------------------

test('a script nobody has on the page is injected, and the call resolves only once it has executed', async () => {
    const dom = createDom();

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1, 'the script must be injected');
    assertEqual(await settled(promise), 'pending', 'it must not resolve before the script has run');

    dom.injected('script')[0].fireLoad();

    assertEqual(await settled(promise), 'fulfilled', 'it must resolve once the script has run');
});

test('an injected script is ordered, not async, so a dependent script still runs after its dependency', async () => {
    const dom = createDom();

    dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script')[0].async, false, 'async must be turned off for insertion-order execution');
});

test('every script of one call is kicked off before the first is awaited, so the downloads overlap', async () => {
    const dom = createDom();

    dom.Extras.initScripts(['https://cdn.test/a.js', 'https://cdn.test/b.js'], false);

    assertEqual(dom.injected('script').length, 2, 'both must be in flight at once');
    assertEqual(dom.injected('script')[0].src, 'https://cdn.test/a.js', 'and in the order they were asked for');
    assertEqual(dom.injected('script')[1].src, 'https://cdn.test/b.js');
});

test('a module script is injected as a module', async () => {
    const dom = createDom();

    dom.Extras.initScripts(['https://cdn.test/lib.js'], true);

    assertEqual(dom.injected('script')[0].type, 'module');
});

test('scripts go to the body and stylesheets to the head', async () => {
    const dom = createDom();

    dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    dom.Extras.initStylesheets(['https://cdn.test/lib.css']);

    assertEqual(dom.body.children.length, 1, 'the script belongs in the body');
    assertEqual(dom.head.children.length, 1, 'the stylesheet belongs in the head');
});

test('a stylesheet resolves on its load event', async () => {
    const dom = createDom();

    const promise = dom.Extras.initStylesheets(['https://cdn.test/lib.css']);

    assertEqual(await settled(promise), 'pending');

    const link = dom.injected('stylesheet')[0];
    link.sheet = {}; // the browser applied it
    link.fireLoad();

    assertEqual(await settled(promise), 'fulfilled');
});

test('nothing to load resolves', async () => {
    const dom = createDom();

    assertEqual(await settled(dom.Extras.initScripts([], false)), 'fulfilled');
    assertEqual(await settled(dom.Extras.initStylesheets([])), 'fulfilled');
    assertEqual(await settled(dom.Extras.initScripts(null, false)), 'fulfilled', 'a null list is not a failure');
    assertEqual(await settled(dom.Extras.initStylesheets(null)), 'fulfilled');
});


// ---------------------------------------------------------------------------------------------------
// Not loading the same thing twice
// ---------------------------------------------------------------------------------------------------

test('two callers asking for the same script at once share one injection and both wait for it', async () => {
    const dom = createDom();

    const first = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    const second = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1, 'the second caller must not inject a second tag');
    assertEqual(await settled(first), 'pending');
    assertEqual(await settled(second), 'pending', 'the second caller must wait for the real load, not assume it');

    dom.injected('script')[0].fireLoad();

    assertEqual(await settled(first), 'fulfilled');
    assertEqual(await settled(second), 'fulfilled');
});

test('a caller that arrives after the load is over resolves without injecting again', async () => {
    const dom = createDom();

    const first = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    dom.injected('script')[0].fireLoad();
    await settled(first);

    const second = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1);
    assertEqual(await settled(second), 'fulfilled');
});

test('the same url written relatively and absolutely is one resource', async () => {
    const dom = createDom();

    dom.Extras.initScripts(['lib.js'], false);
    dom.Extras.initScripts(['https://example.test/app/lib.js'], false);

    assertEqual(dom.injected('script').length, 1, 'both spellings resolve against the document base to one key');
});

test("a host copy carrying a cache-buster is reused rather than re-executed", async () => {
    // Re-executing e.g. mapbox-gl would wipe the access token the host already set on it.
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: 'https://cdn.test/mapbox-gl.js?v=2' });

    const promise = dom.Extras.initScripts(['https://cdn.test/mapbox-gl.js'], false);

    assertEqual(dom.injected('script').length, 0, 'the query string is not part of the identity');
    assertEqual(await settled(promise), 'fulfilled');
});

test('a classic script and a module script of the same url are different resources', async () => {
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: 'https://cdn.test/lib.js', type: '' });

    dom.Extras.initScripts(['https://cdn.test/lib.js'], true);

    assertEqual(dom.injected('script').length, 1, 'a classic tag cannot answer for a module request');
    assertEqual(dom.injected('script')[0].type, 'module');
});

test('a data block is never mistaken for a script that ran', async () => {
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: 'https://cdn.test/lib.js', type: 'application/json' });

    dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1, 'a non-executable type must not satisfy the lookup');
});

test('a nomodule script is never mistaken for a script that ran', async () => {
    // It does not execute in a module-capable browser, which every browser the library targets is.
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: 'https://cdn.test/lib.js', noModule: true });

    dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1);
});


// ---------------------------------------------------------------------------------------------------
// Waiting for what the host put on the page
// ---------------------------------------------------------------------------------------------------

test('a host script that is still in flight is awaited, not assumed ready', async () => {
    const dom = createDom();
    dom.document.readyState = 'loading';
    const hostScript = dom.addHostScript({ src: 'https://cdn.test/lib.js' });

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 0, 'the host tag is the one to wait for');
    assertEqual(await settled(promise), 'pending', 'a tag in the DOM is not a tag that has run');

    hostScript.fireLoad();

    assertEqual(await settled(promise), 'fulfilled');
});

test('a host script whose fetch is already over is not waited on', async () => {
    // Its load event fired before we got here and never fires again, so waiting would stall init forever.
    const dom = createDom();
    dom.document.readyState = 'loading';
    dom.addHostScript({ src: 'https://cdn.test/lib.js' });
    dom.timingEntries['https://cdn.test/lib.js'] = { responseStatus: 200 };

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(await settled(promise), 'fulfilled');
    assertEqual(dom.injected('script').length, 0);
});

test('the window load event settles a wait whose own events never came', async () => {
    const dom = createDom();
    dom.document.readyState = 'loading';
    dom.addHostScript({ src: 'https://cdn.test/lib.js' });

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    assertEqual(await settled(promise), 'pending');

    dom.fireWindowLoad();

    assertEqual(await settled(promise), 'fulfilled', 'the backstop must settle it');
});

test('a wait that is over stops listening, so nothing is kept alive by a listener that never fires', async () => {
    const dom = createDom();
    dom.document.readyState = 'loading';
    const hostScript = dom.addHostScript({ src: 'https://cdn.test/lib.js' });

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    hostScript.fireLoad();
    await settled(promise);

    assertEqual(hostScript.listeners.load.length, 0, 'the element listeners must be removed');
    assertEqual(hostScript.listeners.error.length, 0);
    assertEqual(dom.loaderWindowLoadHandlers(), 0, 'the window backstop above all, since it may never fire');
});

test('a host stylesheet that has been applied is ready at any point in the page load', async () => {
    const dom = createDom();
    dom.document.readyState = 'loading';
    dom.addHostLink({ href: 'https://cdn.test/lib.css', sheet: {} });

    const promise = dom.Extras.initStylesheets(['https://cdn.test/lib.css']);

    assertEqual(await settled(promise), 'fulfilled', 'an applied stylesheet must not wait on an event that already fired');
    assertEqual(dom.injected('stylesheet').length, 0);
});


// ---------------------------------------------------------------------------------------------------
// Failing
// ---------------------------------------------------------------------------------------------------

test('an injected script that fails rejects, and its tag is taken back off the page', async () => {
    const dom = createDom();

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    const injected = dom.injected('script')[0];
    injected.fireError();

    assertEqual(await settled(promise), 'rejected');
    assertEqual(dom.body.children.length, 0, 'the broken tag must not be left to answer a later lookup');
});

test('a failure is not remembered, so a retry really retries', async () => {
    const dom = createDom();

    const first = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    dom.injected('script')[0].fireError();
    await settled(first);

    const second = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 2, 'a fresh tag must be injected');

    dom.injected('script')[1].fireLoad();

    assertEqual(await settled(second), 'fulfilled', 'and the retry must be able to succeed');
});

test('one failing resource does not take the others with it', async () => {
    const dom = createDom();

    const promise = dom.Extras.initScripts(['https://cdn.test/a.js', 'https://cdn.test/b.js'], false);
    const [a, b] = dom.injected('script');
    a.fireError();
    b.fireLoad();

    assertEqual(await settled(promise), 'rejected', 'the call reports the failure');

    // b loaded, and what was learned about it is kept: asking for it again must not re-inject it.
    const again = dom.Extras.initScripts(['https://cdn.test/b.js'], false);
    assertEqual(dom.injected('script').length, 2, 'b is known to be loaded');
    assertEqual(await settled(again), 'fulfilled');
});

test('a host script the page failed to fetch is skipped and a working one injected in its place', async () => {
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: 'https://cdn.test/lib.js' });
    dom.timingEntries['https://cdn.test/lib.js'] = { responseStatus: 404 };

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1, 'a 404 tag is not a loaded resource');

    dom.injected('script')[0].fireLoad();

    assertEqual(await settled(promise), 'fulfilled');
});

test('a host script that fails while being waited on is marked, and a fresh one is injected', async () => {
    const dom = createDom();
    dom.document.readyState = 'loading';
    const hostScript = dom.addHostScript({ src: 'https://cdn.test/lib.js' });

    const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);
    hostScript.fireError();
    await drain(); // the fall back to a fresh tag hangs off the rejection, so it lands a microtask later

    assert(hostScript.hasAttribute('data-bit-load-failed'), 'the broken host tag must be marked');
    assertEqual(dom.injected('script').length, 1, 'and replaced');

    dom.injected('script')[0].fireLoad();

    assertEqual(await settled(promise), 'fulfilled', 'the caller sees the working one, not the failure');
});

test('a host stylesheet that never applied is replaced once the page has finished loading', async () => {
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostLink({ href: 'https://cdn.test/lib.css', sheet: null });

    dom.Extras.initStylesheets(['https://cdn.test/lib.css']);

    assertEqual(dom.injected('stylesheet').length, 1, 'no .sheet after load means it did not apply');
});

test('a stylesheet whose load event fires without it being applied is a failure', async () => {
    const dom = createDom();

    const promise = dom.Extras.initStylesheets(['https://cdn.test/lib.css']);
    dom.injected('stylesheet')[0].fireError();

    assertEqual(await settled(promise), 'rejected');
});


// ---------------------------------------------------------------------------------------------------
// What cannot be known
// ---------------------------------------------------------------------------------------------------

test('a cross-origin host script whose status cannot be read is taken at its word', async () => {
    // Without Timing-Allow-Origin the entry reports responseStatus 0, and some browsers report nothing at
    // all. Neither says the script failed, so assuming it did would re-execute a working script.
    for (const responseStatus of [0, undefined]) {
        const dom = createDom();
        dom.document.readyState = 'complete';
        dom.addHostScript({ src: 'https://cdn.test/lib.js' });
        dom.timingEntries['https://cdn.test/lib.js'] = { responseStatus };

        const promise = dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

        assertEqual(dom.injected('script').length, 0, `responseStatus ${responseStatus} must not read as a failure`);
        assertEqual(await settled(promise), 'fulfilled');
    }
});

test('a host script with no src (an inline block) never answers for a url', async () => {
    const dom = createDom();
    dom.document.readyState = 'complete';
    dom.addHostScript({ src: '' });

    dom.Extras.initScripts(['https://cdn.test/lib.js'], false);

    assertEqual(dom.injected('script').length, 1);
});


// ---------------------------------------------------------------------------------------------------

(async () => {
    let failures = 0;

    for (const { name, body } of tests) {
        try {
            await body();
            console.log(`  PASS  ${name}`);
        } catch (error) {
            failures++;
            console.log(`  FAIL  ${name}`);
            console.log(`        ${error && error.message}`);
        }
    }

    console.log(`${tests.length - failures}/${tests.length} passed`);
    process.exit(failures === 0 ? 0 : 1);
})();
