// The dedicated worker behind the Worker page. A classic (non-module) script, so it is what the
// wrapper's default options produce.
//
// Bit.Butil does not run .NET in here: a worker runs a script you supply, and the conversation is
// messages. This one answers three of them, chosen to show the three things a worker is for.

self.addEventListener('message', e => {
    const data = e.data;

    // Binary in, binary out. The bytes arrived as a transferred ArrayBuffer - no copy was made -
    // and they go back the same way, which is why the page's array is untouched but the buffer
    // this worker holds is detached after the reply.
    if (data instanceof ArrayBuffer) {
        const bytes = new Uint8Array(data);
        for (let i = 0; i < bytes.length; i++) bytes[i] = (bytes[i] + 1) & 0xff;
        // The buffer is both the message and the thing transferred: posting an object that merely
        // lists the buffer as transferable would send JSON and detach the bytes on the way out.
        self.postMessage(bytes.buffer, [bytes.buffer]);
        return;
    }

    const op = data && data.op;

    if (op === 'echo') {
        self.postMessage({ op: 'echo', payload: data.payload, name: self.name || '' });
        return;
    }

    // The reason workers exist. This blocks its own thread for as long as it takes and the page
    // stays responsive throughout - run the same loop on the main thread and the page freezes.
    if (op === 'fib') {
        const n = Math.min(Number(data.n) || 0, 45);
        const started = performance.now();
        const fib = k => (k < 2 ? k : fib(k - 1) + fib(k - 2));
        const value = fib(n);
        self.postMessage({ op: 'fib', n, value, milliseconds: Math.round(performance.now() - started) });
        return;
    }

    // A port arrived with the message: from here on this worker has a private line to whoever holds
    // the other end, and the page that brokered it is not part of that conversation.
    if (op === 'takePort' && e.ports.length > 0) {
        const port = e.ports[0];
        port.addEventListener('message', m => port.postMessage({ op: 'fromWorker', echoOf: m.data }));
        port.start();
        self.postMessage({ op: 'tookPort' });
        return;
    }

    if (op === 'throw') {
        // Uncaught on purpose: it reaches the page's onError callback, and the worker keeps running
        // afterwards - an error does not terminate a worker.
        throw new Error('the worker was asked to throw');
    }

    self.postMessage({ op: 'unknown', received: data });
});
