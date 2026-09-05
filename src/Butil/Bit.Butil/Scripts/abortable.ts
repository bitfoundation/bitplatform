var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.abortable = {
        registry
    };

    // The cancellation plumbing behind the browser-mediated waits (webOtp, digitalCredentials): one
    // AbortController per pending call, filed under the .NET instance's handle and then under the
    // per-call one, so the public Abort() can end everything an instance started while a cancellation
    // token still only ends the call it belongs to. Aborting the controller is the only way to end the
    // browser's wait early - the promise itself has no cancellation.
    //
    // A token that is already cancelled when the C# method is called runs its registration
    // synchronously, so its abort is dispatched *before* the call it belongs to. An abort that finds
    // nothing pending is therefore remembered against the per-call handle rather than dropped, and the
    // call consumes that mark and declines to start. Per-call handles are never reused, so consuming a
    // mark cannot cancel a later call by mistake.
    //
    // Its own module rather than a corner of utils: every module that dispatches a .NET callback depends
    // on utils, and a lazy-loaded module file inlines its dependencies - so parking this there would put
    // it in ~46 downloads to serve the two that want it.
    function registry() {
        const pending: { [instanceId: string]: { [requestId: string]: AbortController } } = {};
        const preAborted: { [requestId: string]: any } = {};

        // How long a pre-abort mark is kept. The call that consumes it is dispatched immediately after
        // the abort, so this only ever collects marks whose call never reached JS at all (prerender).
        const PRE_ABORT_TTL = 30000;

        return { preAborted: takePreAbort, track, release, abort };

        // Whether an abort for this call arrived before the call did - consuming the mark either way.
        // Ask before anything is awaited, so an early abort ends the call here rather than once the
        // browser's prompt is already on screen.
        function takePreAbort(requestId: string) {
            const timer = preAborted[requestId];
            if (timer === undefined) return false;

            clearTimeout(timer);
            delete preAborted[requestId];
            return true;
        }

        function track(instanceId: string, requestId: string) {
            const controller = new AbortController();
            const instance = pending[instanceId] = pending[instanceId] || {};
            instance[requestId] = controller;
            return controller;
        }

        // Identity-checked, so a call releasing its own controller in `finally` cannot drop one that a
        // later call filed under the same handles.
        function release(instanceId: string, requestId: string, controller: AbortController) {
            const instance = pending[instanceId];
            if (instance?.[requestId] !== controller) return;

            delete instance[requestId];
            if (Object.keys(instance).length === 0) delete pending[instanceId];
        }

        // Without a requestId this ends every call the instance has in flight - what the public Abort()
        // does. With one, only that call - and an abort with nothing yet pending is remembered rather
        // than dropped, since the call it belongs to may not have been dispatched yet.
        function abort(instanceId: string, requestId?: string | null) {
            if (requestId === undefined || requestId === null) {
                const ids = Object.keys(pending[instanceId] || {});
                ids.forEach(id => abortOne(instanceId, id));
                return ids.length > 0;
            }

            if (abortOne(instanceId, requestId)) return true;

            preAborted[requestId] = setTimeout(() => { delete preAborted[requestId]; }, PRE_ABORT_TTL);
            return true;
        }

        function abortOne(instanceId: string, requestId: string) {
            const controller = pending[instanceId]?.[requestId];
            if (!controller) return false;

            release(instanceId, requestId, controller);
            try { controller.abort(); } catch { /* already aborted */ }
            return true;
        }
    }
}(BitButil));
