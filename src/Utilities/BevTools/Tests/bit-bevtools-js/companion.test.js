// @vitest-environment happy-dom
// Tests for the companion (bit-bevtools-companion.js): the frontend that shows a page's BevTools runtimes from outside
// the page, and exported sessions offline. Run against the built wwwroot file, like the bridge tests.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, it, expect, vi, afterEach } from 'vitest';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const BUNDLE = path.resolve(HERE, '../../Bit.BevTools/wwwroot/bit-bevtools-companion.js');

async function loadCompanion() {
    if (!fs.existsSync(BUNDLE)) {
        throw new Error(`Missing ${BUNDLE}.\nBuild the package first: dotnet build ../../Bit.BevTools/Bit.BevTools.csproj`);
    }
    vi.resetModules();
    // Relative, not the absolute path: under the happy-dom environment Vite resolves a Windows path as a URL.
    return import('../../Bit.BevTools/wwwroot/bit-bevtools-companion.js');
}

const flush = () => new Promise(resolve => setTimeout(resolve, 0));

function session(id, platform, components) {
    return {
        format: 'bit-bevtools/1',
        session: { id, platform, valueAccess: 'Full', capturing: true, startedAt: new Date().toISOString() },
        overhead: { totalMs: 1.5 },
        components: { live: components.length, items: components },
        timeline: [{ id: 1, timestamp: new Date().toISOString(), kind: 'UiEvent', category: 'ui-event', title: 'onclick → Counter.Increment', severity: 'Info' }],
        errors: [],
        http: [],
        diagnostics: [],
        routing: { current: { uri: '/' }, routes: [], routesSource: 'From the Router.' },
        runtime: [{ title: 'Rendering', scope: 'this session', facts: [{ name: 'Renderer', value: platform, severity: 'Info' }] }],
    };
}

function liveSource(docs) {
    const requests = [];
    return {
        requests,
        label: 'test page',
        live: true,
        async targets() {
            return Object.keys(docs).map(id => ({ id, sessionId: docs[id].session.id, platform: docs[id].session.platform, renderMode: null, label: docs[id].session.platform }));
        },
        async request(targetId, method, params) {
            requests.push([targetId, method, params]);
            switch (method) {
                case 'hello': return { protocol: 1 };
                case 'snapshot': return { version: 1, session: docs[targetId] };
                case 'inspect': return {
                    instanceId: params.instanceId, displayName: 'Counter', parameters: [{ name: 'Settings', type: 'Settings', display: 'Settings', kind: 'Object', path: '', expandable: true }],
                    cascading: [], state: [{ name: 'count', type: 'Int32', display: '3', kind: 'Primitive', path: '', expandable: false }],
                    services: [], causes: { UiEvent: 2 }, recentRenders: [], wiring: { total: 1, bindings: [{ target: '<button>', event: 'onclick', handler: 'Counter.Increment', kind: 'DomEvent' }] },
                };
                case 'expand': return { children: [{ name: 'Host', type: 'string', display: '"smtp"', kind: 'String', path: 'Host', expandable: false }] };
                case 'command': return { done: 'ok', version: 2 };
                default: return { error: 'unknown' };
            }
        },
        async traffic() { return []; },
    };
}

const counter = { instanceId: 7, type: 'App.Counter', displayName: 'Counter', renders: 3, totalRenderMs: 2, maxRenderMs: 1, lastRenderMs: 0.5, createdAt: new Date().toISOString() };

afterEach(() => {
    document.body.replaceChildren();
});

