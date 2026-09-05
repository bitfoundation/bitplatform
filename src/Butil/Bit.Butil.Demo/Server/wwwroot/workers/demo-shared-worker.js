// The shared worker behind the Worker page. One instance serves every page of this origin that
// names this script and the same worker name; each of them arrives here as a 'connect' event
// carrying its own port.
//
// Open the page in a second tab to see it: the connection count is worker state, so the second tab
// is told it is number two.

const ports = [];

self.addEventListener('connect', e => {
    const port = e.ports[0];
    ports.push(port);

    port.addEventListener('message', m => {
        const data = m.data;

        // A shared worker is never told that a page went away, so a page that is leaving says so.
        // Without this the list keeps ports nobody is holding and every count is too high.
        if (data && data.op === 'disconnect') {
            const index = ports.indexOf(port);
            if (index >= 0) ports.splice(index, 1);
            return;
        }

        if (data && data.op === 'count') {
            port.postMessage({ op: 'count', connections: ports.length });
            return;
        }

        if (data && data.op === 'broadcast') {
            // What a shared worker is for: state and delivery across tabs, with no server involved.
            for (const other of ports) other.postMessage({ op: 'broadcast', payload: data.payload });
            return;
        }

        port.postMessage({ op: 'echo', payload: data, connections: ports.length });
    });

    port.start();
    port.postMessage({ op: 'welcome', connections: ports.length });
});
