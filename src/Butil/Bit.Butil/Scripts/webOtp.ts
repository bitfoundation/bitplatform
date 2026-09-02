var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The controller behind each pending wait, keyed by the .NET side's handle. Aborting it is the
    // only way to end the browser's wait early - the promise itself has no cancellation.
    const _pending: { [id: string]: AbortController } = {};

    butil.webOtp = {
        isSupported() { return 'OTPCredential' in window; },
        receive,
        abort
    };

    async function receive(id: string, timeoutMs: number | null) {
        if (!('OTPCredential' in window) || !navigator.credentials) return null;

        // Defensive: a second receive under the same handle would otherwise leave the first
        // controller unreachable, and its browser prompt with it.
        abort(id);

        const controller = new AbortController();
        _pending[id] = controller;

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
