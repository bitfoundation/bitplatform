var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface FrameEntry { handle: number; running: boolean; busy: boolean; }

    const _frames: { [id: string]: FrameEntry } = {};
    const _idle: { [id: string]: number } = {};

    butil.scheduler = {
        // requestAnimationFrame is universal; the check exists so this API has the same shape as
        // the rest.
        isSupported() { return typeof window.requestAnimationFrame === 'function'; },
        isIdleCallbackSupported() { return typeof (window as any).requestIdleCallback === 'function'; },
        isPostTaskSupported() { return typeof (window as any).scheduler?.postTask === 'function'; },
        isYieldSupported() { return typeof (window as any).scheduler?.yield === 'function'; },
        isInputPendingSupported() { return typeof (navigator as any).scheduling?.isInputPending === 'function'; },

        // One frame. The timestamp is the same one every callback in that frame receives, so
        // animations driven from different places stay in step.
        requestFrame(dotNetRef: any, id: string) {
            const handle = requestAnimationFrame(timestamp => {
                delete _frames[id];
                butil.utils.dispatch(dotNetRef, 'InvokeAnimationFrame', id, timestamp);
            });
            _frames[id] = { handle, running: false, busy: false };
            return true;
        },

        // A loop: the next frame is requested before .NET is told about this one, so the cadence is
        // the browser's rather than the round trip's.
        startFrameLoop(dotNetRef: any, id: string) {
            const entry: FrameEntry = { handle: 0, running: true, busy: false };
            _frames[id] = entry;

            const step = (timestamp: number) => {
                if (!entry.running) return;
                entry.handle = requestAnimationFrame(step);

                // A .NET callback slower than a frame would otherwise pile up one dispatch per
                // frame forever. Dropping frames while it catches up is what an animation wants;
                // a growing queue of stale timestamps is not.
                if (entry.busy) return;
                entry.busy = true;

                // Caught before finally, not after: dispatch hands back the invocation's own
                // promise, so a .NET handler that throws would leave the promise derived here
                // rejected and unhandled - a browser-level unhandled rejection every frame.
                const promise = butil.utils.dispatch(dotNetRef, 'InvokeAnimationFrame', id, timestamp);
                if (promise && typeof promise.then === 'function') {
                    promise.then(() => { entry.busy = false; }, () => { entry.busy = false; });
                } else {
                    entry.busy = false;
                }
            };

            entry.handle = requestAnimationFrame(step);
            return true;
        },

        cancelFrame(id: string) {
            const entry = _frames[id];
            if (!entry) return;
            delete _frames[id];
            entry.running = false;
            cancelAnimationFrame(entry.handle);
        },

        requestIdle(dotNetRef: any, id: string, timeoutMs: number) {
            const request = (window as any).requestIdleCallback;
            if (typeof request !== 'function') return false;

            const options = timeoutMs > 0 ? { timeout: timeoutMs } : undefined;
            _idle[id] = request((deadline: any) => {
                delete _idle[id];
                // timeRemaining() is a live reading that falls as the idle period is used up, so
                // .NET gets the value at the moment of the call and a note that it decays.
                butil.utils.dispatch(dotNetRef, 'InvokeIdleCallback', id,
                    deadline.didTimeout === true, deadline.timeRemaining());
            }, options);

            return true;
        },

        cancelIdle(id: string) {
            const handle = _idle[id];
            if (handle === undefined) return;
            delete _idle[id];
            (window as any).cancelIdleCallback?.(handle);
        },

        // scheduler.postTask, with a fallback so a caller does not have to branch. The fallback is
        // setTimeout, which is a single queue with no priorities - the priority argument is honoured
        // where the API exists and ignored where it does not, which is the honest degradation.
        async postTask(dotNetRef: any, id: string, priority: string, delayMs: number, signalId: string | null) {
            // A .NET task that throws still ran, and this call answers "why it did not run". Letting
            // the rejection out would report a task that ran as one that did not on the native path,
            // and on the fallback path it would skip finish() entirely and strand the promise, so
            // the caller would wait for a result that never comes. dispatch has already logged it.
            const run = async () => { try { await butil.utils.dispatch(dotNetRef, 'InvokeScheduledTask', id); } catch { /* logged by dispatch */ } };
            const signal = signalId ? butil.abortController.signalOf(signalId) : undefined;
            const scheduler = (window as any).scheduler;

            if (typeof scheduler?.postTask === 'function') {
                const options: any = { priority };
                if (delayMs > 0) options.delay = delayMs;
                if (signal) options.signal = signal;

                try {
                    await scheduler.postTask(run, options);
                    return null;
                } catch (e: any) {
                    // An aborted task rejects; so does an invalid priority. Both are answers, not
                    // crashes. An abort is reported as the same 'aborted' the fallback returns, so
                    // the answer does not depend on which engine is underneath.
                    if (signal?.aborted || e?.name === 'AbortError') return 'aborted';
                    return e?.message ?? String(e);
                }
            }

            if (signal?.aborted) return 'aborted';

            return await new Promise<string | null>(resolve => {
                // once:true takes the listener off when it fires, but a task that simply finishes
                // would leave it attached - and a signal shared across many tasks would then hold
                // every one of them, and its closure, for as long as it lives.
                const onAbort = () => { clearTimeout(handle); resolve('aborted'); };
                const finish = (result: string | null) => {
                    signal?.removeEventListener('abort', onAbort);
                    resolve(result);
                };

                const handle = setTimeout(async () => {
                    if (signal?.aborted) { finish('aborted'); return; }
                    // The postTask branch above reports a rejected task as its message; the fallback
                    // has to do the same, or the same failing task answers 'no error' on one engine
                    // and an error on another.
                    try {
                        await run();
                        finish(null);
                    } catch (e: any) {
                        finish(e?.message ?? String(e));
                    }
                }, delayMs > 0 ? delayMs : 0);

                signal?.addEventListener('abort', onAbort, { once: true });
            });
        },

        // Hands the thread back so the browser can paint or handle input, then continues. Where
        // scheduler.yield exists the continuation keeps its place in the priority queue; the
        // fallback is a plain macrotask, which goes to the back.
        async yield() {
            const scheduler = (window as any).scheduler;
            if (typeof scheduler?.yield === 'function') {
                try { await scheduler.yield(); return; } catch { /* fall through to the macrotask */ }
            }
            await new Promise(resolve => setTimeout(resolve, 0));
        },

        // False where the API is missing, which reads the same as "nothing is waiting" on purpose:
        // a caller uses it to decide whether to keep working, and the safe answer where it cannot be
        // known is to keep working.
        isInputPending() {
            const scheduling = (navigator as any).scheduling;
            if (typeof scheduling?.isInputPending !== 'function') return false;
            try { return scheduling.isInputPending() === true; } catch { return false; }
        },

        disposeAll() {
            for (const id of Object.keys(_frames)) butil.scheduler.cancelFrame(id);
            for (const id of Object.keys(_idle)) butil.scheduler.cancelIdle(id);
        }
    };
}(BitButil));
