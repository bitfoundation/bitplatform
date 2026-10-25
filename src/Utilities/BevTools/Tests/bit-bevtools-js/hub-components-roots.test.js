// @vitest-environment happy-dom
// Components in the page, the harder shapes: the panel itself as an interactive root (per-page/component
// interactivity, WebAssembly RootComponents), disposed roots that keep Blazor's preserve-content mark, identical roots
// of two runtimes, and picking details - the Pick button, the resolving window, touch, hovering BevTools' own tree.
import { describe, it, expect, vi } from 'vitest';

async function loadHub() {
    vi.resetModules();
    delete window.__BIT_BEVTOOLS__;
    return import('../../Bit.BevTools/wwwroot/bit-bevtools.js');
}

// As blazor.web.js keeps them: logical children, logical parent, and two marks on a Web App root's start comment -
// "attached" (set false when the root is disposed) and "preserve content" (left true).
const CHILDREN = Symbol();
const PARENT = Symbol();
const ATTACHED = Symbol();
const PRESERVE = Symbol();

function logical(node, parent) {
    node[CHILDREN] ??= [];
    if (parent) {
        node[PARENT] = parent;
        parent[CHILDREN].push(node);
    }
    return node;
}

function rect(el, left, top, width, height) {
    el.getBoundingClientRect = () => ({ left, top, width, height, right: left + width, bottom: top + height, x: left, y: top });
}

function webRoot(attached = true) {
    const start = logical(document.createComment('Blazor:{}'));
    start[ATTACHED] = attached;
    start[PRESERVE] = true;
    document.body.appendChild(start);
    return start;
}

function panelIn(container, targetId = 't1') {
    const panel = logical(document.createElement('div'), container);
    panel.className = 'bev-root';
    panel.setAttribute('data-bev-target', targetId);
    const toggle = document.createElement('button');
    toggle.setAttribute('data-bev-pick-toggle', '');
    panel.appendChild(toggle);
    const tree = document.createElement('div');
    tree.className = 'bev-tree';
    const row = document.createElement('div');
    row.className = 'bev-tree-row';
    row.setAttribute('data-bev-id', '42');
    tree.appendChild(row);
    panel.appendChild(tree);
    document.body.appendChild(panel);
    return { panel, toggle, row };
}

/** An island: a root whose component renders <section><p>text</p></section>. */
function island(attached = true) {
    const root = webRoot(attached);
    const section = logical(document.createElement('section'), root);
    const p = logical(document.createElement('p'), section);
    p.textContent = 'island';
    section.appendChild(p);
    document.body.appendChild(section);
    rect(section, 0, 100, 300, 40);
    return { root, section, p };
}

function runtime(answers, id = 1) {
    const calls = [];
    return {
        calls,
        _id: id,
        invokeMethodAsync(method, ...args) {
            calls.push([method, ...args]);
            if (method === 'Protocol') {
                const [name, json] = args;
                return Promise.resolve(JSON.stringify(answers[name] ? answers[name](JSON.parse(json || 'null')) : { error: 'unknown' }));
            }
            return Promise.resolve();
        },
    };
}

describe('the panel as an interactive root of its own', () => {
    it('Web App: the panel\'s parent is a root start comment, and other roots are still found', async () => {
        document.body.innerHTML = '';
        const panelRoot = webRoot();
        const { panel } = panelIn(panelRoot);
        const { p } = island();
        const hub = await loadHub();
        const ref = runtime({ resolve: args => ({ instanceId: 5, name: 'Island', depth: -1, ...args }) });
        hub.attach(ref, { platform: 'Server' });

        const owner = await window.__BIT_BEVTOOLS__.componentAt(p);
        expect(owner).toMatchObject({ instanceId: 5, name: 'Island' });
        const request = JSON.parse(ref.calls.find(c => c[1] === 'resolve')[2]);
        expect(request).toMatchObject({ anchored: false, signature: '1|section', ordinal: 0, sameCount: 1 });

        // The panel's own root is its anchor: a path from it outlines through the panel's root.
        const result = hub.highlight('t1', { instanceId: 1, name: 'Panel', anchored: true, rootSignature: '', rootOrdinal: 0, path: [{ index: 0, kind: 'div' }] });
        expect(result.ok).toBe(true);
        expect(panel.isConnected).toBe(true);
    });

    it('WebAssembly: RootComponents.Add<BevToolsPanel>("#tools") makes an element the panel\'s logical parent', async () => {
        document.body.innerHTML = '';
        const host = logical(document.createElement('div'));
        host.id = 'tools';
        host[ATTACHED] = true;
        document.body.appendChild(host);
        panelIn(host);
        const { p } = island();
        const hub = await loadHub();
        hub.attach(runtime({ resolve: () => ({ instanceId: 5, name: 'Island', depth: -1 }) }), { platform: 'WebAssembly' });

        expect(await window.__BIT_BEVTOOLS__.componentAt(p)).toMatchObject({ instanceId: 5 });
    });
});

