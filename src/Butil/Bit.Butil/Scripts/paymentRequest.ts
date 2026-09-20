var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The sheet outlives the call that opened it: show() resolves while the browser is still
    // showing "processing", and only complete() dismisses it. So both sides are held here, under the
    // .NET instance's handle - the request until it resolves (that is what abort() reaches), and the
    // response until the .NET side completes it. Keying the response by the same handle is what keeps
    // a response nobody completed from living for the rest of the page: the instance's next show()
    // replaces it.
    const _requests: { [id: string]: any } = {};
    const _responses: { [id: string]: any } = {};

    butil.paymentRequest = {
        isSupported() { return 'PaymentRequest' in window; },
        canMakePayment,
        show,
        complete,
        abort
    };

    // The .NET DTOs serialize every unset member as null, and the payment dictionaries reject a
    // null where they would accept an absent key ("null is not a valid enum value" for
    // shippingType, a null displayItems that is not iterable). Dropping them is what makes an
    // optional property optional again.
    // `data` - on a payment method and on a modifier - is the exception: it is the processor's own
    // configuration, which the browser hands to the payment app as written, and a null in there can
    // be one that app reads. It is copied across whole, so only an absent `data` itself is dropped,
    // which is all an unset DTO member means.
    const OPAQUE_KEYS = ['data'];

    function prune(value: any): any {
        if (Array.isArray(value)) return value.map(item => prune(item));
        if (value === null || typeof value !== 'object') return value;

        const out: any = {};
        for (const key of Object.keys(value)) {
            if (value[key] === null || value[key] === undefined) continue;
            out[key] = OPAQUE_KEYS.indexOf(key) >= 0 ? value[key] : prune(value[key]);
        }
        return out;
    }

    function build(methods: any[], details: any, options: any) {
        const PR = (window as any).PaymentRequest;
        // An undefined optional dictionary is the same as an absent one to WebIDL; a null one is not.
        return new PR(prune(methods), prune(details), prune(options) ?? undefined);
    }

    function toAddress(address: any) {
        if (!address) return null;
        // ContactAddress exposes its fields as accessors on the prototype, so a spread copies
        // nothing - each one is read by name.
        return {
            addressLine: address.addressLine ?? [],
            country: address.country ?? null,
            city: address.city ?? null,
            region: address.region ?? null,
            postalCode: address.postalCode ?? null,
            dependentLocality: address.dependentLocality ?? null,
            sortingCode: address.sortingCode ?? null,
            organization: address.organization ?? null,
            recipient: address.recipient ?? null,
            phone: address.phone ?? null
        };
    }

    async function canMakePayment(methods: any[], details: any) {
        if (!('PaymentRequest' in window)) return false;
        try {
            const request = build(methods, details, null);
            return !!(await request.canMakePayment());
        } catch {
            // Malformed method data, an unsupported currency, or the browser's rate limit.
            return false;
        }
    }

    async function show(id: string, methods: any[], details: any, options: any) {
        if (!('PaymentRequest' in window)) return null;

        let request: any;
        try { request = build(methods, details, options); }
        catch { return null; }

        // The handle is per .NET instance and the services are scoped, so two components sharing one
        // instance can overlap even though the sheet is modal. The second show() rejects at once, so
        // it must neither evict the first, still-open sheet's request nor delete it on the way out -
        // abort() has to keep reaching the sheet that is actually up.
        if (_requests[id] === undefined) _requests[id] = request;

        try {
            const response = await request.show();

            // A response the .NET side never completed - its processing threw, the circuit dropped,
            // the browser timed the sheet out - is dropped here, once this instance opens the next
            // sheet, rather than kept for the life of the page.
            _responses[id] = response;

            return {
                id,
                requestId: response.requestId ?? '',
                methodName: response.methodName ?? '',
                details: response.details ?? null,
                payerName: response.payerName ?? null,
                payerEmail: response.payerEmail ?? null,
                payerPhone: response.payerPhone ?? null,
                shippingOption: response.shippingOption ?? null,
                shippingAddress: toAddress(response.shippingAddress)
            };
        } catch {
            // AbortError when the user dismissed the sheet or abort() closed it, NotAllowedError
            // without a gesture - all of them "no payment", which is what null says.
            return null;
        } finally {
            if (_requests[id] === request) delete _requests[id];
        }
    }

    async function complete(id: string, result: string) {
        const response = _responses[id];
        if (!response) return;

        delete _responses[id];
        try { await response.complete(result); }
        catch { /* the sheet was already dismissed */ }
    }

    async function abort(id: string) {
        const request = _requests[id];
        if (!request) return false;

        try { await request.abort(); return true; }
        catch { return false; }   // the user was already authorizing; the sheet stays up
    }
}(BitButil));
