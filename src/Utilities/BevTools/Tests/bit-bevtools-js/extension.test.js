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
