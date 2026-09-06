var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // One controller per pending wait, filed under the .NET instance's handle and the per-call one -
    // see butil.abortable.registry for why the per-call handle exists.
    const _waits = butil.abortable.registry();

    butil.webOtp = {
        isSupported() { return 'OTPCredential' in window; },
        receive,
        abort
    };

    async function receive(instanceId: string, requestId: string, timeoutMs: number | null) {
        if (!('OTPCredential' in window) || !navigator.credentials) {
            _waits.preAborted(requestId);
            return null;
        }

        // Asked first, before anything else is touched: a wait whose abort already arrived never
        // starts - and must leave whatever else the instance has in flight alone.
        if (_waits.preAborted(requestId)) return null;

        // One wait per instance: a second receive would otherwise leave the first controller
        // unreachable, and its browser prompt with it.
        _waits.abort(instanceId);

        const controller = _waits.track(instanceId, requestId);

        // Zero is a timeout like any other - "give up at once" - so only a null means no timeout.
        // Negative values never arrive: the C# side rejects them before dispatching.
        const cancelTimer = (timeoutMs !== null && timeoutMs !== undefined && timeoutMs >= 0)
            ? deadline(timeoutMs, () => { try { controller.abort(); } catch { } })
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
            if (cancelTimer !== null) cancelTimer();
            _waits.release(instanceId, requestId, controller);
        }
    }

    // setTimeout's delay is a signed 32-bit int: anything past ~24.9 days wraps and fires on the
    // next tick, which would turn a long TimeSpan into an immediate abort - the one outcome a
    // timeout must never produce. Long waits are chained in bounded slices so the deadline is the
    // one that was asked for. Returns its own canceller, since only the innermost timer is live.
    const MAX_DELAY = 2147483647;

    function deadline(delayMs: number, onElapsed: () => void) {
        let timer: any = null;
        let remaining = delayMs;

        const arm = () => {
            const slice = Math.min(remaining, MAX_DELAY);
            remaining -= slice;
            timer = setTimeout(() => (remaining > 0 ? arm() : onElapsed()), slice);
        };
        arm();

        return () => { if (timer !== null) clearTimeout(timer); };
    }

    // Without a requestId this ends every wait the instance has in flight - what the public Abort()
    // does. With one, only that wait.
    function abort(instanceId: string, requestId?: string | null) {
        return _waits.abort(instanceId, requestId);
    }
}(BitButil));