describe('companion', () => {
    it('shows every runtime of the page and switches between them', async () => {
        const { Companion } = await loadCompanion();
        const source = liveSource({ t1: session(3, 'Server', [counter]), t2: session(1, 'WebAssembly', []) });
        const companion = new Companion(source, document.body);

        await companion.start();
        await flush();

        const runtimes = [...document.querySelectorAll('.bc-runtime')].map(b => b.textContent);
        expect(runtimes).toEqual(['Server #3', 'WebAssembly #1']);
        expect(document.querySelector('.bc-status').textContent).toContain('Server');
        document.querySelectorAll('.bc-runtime')[1].click();
        await flush();
        await flush();
        expect(source.requests.some(r => r[0] === 't2' && r[1] === 'snapshot')).toBe(true);
        companion.stop();
    });

    it('inspects a component live and expands its values on demand', async () => {
        const { Companion } = await loadCompanion();
        const source = liveSource({ t1: session(3, 'Server', [counter]) });
        const companion = new Companion(source, document.body);
        await companion.start();
        await flush();

        [...document.querySelectorAll('.bc-tab')].find(t => t.textContent.startsWith('Components')).click();
        document.querySelector('.bc-tree-row').click();
        await flush();
        await flush();

        expect(source.requests).toContainEqual(['t1', 'inspect', { instanceId: 7 }]);
        expect(document.body.textContent).toContain('Counter.Increment');
        const caret = [...document.querySelectorAll('.bc-vt-row .bc-caret')].find(c => c.textContent === '▸');
        caret.click();
        await flush();
        await flush();
        expect(source.requests).toContainEqual(['t1', 'expand', { instanceId: 7, section: 'parameters', name: 'Settings', path: '' }]);
        expect(document.body.textContent).toContain('"smtp"');
        companion.stop();
    });

    it('renders application strings as text, never as markup', async () => {
        const { Companion } = await loadCompanion();
        const hostile = { ...counter, displayName: '<img src=x onerror=alert(1)>' };
        const companion = new Companion(liveSource({ t1: session(3, 'Server', [hostile]) }), document.body);
        await companion.start();
        await flush();

        [...document.querySelectorAll('.bc-tab')].find(t => t.textContent.startsWith('Components')).click();

        expect(document.querySelector('img')).toBeNull();
        expect(document.body.textContent).toContain('<img src=x onerror=alert(1)>');
        companion.stop();
    });

    it('sends the header commands to the runtime shown', async () => {
        const { Companion } = await loadCompanion();
        const source = liveSource({ t1: session(3, 'Server', [counter]) });
        const companion = new Companion(source, document.body);
        await companion.start();
        await flush();

        [...document.querySelectorAll('button')].find(b => b.textContent.includes('Pause')).click();
        await flush();

        expect(source.requests).toContainEqual(['t1', 'command', { name: 'pause', argument: undefined }]);
        companion.stop();
    });

    it('opens an exported session offline, and says what a live page would add', async () => {
        const { Companion, FileSource } = await loadCompanion();
        const companion = new Companion(new FileSource(session(9, 'WebAssembly', [counter]), 'export.json'), document.body);
        await companion.start();
        await flush();

        expect(document.querySelector('.bc-status').textContent).toContain('export.json');
        [...document.querySelectorAll('.bc-tab')].find(t => t.textContent.startsWith('Components')).click();
        document.querySelector('.bc-tree-row').click();
        await flush();

        expect(document.body.textContent).toContain('An exported session carries render statistics, not values');
        expect([...document.querySelectorAll('button')].some(b => b.textContent.includes('Pause')), 'no commands on a file').toBe(false);
        companion.stop();
    });
});