describe('roots that are gone, and roots that look alike', () => {
    it('a disposed root keeps its preserve-content mark but is not a root', async () => {
        document.body.innerHTML = '';
        const anchor = webRoot();
        panelIn(anchor);
        const { p } = island(false);
        const hub = await loadHub();
        const ref = runtime({ resolve: () => ({ instanceId: 5, name: 'Island', depth: -1 }) });
        hub.attach(ref, { platform: 'Server' });

        const owner = await window.__BIT_BEVTOOLS__.componentAt(p);
        expect(owner.error).toMatch(/not inside an interactive component/);
        expect(ref.calls.some(c => c[1] === 'resolve')).toBe(false);
    });

    it('identical roots both runtimes claim are reported as ambiguous, not picked', async () => {
        document.body.innerHTML = '';
        panelIn(webRoot(), 't1');
        panelIn(webRoot(), 't2');
        const { p } = island();
        island();
        const hub = await loadHub();
        hub.attach(runtime({ resolve: () => ({ instanceId: 5, name: 'Counter', depth: -1 }) }, 1), { platform: 'Server' });
        hub.attach(runtime({ resolve: () => ({ instanceId: 9, name: 'Counter', depth: -1 }) }, 2), { platform: 'WebAssembly' });

        const owner = await window.__BIT_BEVTOOLS__.componentAt(p);
        expect(owner.error).toMatch(/cannot tell which one/);
    });

    it('a location among identical roots is found by position only when the counts agree', async () => {
        document.body.innerHTML = '';
        panelIn(webRoot());
        const first = island();
        const second = island();
        rect(second.section, 0, 200, 300, 40);
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'Server' });
        const location = { instanceId: 9, name: 'Island', anchored: false, rootSignature: '1|section', rootOrdinal: 1, rootCount: 2, path: [] };

        expect(hub.highlight('t1', location).ok).toBe(true);
        const box = document.querySelector('.bev-overlay .bev-hl');
        expect(box.style.top).toBe('200px');
        expect(box.textContent).toContain('matched by position');

        // A third identical root belongs to someone else: the position no longer identifies anything.
        island();
        expect(hub.highlight('t1', { ...location, path: [] }).error).toMatch(/cannot tell which one/);
        expect(first.section.isConnected).toBe(true);
    });
});

