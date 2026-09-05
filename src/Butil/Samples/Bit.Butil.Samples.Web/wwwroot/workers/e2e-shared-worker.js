// The shared worker the /e2e harness drives. Each connecting page arrives as a 'connect' event
// carrying its own port; the connection count is worker state, which is the thing worth asserting.

const ports = [];

self.addEventListener('connect', e => {
    const port = e.ports[0];
    ports.push(port);

    port.addEventListener('message', m => {
        const data = m.data;
        if (data && data.op === 'count') { port.postMessage({ op: 'count', connections: ports.length }); return; }
        port.postMessage({ op: 'echo', payload: data && data.payload });
    });

    port.start();
});
