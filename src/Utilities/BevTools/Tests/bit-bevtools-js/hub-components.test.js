// @vitest-environment happy-dom
// Components in the page: the hub walks the logical element tree Blazor's browser renderer keeps on DOM nodes. The
// page here is built the way blazor.web.js builds it - Symbol() keys without descriptions for the logical children and
// the logical parent, a `true` marker on interactive root containers, component containers as comment nodes - so the
// tests exercise the key learning and path verification against the same shape, without the framework.
import { describe, it, expect, vi, beforeEach } from 'vitest';

async function loadHub() {
    vi.resetModules();
    // The hook is defined once per window; a fresh module must expose its own.
    delete window.__BIT_BEVTOOLS__;
    return import('../../Bit.BevTools/wwwroot/bit-bevtools.js');
}

const CHILDREN = Symbol();
const PARENT = Symbol();
const ROOT = Symbol();
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

/**
 * <div id="app">           root container (marked)
 *   <main>                 child 0
 *   <!--!-->               child 1: the List component
 *     <ul><li>a</li><li>b</li></ul>
 *   <!--!-->               child 2: the BevTools panel
 *     <div class="bev-root" data-bev-target="t1">
 * <div id="island">        a second root, which no panel renders in
 *   <p>
 */
function buildPage() {
    document.body.innerHTML = '';
    const app = document.createElement('div');
    app.id = 'app';
    document.body.appendChild(app);
    logical(app);
    app[ROOT] = true;
    app[PRESERVE] = true;

    const main = logical(document.createElement('main'), app);
    const list = logical(document.createComment('!'), app);
    const ul = logical(document.createElement('ul'), list);
    const items = ['a', 'b'].map(text => {
        const li = logical(document.createElement('li'), ul);
        li.textContent = text;
        ul.appendChild(li);
        return li;
    });
    const panelContainer = logical(document.createComment('!'), app);
    const panel = logical(document.createElement('div'), panelContainer);
    panel.className = 'bev-root';
    panel.setAttribute('data-bev-target', 't1');
    const button = document.createElement('button');
    panel.appendChild(button);
    app.append(main, list, ul, panelContainer, panel);

    const island = document.createElement('div');
    island.id = 'island';
    document.body.appendChild(island);
    logical(island);
    island[ROOT] = true;
    const p = logical(document.createElement('p'), island);
    island.appendChild(p);

    rect(ul, 10, 40, 200, 50);
    rect(p, 0, 300, 100, 20);
    return { app, main, list, ul, items, panel, button, island, p };
}

