// Bridge between Blex and the Redux DevTools browser extension.
// Implements the documented extension integration API:
//   window.__REDUX_DEVTOOLS_EXTENSION__.connect(options) -> { init, send, subscribe }
// Time-travel is driven by the extension sending DISPATCH messages back to us.
//
// ES modules are cached per URL, so every importer shares this module instance. Connections are
// therefore keyed by a handle rather than held in a single slot: a provider that is torn down and
// re-created (render-mode switch, enhanced navigation) would otherwise have the outgoing instance's
// disconnect() tear down the incoming instance's live connection.
const connections = new Map();
let nextHandle = 1;
export function connect(dotNetRef, name) {
    const ext = window.__REDUX_DEVTOOLS_EXTENSION__;
    if (!ext) {
        console.info('[Blex] Redux DevTools extension not detected. Time-travel disabled.');
        return 0;
    }
    const connection = ext.connect({
        name: name || 'Blex',
        // Only advertise what the .NET side actually implements: the extension allows exactly the
        // features listed as true and hides the rest, so a button left false here can never send
        // its message. `skip`/`reorder`/`dispatch`/`test` need the extension's lifted action log,
        // which Blex does not keep - it streams state, it does not replay reducers.
        features: {
            pause: true,
            lock: true,
            export: true,
            import: 'custom',
            jump: true,
            skip: false,
            reorder: false,
            dispatch: false,
            test: false
        }
    });
    // Forward every extension message to .NET, which decides how to time-travel.
    const unsubscribe = connection.subscribe(message => {
        dotNetRef.invokeMethodAsync('HandleMessage', JSON.stringify(message));
    });
    const handle = nextHandle++;
    connections.set(handle, { connection, unsubscribe });
    return handle;
}
export function init(handle, stateJson) {
    const entry = connections.get(handle);
    if (entry)
        entry.connection.init(safeParse(stateJson));
}
export function send(handle, actionType, stateJson, payloadJson) {
    const entry = connections.get(handle);
    if (!entry)
        return;
    const action = { type: actionType };
    if (payloadJson) {
        action.payload = safeParse(payloadJson);
    }
    entry.connection.send(action, safeParse(stateJson));
}
export function disconnect(handle) {
    const entry = connections.get(handle);
    if (!entry)
        return;
    connections.delete(handle);
    // Detach only this connection; ext.disconnect() would kill every DevTools
    // instance on the page (including other libraries').
    if (typeof entry.unsubscribe === 'function') {
        try {
            entry.unsubscribe();
        }
        catch { /* extension already gone */ }
    }
}
function safeParse(json) {
    try {
        return json ? JSON.parse(json) : {};
    }
    catch {
        return {};
    }
}