describe('picking details', () => {
    function page() {
        document.body.innerHTML = '';
        const anchor = webRoot();
        const { panel, toggle, row } = panelIn(anchor);
        const { p } = island();
        return { panel, toggle, row, p };
    }

    it('the panel\'s own Pick button goes through without cancelling, so it can stop picking itself', async () => {
        const { toggle } = page();
        const hub = await loadHub();
        const ref = runtime({});
        hub.attach(ref, { platform: 'Server' });
        const clicked = vi.fn();
        toggle.addEventListener('click', clicked);

        hub.startPick('t1');
        toggle.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        expect(clicked).toHaveBeenCalledTimes(1);
        expect(hub.isPicking(), 'the panel stops it, not the hub').toBe(true);
        expect(ref.calls.some(c => c[0] === 'OnPickEnded')).toBe(false);
        hub.stopPick();
        expect(hub.isPicking()).toBe(false);
    });

    it('is still picking while the clicked node is being resolved', async () => {
        const { p } = page();
        let answer;
        const hub = await loadHub();
        hub.attach({
            _id: 1,
            invokeMethodAsync(method) {
                return method === 'Protocol' ? new Promise(resolve => { answer = resolve; }) : Promise.resolve();
            },
        }, { platform: 'Server' });

        hub.startPick('t1', { tellPanel: false });
        p.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        await vi.waitFor(() => expect(answer).toBeTypeOf('function'));
        expect(hub.isPicking(), 'a frontend polling now must wait for the outcome').toBe(true);
        answer(JSON.stringify({ instanceId: 5, name: 'Island', depth: -1 }));
        await vi.waitFor(() => expect(hub.lastPick()?.instanceId).toBe(5));
        expect(hub.isPicking()).toBe(false);
    });

    it('a tap picks, and the click it would have become is cancelled', async () => {
        const { p } = page();
        const hub = await loadHub();
        hub.attach(runtime({ resolve: () => ({ instanceId: 5, name: 'Island', depth: -1 }) }), { platform: 'Server' });
        document.elementFromPoint = () => p;

        hub.startPick('t1', { tellPanel: false });
        const touch = new Event('touchend', { bubbles: true, cancelable: true });
        touch.changedTouches = [{ clientX: 10, clientY: 110 }];
        p.dispatchEvent(touch);
        expect(touch.defaultPrevented).toBe(true);
        await vi.waitFor(() => expect(hub.lastPick()?.instanceId).toBe(5));
    });

    it('hovering a row of BevTools\' own tree asks the runtime where its component is, without a Blazor event', async () => {
        const { row } = page();
        const hub = await loadHub();
        const ref = runtime({ locate: args => ({ instanceId: args.instanceId, name: 'Island', anchored: false, rootSignature: '1|section', rootOrdinal: 0, rootCount: 1, path: [] }) });
        hub.attach(ref, { platform: 'Server' });

        row.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
        await vi.waitFor(() => expect(document.querySelector('.bev-overlay .bev-hl')?.textContent).toMatch(/^Island/));
        expect(JSON.parse(ref.calls.find(c => c[1] === 'locate')[2])).toEqual({ instanceId: 42 });

        row.dispatchEvent(new MouseEvent('mouseout', { bubbles: true, relatedTarget: document.body }));
        expect(document.querySelector('.bev-overlay .bev-hl')).toBeNull();
    });
});

// ----- traffic: BevTools' own events and the overlay -----

class FakeSocket {
    constructor(url) {
        this.url = url;
        this.listeners = {};
    }

    addEventListener(type, handler) {
        (this.listeners[type] ||= []).push(handler);
    }

    receive(data) {
        for (const handler of this.listeners.message || []) handler({ data });
    }
}
FakeSocket.prototype.send = function () { };

const RS = '\x1e';
const json = message => JSON.stringify(message) + RS;

