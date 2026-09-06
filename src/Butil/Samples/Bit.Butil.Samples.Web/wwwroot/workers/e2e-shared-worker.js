// The shared worker the /e2e harness drives. Each connecting page arrives as a 'connect' event
// carrying its own port; the connection count is worker state, which is the thing worth asserting.
// A shared worker outlives the page that started it, so a port whose page has gone has to leave the
// list too - otherwise the count is a tally of every page that ever connected.

const ports = [];

self.addEventListener('connect', e => {
    const port = e.ports[0];
    ports.push(port);

    const forget = () => {
        const index = ports.indexOf(port);
        if (index >= 0) ports.splice(index, 1);
    };

    // 'close' fires when the other end is closed or collected, where the engine has it; engines
    // that do not are covered by the explicit disconnect below, which the client sends before it
    // closes its own port. Both, because neither alone covers every engine.
    port.addEventListener('close', forget);

    port.addEventListener('message', m => {
        const data = m.data;
        if (data && data.op === 'disconnect') { forget(); return; }
        if (data && data.op === 'count') { port.postMessage({ op: 'count', connections: ports.length }); return; }
        port.postMessage({ op: 'echo', payload: data && data.payload });
    });

    port.start();
});
