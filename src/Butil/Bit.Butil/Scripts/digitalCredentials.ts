var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The controller behind each exchange, filed under the .NET instance's handle and then under the
    // per-call one, so Abort() can reach every exchange an instance started while a cancellation token
    // still only ends the call it belongs to.
    const _pending: { [instanceId: string]: { [requestId: string]: AbortController } } = {};

    // Handles whose abort arrived before the exchange got as far as creating a controller - a token
    // already cancelled when Get/Create was called dispatches its abort first. Keyed by the per-call
    // handle, which is never reused, so consuming one cannot cancel a later call by mistake.
    const _preAborted: { [requestId: string]: any } = {};

    // How long a pre-abort mark is kept. The exchange that consumes it is dispatched immediately after
    // the abort, so this only ever collects marks whose call never reached JS at all (prerender).
    const PRE_ABORT_TTL = 30000;

    butil.digitalCredentials = {
        isSupported() { return 'DigitalCredential' in window; },
        isProtocolSupported,
        get,
        create,
        abort
    };

    function isProtocolSupported(protocol: string) {
        const DigitalCredential = (window as any).DigitalCredential;
        if (typeof DigitalCredential?.userAgentAllowsProtocol !== 'function') return false;

        try { return !!DigitalCredential.userAgentAllowsProtocol(protocol); }
        catch { return false; }
    }

    // The protocol response arrives as a string for some protocols and as an object for others.
    // Parsing what parses gives the .NET side a JSON object either way, and leaves anything else
    // as the string it is rather than losing it.
    function toData(data: any) {
        if (typeof data !== 'string') return data ?? null;
        try { return JSON.parse(data); }
        catch { return data; }
    }

    function get(instanceId: string, requestId: string, requests: any[], mediation: string) {
        return exchange('get', instanceId, requestId, requests, mediation);
    }

    function create(instanceId: string, requestId: string, requests: any[]) {
        return exchange('create', instanceId, requestId, requests, null);
    }

    async function exchange(kind: string, instanceId: string, requestId: string, requests: any[], mediation: string | null) {
        if (!('DigitalCredential' in window) || !navigator.credentials || !requests?.length) {
            takePreAbort(requestId);
            return null;
        }

        // Claimed before the controller exists, with nothing awaited in between: an abort that arrived
        // first ends the call here rather than once the wallet chooser is already on screen.
        if (takePreAbort(requestId)) return null;

        const controller = new AbortController();
        const instance = _pending[instanceId] = _pending[instanceId] || {};
        instance[requestId] = controller;

        const digital = { requests: requests.map(request => ({ protocol: request.protocol, data: request.data })) };
        const options: any = { digital, signal: controller.signal };
        if (mediation) options.mediation = mediation;

        try {
            const credential: any = kind === 'create'
                ? await navigator.credentials.create(options)
                : await navigator.credentials.get(options);

            if (!credential) return null;

            return { protocol: credential.protocol ?? '', data: toData(credential.data) };
        } catch {
            // Declined, no wallet held a match, or the protocol is not one this browser speaks.
            return null;
        } finally {
            release(instanceId, requestId, controller);
        }
    }

    // Without a requestId this aborts every exchange the instance has in flight - what the public
    // Abort() does. With one, only that call, and an abort with nothing yet pending is remembered
    // rather than dropped, since the exchange it belongs to may not have been dispatched yet.
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