/** A runtime reference that answers the protocol like BevToolsProtocol would, and records what the hub tells it. */
function runtime(answers) {
    const calls = [];
    return {
        calls,
        _id: 1,
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

const listLocation = { instanceId: 7, name: 'List', anchored: true, rootSignature: '3|main,#c,#c', rootOrdinal: 0, path: [{ index: 1, kind: '#c' }] };

describe('highlighting a component', () => {
    let page;
    beforeEach(() => { page = buildPage(); });

    it('finds the anchored root from the panel and outlines the component', async () => {
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'WebAssembly' });

        const result = hub.highlight('t1', listLocation, { durationMs: 0 });
        expect(result).toEqual({ ok: true, visible: true });
        const box = document.querySelector('.bev-overlay .bev-hl');
        expect(box.style.left).toBe('10px');
        expect(box.style.width).toBe('200px');
        expect(box.textContent).toBe('List · 200×50');
        expect(box.closest('.bev-root'), 'the overlay counts as BevTools\' own DOM').not.toBeNull();

        hub.clearHighlight();
        expect(document.querySelector('.bev-overlay .bev-hl')).toBeNull();
    });

    it('reports a path the page does not match instead of outlining something else', async () => {
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'WebAssembly' });

        const stale = hub.highlight('t1', { ...listLocation, path: [{ index: 1, kind: 'section' }] });
        expect(stale.ok).toBe(false);
        expect(stale.error).toMatch(/does not match/);
        const missing = hub.highlight('t1', { ...listLocation, path: [{ index: 7, kind: '#c' }] });
        expect(missing.ok).toBe(false);
        expect(document.querySelector('.bev-overlay .bev-hl')).toBeNull();
    });

    it('finds a root no panel renders in by its signature', async () => {
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'WebAssembly' });

        const result = hub.highlight('t1', { instanceId: 9, name: 'Island', anchored: false, rootSignature: '1|p', rootOrdinal: 0, path: [] });
        expect(result.ok).toBe(true);
        expect(document.querySelector('.bev-overlay .bev-hl').style.top).toBe('300px');
    });

    it('asks the runtime where a component is for frontends outside the panel', async () => {
        const hub = await loadHub();
        const ref = runtime({ locate: args => (args.instanceId === 7 ? listLocation : { error: 'No such component.' }) });
        hub.attach(ref, { platform: 'WebAssembly' });

        expect(await window.__BIT_BEVTOOLS__.highlightComponent('t1', 7)).toEqual({ ok: true, visible: true });
        expect(await window.__BIT_BEVTOOLS__.highlightComponent('t1', 99)).toEqual({ ok: false, error: 'No such component.' });
    });

    it('says when the page has no logical tree to read', async () => {
        document.body.innerHTML = '<div class="bev-root" data-bev-target="t1"></div>';
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'WebAssembly' });

        expect(hub.highlight('t1', listLocation).error).toMatch(/logical element tree/);
        expect(hub.startPick('t1')).toBe(false);
        expect(hub.lastPick().error).toMatch(/logical element tree/);
    });
});

