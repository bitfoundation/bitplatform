// Tests for the browser extension's source (Bit.BevTools.Extension/devtools-source.js): requests reach the page hub
// through inspectedWindow.eval, answers come back through the page mailbox, and whatever a request carries stays data.
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, it, expect } from 'vitest';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const SOURCE = path.resolve(HERE, '../../Bit.BevTools.Extension/devtools-source.js');

/** What inspectedWindow.eval does: evaluates in the page's global scope and hands back a JSON-serializable result. */
function evalInPage(expression) {
    try {
        const result = (0, eval)(expression);
        return Promise.resolve(result === undefined ? undefined : JSON.parse(JSON.stringify(result)));
    } catch (err) {
        return Promise.reject(err);
    }
}

function installHub(answer) {
    const requests = [];
    globalThis.window = globalThis;
    globalThis.__BIT_BEVTOOLS__ = {
        protocol: 1,
        targets: () => [{ id: 't1', platform: 'Server', sessionId: 3, label: 'Server circuit' }],
        traffic: () => [{ id: 1, kind: 'blazor-circuit' }],
        request: (targetId, method, params) => {
            requests.push([targetId, method, params]);
            return answer instanceof Error ? Promise.reject(answer) : Promise.resolve(answer);
        },
    };
    return requests;
}

describe('the extension source', () => {
    it('lists targets and traffic, and answers requests through the page mailbox', async () => {
        const requests = installHub({ protocol: 1, sessionId: 3 });
        const { createDevToolsSource } = await import(SOURCE);
        const source = createDevToolsSource(evalInPage, () => Promise.resolve());

        expect(await source.targets()).toEqual([{ id: 't1', platform: 'Server', sessionId: 3, label: 'Server circuit' }]);
        expect(await source.traffic()).toEqual([{ id: 1, kind: 'blazor-circuit' }]);
        expect(await source.request('t1', 'snapshot', { since: 4, sections: ['timeline'] })).toEqual({ protocol: 1, sessionId: 3 });
        expect(requests).toEqual([['t1', 'snapshot', { since: 4, sections: ['timeline'] }]]);
        expect(Object.keys(globalThis.__BIT_BEVTOOLS_DEVTOOLS__), 'the mailbox is emptied').toHaveLength(0);
    });

    it('keeps what a request carries as data, however it is quoted', async () => {
        const requests = installHub({ ok: true });
        globalThis.injected = false;
        const { createDevToolsSource } = await import(SOURCE);
        const source = createDevToolsSource(evalInPage, () => Promise.resolve());
        const hostile = "t1'); globalThis.injected = true; ('\"); globalThis.injected = true; (\"";

        await source.request(hostile, 'inspect', { name: "</script>'\"`${globalThis.injected = true}`" });

        expect(globalThis.injected).toBe(false);
        expect(requests[0][0]).toBe(hostile);
        expect(requests[0][2].name).toBe("</script>'\"`${globalThis.injected = true}`");
    });

    it('turns a rejected request and a page without BevTools into errors', async () => {
        installHub(new Error('No such component.'));
        const { createDevToolsSource } = await import(SOURCE);
        const source = createDevToolsSource(evalInPage, () => Promise.resolve());

        await expect(source.request('t1', 'inspect', { instanceId: 1 })).rejects.toThrow('No such component.');
        delete globalThis.__BIT_BEVTOOLS__;
        expect(await source.targets()).toEqual([]);
        await expect(source.request('t1', 'hello')).rejects.toThrow('No BevTools runtime on this page');
    });
});

describe('the extension source in the page', () => {
    it('picks without telling the in-app panel, outlines through the mailbox, and resolves the Elements selection', async () => {
        installHub({});
        const calls = [];
        Object.assign(globalThis.__BIT_BEVTOOLS__, {
            startPick: (target, options) => { calls.push(['startPick', target, options]); return true; },
            stopPick: () => calls.push(['stopPick']),
            isPicking: () => false,
            lastPick: () => ({ targetId: 't1', instanceId: 7, at: 1 }),
            clearHighlight: () => calls.push(['clearHighlight']),
            highlightComponent: (target, id, options) => Promise.resolve({ ok: true, visible: true, target, id, options }),
            componentAt: (node, includeHidden) => Promise.resolve({ targetId: 't1', instanceId: 9, node: node.id, includeHidden }),
        });
        globalThis.$0 = { id: 'selected' };
        const { createDevToolsSource } = await import(SOURCE);
        const { page } = createDevToolsSource(evalInPage, () => Promise.resolve());

        expect(await page.startPick(true)).toBe(true);
        expect(calls[0]).toEqual(['startPick', null, { includeHidden: true, tellPanel: false }]);
        expect(await page.lastPick()).toEqual({ targetId: 't1', instanceId: 7, at: 1 });
        expect(await page.highlight("t1'); globalThis.injected = true; ('", 7, true)).toMatchObject({ ok: true, target: "t1'); globalThis.injected = true; ('", id: 7, options: { scroll: true, durationMs: 2500 } });
        expect(await page.inspectedElement(false)).toEqual({ targetId: 't1', instanceId: 9, node: 'selected', includeHidden: false });
        await page.clearHighlight();
        expect(calls).toContainEqual(['clearHighlight']);
        expect(globalThis.injected).not.toBe(true);
    });

    it('reports picking as unavailable on a hub that predates it', async () => {
        installHub({});
        const { createDevToolsSource } = await import(SOURCE);
        const { page } = createDevToolsSource(evalInPage, () => Promise.resolve());
        expect(await page.startPick(false)).toBe(false);
        expect(await page.lastPick()).toBeNull();
    });
});
