// One listener pair for "the page may be going away": visibilitychange to hidden and pagehide. Instrumentations
// that report on hide (web vitals) register first-phase callbacks, so their records are queued before the
// last-phase callback (the unload flush) runs.

type Callback = () => void;

const first: Callback[] = [];
const last: Callback[] = [];
let installed = false;

function fire(): void {
    for (const callback of first.concat(last)) {
        try {
            callback();
        } catch {
            // Never throw into the host page.
        }
    }
}

function onVisibilityChange(): void {
    if (document.visibilityState === 'hidden') fire();
}

/** Registers a callback for when the page is hidden or unloaded. Returns the unregister function. */
export function onHidden(callback: Callback, phase: 'first' | 'last' = 'first'): () => void {
    if (!installed && typeof document !== 'undefined') {
        installed = true;
        document.addEventListener('visibilitychange', onVisibilityChange, true);
        addEventListener('pagehide', fire, true);
    }
    const list = phase === 'first' ? first : last;
    list.push(callback);
    return () => {
        const index = list.indexOf(callback);
        if (index >= 0) list.splice(index, 1);
    };
}
