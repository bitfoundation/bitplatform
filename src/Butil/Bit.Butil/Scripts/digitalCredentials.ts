var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _pending: { [id: string]: AbortController } = {};

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

    function get(id: string, requests: any[], mediation: string) {
        return exchange('get', id, requests, mediation);
    }

    function create(id: string, requests: any[]) {
        return exchange('create', id, requests, null);
    }

    async function exchange(kind: string, id: string, requests: any[], mediation: string | null) {
        if (!('DigitalCredential' in window) || !navigator.credentials || !requests?.length) return null;

        abort(id);

        const controller = new AbortController();
        _pending[id] = controller;

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
            if (_pending[id] === controller) delete _pending[id];
        }
    }

    function abort(id: string) {
        const controller = _pending[id];
        if (!controller) return false;

        delete _pending[id];
        try { controller.abort(); } catch { /* already aborted */ }
        return true;
    }
}(BitButil));