describe('picking a component in the page', () => {
    let page;
    beforeEach(() => { page = buildPage(); });

    const resolveAnswer = args => {
        // The path from the root container to the clicked <li>: the List container, <ul>, then the <li>.
        expect(args).toMatchObject({ anchored: true, path: [{ index: 1, kind: '#c' }, { index: 0, kind: 'ul' }, { index: 1, kind: 'li' }] });
        return { instanceId: 7, name: 'List', depth: 1, renderedBy: null, element: '<li>', inMarkup: false, ambiguous: false };
    };

    it('selects what a click lands on, and the click never reaches the application', async () => {
        const hub = await loadHub();
        const ref = runtime({ resolve: resolveAnswer });
        hub.attach(ref, { platform: 'WebAssembly' });
        const appClick = vi.fn();
        page.items[1].addEventListener('click', appClick);
        const events = [];
        window.__BIT_BEVTOOLS__.subscribe(e => events.push(e));

        expect(hub.startPick('t1', { tellPanel: true })).toBe(true);
        expect(document.querySelector('.bev-overlay').hasAttribute('data-picking')).toBe(true);
        page.items[1].dispatchEvent(new MouseEvent('mousemove', { bubbles: true }));
        await vi.waitFor(() => expect(document.querySelector('.bev-overlay .bev-hl').textContent).toBe('List · 200×50'));

        page.items[1].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        await vi.waitFor(() => expect(ref.calls.some(c => c[0] === 'OnComponentPicked')).toBe(true));

        expect(appClick).not.toHaveBeenCalled();
        expect(ref.calls.find(c => c[0] === 'OnComponentPicked')).toEqual(['OnComponentPicked', 7]);
        expect(hub.isPicking()).toBe(false);
        expect(hub.lastPick()).toMatchObject({ targetId: 't1', instanceId: 7, name: 'List', element: '<li>' });
        expect(events.some(e => e.type === 'picked' && e.instanceId === 7)).toBe(true);
        expect(document.querySelector('.bev-overlay').hasAttribute('data-picking')).toBe(false);

        // Picking is over: the application's own clicks work again.
        page.items[1].dispatchEvent(new MouseEvent('click', { bubbles: true }));
        expect(appClick).toHaveBeenCalledTimes(1);
    });

    it('does not tell the in-app panel when a frontend outside it picked', async () => {
        const hub = await loadHub();
        const ref = runtime({ resolve: resolveAnswer });
        hub.attach(ref, { platform: 'WebAssembly' });

        window.__BIT_BEVTOOLS__.startPick(null, { tellPanel: false });
        page.items[1].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        await vi.waitFor(() => expect(hub.lastPick()?.instanceId).toBe(7));
        expect(ref.calls.some(c => c[0] === 'OnComponentPicked')).toBe(false);
    });

    it('Escape cancels, and the panel that started it hears so', async () => {
        const hub = await loadHub();
        const ref = runtime({ resolve: resolveAnswer });
        hub.attach(ref, { platform: 'WebAssembly' });

        hub.startPick('t1');
        window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        expect(hub.isPicking()).toBe(false);
        expect(ref.calls).toContainEqual(['OnPickEnded', null]);
    });

    it('a click in BevTools\' own UI ends picking and goes through', async () => {
        const hub = await loadHub();
        hub.attach(runtime({ resolve: resolveAnswer }), { platform: 'WebAssembly' });
        const panelClick = vi.fn();
        page.button.addEventListener('click', panelClick);

        hub.startPick('t1');
        page.button.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        expect(hub.isPicking()).toBe(false);
        expect(panelClick).toHaveBeenCalledTimes(1);
    });

    it('explains static content and failed resolutions instead of selecting anything', async () => {
        const hub = await loadHub();
        const ref = runtime({ resolve: () => ({ error: 'That is BevTools\' own UI.' }) });
        hub.attach(ref, { platform: 'WebAssembly' });
        const outside = document.createElement('aside');
        document.body.appendChild(outside);

        expect((await window.__BIT_BEVTOOLS__.componentAt(outside)).error).toMatch(/did not render/);
        hub.startPick('t1');
        page.items[0].dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true }));
        await vi.waitFor(() => expect(ref.calls).toContainEqual(['OnPickEnded', 'That is BevTools\' own UI.']));
        expect(document.querySelector('.bev-overlay .bev-hl-note').textContent).toBe('That is BevTools\' own UI.');
    });

    it('resolves a node under a root no panel renders in with that root\'s signature', async () => {
        const hub = await loadHub();
        const ref = runtime({ resolve: args => ({ instanceId: 9, name: 'Island', depth: -1, ...args }) });
        hub.attach(ref, { platform: 'WebAssembly' });

        const owner = await window.__BIT_BEVTOOLS__.componentAt(page.p);
        expect(owner).toMatchObject({ targetId: 't1', instanceId: 9, name: 'Island' });
        const request = JSON.parse(ref.calls.find(c => c[0] === 'Protocol' && c[1] === 'resolve')[2]);
        expect(request).toMatchObject({ anchored: false, signature: '1|p', ordinal: 0, path: [{ index: 0, kind: 'p' }] });
    });
});

describe('render flashes and cleanup', () => {
    it('outlines the components that rendered, coloured by count, and removes everything on detach', async () => {
        buildPage();
        const hub = await loadHub();
        hub.attach(runtime({}), { platform: 'WebAssembly' });

        expect(hub.flash('t1', [{ location: listLocation, count: 1 }, { location: listLocation, count: 12 }, { location: { ...listLocation, path: [{ index: 5, kind: '#c' }] }, count: 2 }])).toBe(2);
        const boxes = document.querySelectorAll('.bev-overlay .bev-flash-box');
        expect(boxes.length).toBe(2);
        expect(boxes[0].className).toContain('bev-flash-1');
        expect(boxes[1].className).toContain('bev-flash-4');

        hub.startPick('t1');
        hub.detach('t1');
        expect(document.querySelector('.bev-overlay')).toBeNull();
        expect(hub.isPicking()).toBe(false);
    });
});
