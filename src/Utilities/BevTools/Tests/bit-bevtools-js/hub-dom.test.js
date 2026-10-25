// @vitest-environment happy-dom
// The hub tells BevTools' own render batches from the app's by where their DOM changes landed: found in a real browser,
// where the panel's own rendering over a Blazor Server circuit was reported as the app's large render batches.
import { describe, it, expect, vi } from 'vitest';

async function loadHub() {
    vi.resetModules();
    // Relative, not absolute: under the happy-dom environment Vite resolves a Windows path as a URL.
    return import('../../Bit.BevTools/wwwroot/bit-bevtools.js');
}

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

const str = s => { const b = [...Buffer.from(s, 'utf8')]; return [0xa0 | b.length, ...b]; };
const int = n => (n < 128 ? [n] : [0xcd, (n >> 8) & 255, n & 255]);
const arr = (...items) => [0x90 | items.length, ...items.flat()];
const bin = n => [0xc5, (n >> 8) & 255, n & 255, ...new Array(n).fill(1)];
const invocation = (target, ...args) => arr(int(1), [0x80], [0xc0], str(target), arr(...args));
function frame(message) {
    const out = [];
    let length = message.length;
    do {
        let b = length & 0x7f;
        length >>= 7;
        if (length) b |= 0x80;
        out.push(b);
    } while (length);
    return new Uint8Array([...out, ...message]).buffer;
}

describe('render batches over a circuit', () => {
    it('count a batch that changed only the BevTools panel as BevTools\' own', async () => {
        document.body.innerHTML = '<main id="app"><p class="x">app</p></main><div class="bev-root"><div class="bev-panel"><span>0</span></div></div>';
        window.WebSocket = FakeSocket;
        const hub = await loadHub();
        hub.attach({ _id: 1, invokeMethodAsync: () => Promise.resolve() }, { platform: 'Server' });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');

        // The panel re-renders: batch 7 changes text inside .bev-root, then the browser acknowledges it.
        socket.receive(frame(invocation('JS.RenderBatch', int(7), bin(3000))));
        document.querySelector('.bev-panel span').textContent = '1';
        socket.send(frame(invocation('OnRenderCompleted', int(7), [0xc0])));

        // A page script re-applies an attribute to the value it already has whenever anything renders: that is not a change.
        socket.receive(frame(invocation('JS.RenderBatch', int(6), bin(2000))));
        document.querySelector('.bev-panel span').textContent = '0';
        document.querySelector('#app p').setAttribute('class', 'x');
        socket.send(frame(invocation('OnRenderCompleted', int(6), [0xc0])));

        // The app re-renders: batch 8 changes the app's DOM.
        socket.receive(frame(invocation('JS.RenderBatch', int(8), bin(500))));
        document.querySelector('#app p').textContent = 'changed';
        socket.send(frame(invocation('OnRenderCompleted', int(8), [0xc0])));

        // Both at once: batch 9 counts as the app's.
        socket.receive(frame(invocation('JS.RenderBatch', int(9), bin(700))));
        document.querySelector('#app p').textContent = 'again';
        document.querySelector('.bev-panel span').textContent = '2';
        socket.send(frame(invocation('OnRenderCompleted', int(9), [0xc0])));

        const [stats] = hub.traffic();
        expect(stats.renderBatches.bevTools, 'the panel batch, and the one beside a no-op write').toBe(2);
        expect(stats.renderBatches.count).toBe(2);
        expect(stats.renderBatches.maxBytes, 'the panel\'s 3000-byte batch is not the app\'s largest').toBe(700);
        expect(stats.messages['← JS.RenderBatch'].count).toBe(2);
        expect(stats.own.count, 'the two panel batches and their acknowledgements').toBe(4);
        expect(stats.renderBatches.pending).toBe(0);
        hub.detach();
    });

    it('settles a batch acknowledged before its frame is decoded, the order a real circuit uses', async () => {
        // Found in a real browser: SignalR applies the batch and the browser acknowledges it while the frame is still
        // being dispatched, so the acknowledgement reaches this module before the batch does.
        document.body.innerHTML = '<main id="app"><p class="x">app</p></main><div class="bev-root"><div class="bev-panel"><span>0</span></div></div>';
        window.WebSocket = FakeSocket;
        const hub = await loadHub();
        hub.attach({ _id: 1, invokeMethodAsync: () => Promise.resolve() }, { platform: 'Server' });
        const socket = new window.WebSocket('wss://app.example/_blazor?id=x');

        document.querySelector('#app p').textContent = 'changed';
        socket.send(frame(invocation('OnRenderCompleted', int(3), [0xc0])));
        socket.receive(frame(invocation('JS.RenderBatch', int(3), bin(900))));
        document.querySelector('.bev-panel span').textContent = '1';
        socket.send(frame(invocation('OnRenderCompleted', int(4), [0xc0])));
        socket.receive(frame(invocation('JS.RenderBatch', int(4), bin(4000))));

        const [stats] = hub.traffic();
        expect(stats.renderBatches.count).toBe(1);
        expect(stats.renderBatches.bytes).toBe(900);
        expect(stats.renderBatches.bevTools).toBe(1);
        expect(stats.renderBatches.pending, 'nothing left waiting for an acknowledgement that already came').toBe(0);
        expect(stats.messages['→ OnRenderCompleted'].count).toBe(1);
        hub.detach();
    });
});
