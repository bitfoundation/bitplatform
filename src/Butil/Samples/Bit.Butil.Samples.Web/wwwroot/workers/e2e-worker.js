// The worker the /e2e harness drives. Deterministic on purpose: every reply is derived from what
// was sent, so an assertion can name the exact answer rather than a shape.

self.addEventListener('message', e => {
    const data = e.data;

    // Binary in, binary out - transferred both ways, so a round trip is provable from the values.
    if (data instanceof ArrayBuffer) {
        const bytes = new Uint8Array(data);
        for (let i = 0; i < bytes.length; i++) bytes[i] = (bytes[i] + 1) & 0xff;
        self.postMessage(bytes.buffer, [bytes.buffer]);
        return;
    }

    const op = data && data.op;

    if (op === 'echo') { self.postMessage({ op: 'echo', payload: data.payload, name: self.name || '' }); return; }

    if (op === 'takePort' && e.ports.length > 0) {
        const port = e.ports[0];
        port.addEventListener('message', m => port.postMessage({ op: 'fromWorker', echoOf: m.data }));
        port.start();
        self.postMessage({ op: 'tookPort' });
        return;
    }

    // Uncaught on purpose: it has to reach the page's onError callback, and the worker has to keep
    // answering afterwards.
    if (op === 'throw') throw new Error('e2e worker threw');

    self.postMessage({ op: 'unknown' });
});
