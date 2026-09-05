var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The controller behind each pending wait, filed under the .NET instance's handle and then under
    // the per-call one. Aborting it is the only way to end the browser's wait early - the promise
    // itself has no cancellation.
    const _pending: { [instanceId: string]: { [requestId: string]: AbortController } } = {};

    // Handles whose abort arrived before receive got as far as creating a controller - a token already
    // cancelled when Receive was called dispatches its abort first. Keyed by the per-call handle, which
    // is never reused, so consuming one cannot cancel a later wait by mistake.
    const _preAborted: { [requestId: string]: any } = {};

    // How long a pre-abort mark is kept. The receive that consumes it is dispatched immediately after
    // the abort, so this only ever collects marks whose call never reached JS at all (prerender).
    const PRE_ABORT_TTL = 30000;

    butil.webOtp = {
        isSupported() { return 'OTPCredential' in window; },
        receive,
        abort
    };

    async function receive(instanceId: string, requestId: string, timeoutMs: number | null) {
        if (!('OTPCredential' in window) || !navigator.credentials) {
            takePreAbort(requestId);
            return null;
        }

        // Defensive: a second receive on the same instance would otherwise leave the first controller
        // unreachable, and its browser prompt with it.
        abort(instanceId);

        // Claimed before the controller exists, with nothing awaited in between: an abort that arrived
        // first ends the wait here rather than after the browser's prompt is already up.
        if (takePreAbort(requestId)) return null;

        const controller = new AbortController();
        const instance = _pending[instanceId] = _pending[instanceId] || {};
        instance[requestId] = controller;

        const timer = (timeoutMs && timeoutMs > 0)
            ? setTimeout(() => { try { controller.abort(); } catch { } }, timeoutMs)
            : null;

        try {
            const credential: any = await navigator.credentials.get({
                otp: { transport: ['sms'] },
                signal: controller.signal
            } as any);

            return credential?.code ?? null;
        } catch {
            // Aborted, timed out, or the user dismissed the browser's prompt.
            return null;
        } finally {
            if (timer !== null) clearTimeout(timer);
            release(instanceId, requestId, controller);
        }
    }

    // Without a requestId this ends every wait the instance has in flight - what the public Abort()
    // does. With one, only that wait, and an abort with nothing yet pending is remembered rather than
    // dropped, since the receive it belongs to may not have been dispatched yet.
    function abort(instanceId: string, requestId?: string | null) {
        if (requestId === undefined || requestId === null) {
            const ids = Object.keys(_pending[instanceId] || {});
            ids.forEach(id => abortOne(instanceId, id));
            return ids.length > 0;
        }

        if (abortOne(instanceId, requestId)) return true;

        _preAborted[requestId] = setTimeout(() => { delete _preAborted[requestId]; }, PRE_ABORT_TTL);
        return true;
    }

    function abortOne(instanceId: string, requestId: string) {
        const controller = _pending[instanceId]?.[requestId];
        if (!controller) return false;

        release(instanceId, requestId, controller);
        try { controller.abort(); } catch { /* already aborted */ }
        return true;
    }

    function release(instanceId: string, requestId: string, controller: AbortController) {
        const instance = _pending[instanceId];
        if (instance?.[requestId] !== controller) return;

        delete instance[requestId];
        if (Object.keys(instance).length === 0) delete _pending[instanceId];
    }

    function takePreAbort(requestId: string) {
        const timer = _preAborted[requestId];
        if (timer === undefined) return false;

        clearTimeout(timer);
        delete _preAborted[requestId];
        return true;
    }
}(BitButil));