describe('components in the page', () => {
    function withPage(source) {
        const calls = [];
        let picked = null;
        let picking = false;
        source.page = {
            calls,
            async startPick(includeHidden) { calls.push(['startPick', includeHidden]); picking = true; return true; },
            async stopPick() { calls.push(['stopPick']); picking = false; },
            async isPicking() { return picking; },
            async lastPick() { return picked; },
            async highlight(targetId, instanceId, scroll) { calls.push(['highlight', targetId, instanceId, scroll]); return { ok: true, visible: true }; },
            async clearHighlight() { calls.push(['clearHighlight']); },
            async inspectedElement() { return { targetId: 't2', instanceId: 7, name: 'Counter', at: Date.now() }; },
            pick(result) { picked = { ...result, at: Date.now() + 1 }; picking = false; },
        };
        return source.page;
    }

    it('picks in the page and inspects what was picked, on the runtime that rendered it', async () => {
        vi.useFakeTimers({ shouldAdvanceTime: true });
        try {
            const { Companion } = await loadCompanion();
            const source = liveSource({ t1: session(3, 'Server', [counter]), t2: session(1, 'WebAssembly', [counter]) });
            const page = withPage(source);
            const companion = new Companion(source, document.body);
            await companion.start();
            await flush();
            [...document.querySelectorAll('.bc-tab')].find(b => b.textContent.startsWith('Components')).click();
            await flush();

            const pick = [...document.querySelectorAll('.bc-toolbar button')].find(b => b.textContent.includes('Pick'));
            expect(pick, 'a live page offers picking').toBeTruthy();
            pick.click();
            await flush();
            expect(page.calls[0]).toEqual(['startPick', false]);

            page.pick({ targetId: 't2', instanceId: 7, name: 'Counter' });
            await vi.advanceTimersByTimeAsync(300);
            await flush();
            await flush();
            expect(source.requests).toContainEqual(['t2', 'inspect', { instanceId: 7 }]);
            expect(document.querySelector('.bc-runtime.bc-active').textContent).toContain('WebAssembly');
            companion.stop();
        } finally {
            vi.useRealTimers();
        }
    });

    it('outlines a hovered component, offers Show in page, and follows the Elements panel', async () => {
        const { Companion } = await loadCompanion();
        const source = liveSource({ t1: session(3, 'Server', [counter]), t2: session(1, 'WebAssembly', [counter]) });
        const page = withPage(source);
        const companion = new Companion(source, document.body);
        await companion.start();
        await flush();
        [...document.querySelectorAll('.bc-tab')].find(b => b.textContent.startsWith('Components')).click();
        await flush();

        const row = document.querySelector('.bc-tree-row');
        row.dispatchEvent(new MouseEvent('mouseenter'));
        await flush();
        expect(page.calls).toContainEqual(['highlight', 't1', 7, false]);
        document.querySelector('.bc-left').dispatchEvent(new MouseEvent('mouseleave'));
        await flush();
        expect(page.calls).toContainEqual(['clearHighlight']);

        row.click();
        await flush();
        await flush();
        [...document.querySelectorAll('.bc-inspector-head button')].find(b => b.textContent.includes('Show in page')).click();
        await flush();
        expect(page.calls).toContainEqual(['highlight', 't1', 7, true]);

        await companion.revealInspectedElement();
        await flush();
        expect(source.requests).toContainEqual(['t2', 'inspect', { instanceId: 7 }]);
        companion.stop();
    });

    it('offers no picking for an exported file', async () => {
        const { Companion, FileSource } = await loadCompanion();
        const companion = new Companion(new FileSource(session(3, 'Server', [counter]), 'x.json'), document.body);
        await companion.start();
        await flush();
        [...document.querySelectorAll('.bc-tab')].find(b => b.textContent.startsWith('Components')).click();
        await flush();
        expect([...document.querySelectorAll('.bc-toolbar button')].some(b => b.textContent.includes('Pick'))).toBe(false);
        companion.stop();
    });
});

describe('source locations', () => {
    it('links a component and its handlers to the editor, and never makes a script URL a link', async () => {
        const { Companion } = await loadCompanion();
        const source = liveSource({ t1: session(3, 'Server', [counter]) });
        const inspect = source.request.bind(source);
        source.request = async (targetId, method, params) => {
            const answer = await inspect(targetId, method, params);
            if (method !== 'inspect') return answer;
            return {
                ...answer,
                sources: [{ path: 'D:/app/Counter.razor', file: 'Counter.razor', line: 3, column: 1, url: 'vscode://file/D:/app/Counter.razor:3:1' }],
                wiring: { total: 1, bindings: [{ target: '<button>', event: 'onclick', handler: 'Counter.Increment', kind: 'DomEvent', handlerSource: [{ path: 'D:/app/Counter.razor', file: 'Counter.razor', line: 20, column: 5, url: 'javascript:alert(1)' }] }] },
            };
        };
        const companion = new Companion(source, document.body);
        await companion.start();
        await flush();
        [...document.querySelectorAll('.bc-tab')].find(b => b.textContent.startsWith('Components')).click();
        await flush();
        document.querySelector('.bc-tree-row').click();
        await flush();
        await flush();

        const link = [...document.querySelectorAll('a.bc-mono')].find(a => a.textContent === 'Counter.razor:3');
        expect(link.getAttribute('href')).toBe('vscode://file/D:/app/Counter.razor:3:1');
        const handler = [...document.querySelectorAll('.bc-mono')].find(e => e.textContent === 'Counter.razor:20');
        expect(handler.tagName, 'a javascript: URL is shown as text').toBe('SPAN');
        companion.stop();
    });
});