describe('what BevTools\' own interaction sends over a circuit', () => {
    it('a Blazor event raised in the panel is BevTools\' traffic; the same event in the app is the app\'s', async () => {
        document.body.innerHTML = '<main id="app"><button id="app-button">app</button></main><div class="bev-root" data-bev-target="t1"><button id="tab">tab</button></div>';
        window.WebSocket = FakeSocket;
        const hub = await loadHub();
        hub.attach({ _id: 1, invokeMethodAsync: () => Promise.resolve() }, { platform: 'Server' });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');
        const dispatch = id => json({ type: 1, target: 'BeginInvokeDotNetFromJS', arguments: [String(id), null, 'DispatchEventAsync', 7, '[]'] });

        // Blazor sends the event's message while the browser is dispatching it.
        document.getElementById('tab').addEventListener('click', () => socket.send(dispatch(1)));
        document.getElementById('app-button').addEventListener('click', () => socket.send(dispatch(2)));
        document.getElementById('tab').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        await new Promise(resolve => setTimeout(resolve, 5));
        document.getElementById('app-button').dispatchEvent(new MouseEvent('click', { bubbles: true }));
        socket.receive(json({ type: 1, target: 'JS.EndInvokeDotNet', arguments: ['1', true, null] }));

        const [stats] = hub.traffic();
        expect(stats.messages['→ BeginInvokeDotNetFromJS'].count, 'only the app button\'s').toBe(1);
        expect(stats.own.count, 'the panel event and its completion').toBe(2);
    });

    it('outlines and render flashes do not decide whose a render batch was', async () => {
        document.body.innerHTML = '<main id="app"><p>app</p></main><div class="bev-root" data-bev-target="t1"><span>0</span></div>';
        window.WebSocket = FakeSocket;
        const hub = await loadHub();
        hub.attach({ _id: 1, invokeMethodAsync: () => Promise.resolve() }, { platform: 'Server' });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');
        const str = s => { const b = [...Buffer.from(s, 'utf8')]; return [0xa0 | b.length, ...b]; };
        const arr = (...items) => [0x90 | items.length, ...items.flat()];
        const bin = n => [0xc5, (n >> 8) & 255, n & 255, ...new Array(n).fill(1)];
        const invocation = (target, ...args) => arr([1], [0x80], [0xc0], str(target), arr(...args));
        const frame = message => { const out = []; let length = message.length; do { let b = length & 0x7f; length >>= 7; if (length) b |= 0x80; out.push(b); } while (length); return new Uint8Array([...out, ...message]).buffer; };

        // An app batch that changed nothing, while a flash box appears: still the app's batch.
        socket.receive(frame(invocation('JS.RenderBatch', [3], bin(900))));
        const box = document.createElement('div');
        box.className = 'bev-root bev-overlay';
        document.body.appendChild(box);
        box.appendChild(document.createElement('div'));
        socket.send(frame(invocation('OnRenderCompleted', [3], [0xc0])));
        await new Promise(resolve => setTimeout(resolve, 0));

        const [stats] = hub.traffic();
        expect(stats.renderBatches.count).toBe(1);
        expect(stats.renderBatches.bevTools).toBe(0);
    });
});

describe('symbols for a runtime without files (WebAssembly)', () => {
    it('fetches the PDB the runtime lists for this build, fingerprinted or not, and nothing else', async () => {
        const hub = await loadHub();
        const fetched = [];
        const options = [];
        globalThis.fetch = async (url, init) => { fetched.push(String(url)); options.push(init); return { ok: true, arrayBuffer: async () => new Uint8Array([1, 2, 3]).buffer }; };

        // .NET 9+: a list of resources with fingerprinted names and resolved URLs.
        window.Blazor = { runtime: { getConfig: () => ({ resources: { pdb: [{ virtualPath: 'App.pdb', name: 'App.abc123.pdb', resolvedUrl: 'http://localhost/_framework/App.abc123.pdb', integrity: 'sha256-abc' }] } }) } };
        expect(Array.from(await hub.fetchPdb('App'))).toEqual([1, 2, 3]);
        expect(fetched).toEqual(['http://localhost/_framework/App.abc123.pdb']);
        // The build's own hash, past the HTTP cache: a PDB of an earlier build is refused, not read.
        expect(options[0]).toEqual({ integrity: 'sha256-abc', cache: 'no-cache' });
        expect(await hub.fetchPdb('Other'), 'not listed: no request').toBeNull();
        expect(fetched).toHaveLength(1);

        // .NET 8: a map of plain names.
        window.Blazor = { runtime: { getConfig: () => ({ resources: { pdb: { 'App.pdb': 'sha256-x' } } }) } };
        expect(await hub.fetchPdb('App')).not.toBeNull();
        expect(fetched[1]).toMatch(/\/_framework\/App\.pdb$/);
        expect(options[1].integrity).toBe('sha256-x');

        // A failed download is not "no symbols": it throws, so the panel can ask again.
        globalThis.fetch = async () => ({ ok: false, status: 503 });
        await expect(hub.fetchPdb('App')).rejects.toThrow(/HTTP 503/);
        globalThis.fetch = async (url, init) => { fetched.push(String(url)); options.push(init); return { ok: true, arrayBuffer: async () => new Uint8Array([1, 2, 3]).buffer }; };

        // A Server circuit: no WebAssembly runtime on the page.
        delete window.Blazor;
        expect(await hub.fetchPdb('App')).toBeNull();
    });
});

