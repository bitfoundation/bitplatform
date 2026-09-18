// The worker behind the samples' Workers page. A worker runs a script you supply - this is that
// script, and there is no .NET inside it.

self.addEventListener('message', e => {
    const data = e.data;

    // Binary in, transferred in with no copy; the reply is a JSON sum object rather than bytes.
    if (data instanceof ArrayBuffer) {
        const bytes = new Uint8Array(data);
        let sum = 0;
        for (let i = 0; i < bytes.length; i++) sum += bytes[i];
        self.postMessage({ op: 'sum', length: bytes.length, sum });
        return;
    }

    const op = data && data.op;

    // The reason workers exist: this blocks its own thread and the page keeps repainting.
    if (op === 'fib') {
        const n = Math.min(Number(data.n) || 0, 45);
        const started = Date.now();
        const fib = k => (k < 2 ? k : fib(k - 1) + fib(k - 2));
        self.postMessage({ op: 'fib', n, value: fib(n), milliseconds: Date.now() - started });
        return;
    }

    self.postMessage({ op: 'echo', payload: data });
});
