var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One controller per exchange, filed under the .NET instance's handle and the per-call one, so
    // Abort() can reach every exchange an instance started while a cancellation token still only ends
    // the call it belongs to - see butil.abortable.registry.
    const _exchanges = butil.abortable.registry();

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
            _exchanges.preAborted(requestId);
            return null;
        }

        // Asked before the controller exists, with nothing awaited in between: an abort that arrived
        // first ends the call here rather than once the wallet chooser is already on screen.
        if (_exchanges.preAborted(requestId)) return null;

        const controller = _exchanges.track(instanceId, requestId);

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
            _exchanges.release(instanceId, requestId, controller);
        }
    }

    // Without a requestId this aborts every exchange the instance has in flight - what the public
    // Abort() does. With one, only that call.
    function abort(instanceId: string, requestId?: string | null) {
        return _exchanges.abort(instanceId, requestId);
    }
}(BitButil));