describe('accessibility checks of the page', () => {
    it('finds what markup alone proves, names the component, and leaves BevTools out unless asked', async () => {
        document.body.innerHTML = '';
        document.documentElement.removeAttribute('lang');
        const anchor = webRoot();
        panelIn(anchor);
        // A component in the app: its unnamed icon button is a finding with a component.
        const container = logical(document.createComment('!'), anchor);
        const iconButton = logical(document.createElement('button'), container);
        iconButton.innerHTML = '<svg></svg>';
        document.body.append(container, iconButton);
        // Static markup: findings without a component.
        document.body.insertAdjacentHTML('beforeend', `
            <img src="a.png">
            <img src="b.png" alt="">
            <button aria-label="Close"></button>
            <a href="/x"><img src="c.png" alt="Home"></a>
            <input type="text" placeholder="Search">
            <label>Name <input type="text"></label>
            <label for="email">Email</label><input id="email" type="email">
            <input type="hidden">
            <div aria-hidden="true"><button>Hidden but focusable</button></div>
            <div tabindex="3">first</div>
            <span id="dup">a</span><span id="dup">b</span><input aria-labelledby="dup">
            <h2></h2><h2>Title</h2>
            <div style="display:none"><img src="d.png"></div>`);
        const hub = await loadHub();
        hub.attach(runtime({ resolve: () => ({ instanceId: 12, name: 'Toolbar', depth: 1 }) }), { platform: 'Server' });

        const findings = await hub.auditPage();
        const rules = findings.map(f => f.rule + ' ' + f.element).sort();
        expect(rules).toEqual([
            'control-name <button>',
            'duplicate-id <input>',
            'field-label <input>',
            'heading-empty <h2>',
            'hidden-focusable <button>',
            'image-alt <img>',
            'page-lang <html>',
            'tabindex-positive <div>',
        ]);
        expect(findings.find(f => f.rule === 'control-name')).toMatchObject({ component: 'Toolbar', instanceId: 12, wcag: '4.1.2' });
        expect(findings.find(f => f.rule === 'field-label').message).toMatch(/placeholder is not one/);
        expect(findings.find(f => f.rule === 'image-alt').component).toBeUndefined();

        // The panel's own unnamed button is BevTools' business: included only in the self-check.
        document.querySelector('.bev-root button').textContent = '';
        expect((await hub.auditPage()).length).toBe(8);
        expect((await hub.auditPage({ includeBevTools: true })).length).toBe(9);

        expect(hub.highlightFinding(0, 'image-alt')).toBe(false);
        expect(hub.highlightFinding(1, 'control-name')).toBe(true);
        document.documentElement.setAttribute('lang', 'en');
    });

    it('reads names as a screen reader does and reports only what a keyboard reaches', async () => {
        document.body.innerHTML = '';
        document.documentElement.setAttribute('lang', 'en');
        document.body.insertAdjacentHTML('beforeend', `
            <button id="glyph"><span aria-hidden="true">✕</span></button>
            <h2 id="star"><span aria-hidden="true">★</span></h2>
            <button id="hiddentext"><span style="display:none">Save</span></button>
            <img id="logo" src="l.png" aria-label="Company logo">
            <span id="chart-name">Chart of sales</span><img id="chart" src="c.png" aria-labelledby="chart-name">
            <span id="named" aria-label="Delete order"></span><button id="by-reference" aria-labelledby="named"></button>
            <custom-button id="shadowed"></custom-button>
            <div aria-hidden="true"><button tabindex="-1">Remedied</button></div>
            <div aria-hidden="true" style="display:none"><a href="/a">Off canvas</a></div>
            <div aria-hidden="true" inert><a href="/b">Inert</a></div>
            <div aria-hidden="true"><a id="reachable" href="/c">Reachable</a></div>
            <div tabindex="0">natural order</div>
            <span id="unique">x</span><input aria-labelledby="unique">`);
        // A custom element whose text is in its shadow root, as many design systems' buttons are.
        const shadow = document.getElementById('shadowed').attachShadow({ mode: 'open' });
        shadow.innerHTML = '<button>Save</button>';
        document.getElementById('shadowed').setAttribute('role', 'button');
        const hub = await loadHub();

        const findings = (await hub.auditPage()).map(f => f.rule + ' ' + f.element).sort();

        expect(findings).toEqual([
            'control-name <button#glyph>',
            'control-name <button#hiddentext>',
            'heading-empty <h2#star>',
            'hidden-focusable <a#reachable>',
        ]);
    });
});
