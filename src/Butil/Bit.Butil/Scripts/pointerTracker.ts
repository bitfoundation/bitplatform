var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _trackers: { [id: string]: { element: HTMLElement, events: string[], handler: any } } = {};

    butil.pointerTracker = {
        // Both halves ship together in practice, but they are separate features: coalesced events
        // are the samples the browser dropped between frames, predicted ones are where it thinks the
        // pointer is going.
        isSupported() { return typeof PointerEvent !== 'undefined' && 'getCoalescedEvents' in PointerEvent.prototype; },
        supportsPrediction() { return typeof PointerEvent !== 'undefined' && 'getPredictedEvents' in PointerEvent.prototype; },
        track(element: HTMLElement, id: string, events: string[], includePredicted: boolean, dotNetRef: any, method: string) {
            if (!element) return false;

            const sample = (e: any) => ({
                x: e.offsetX ?? 0,
                y: e.offsetY ?? 0,
                clientX: e.clientX ?? 0,
                clientY: e.clientY ?? 0,
                pressure: e.pressure ?? 0,
                tangentialPressure: e.tangentialPressure ?? 0,
                tiltX: e.tiltX ?? 0,
                tiltY: e.tiltY ?? 0,
                twist: e.twist ?? 0,
                width: e.width ?? 0,
                height: e.height ?? 0,
                timeStamp: e.timeStamp ?? 0
            });

            const handler = (e: PointerEvent) => {
                const anyEvent = e as any;

                // getCoalescedEvents returns the samples the browser merged into this one frame's
                // event. A drawing surface that only reads the event itself loses every sample
                // between frames - which is the difference between a smooth stroke and a polyline.
                const coalesced = typeof anyEvent.getCoalescedEvents === 'function'
                    ? anyEvent.getCoalescedEvents().map(sample)
                    : [sample(e)];

                const predicted = includePredicted && typeof anyEvent.getPredictedEvents === 'function'
                    ? anyEvent.getPredictedEvents().map(sample)
                    : [];

                butil.utils.dispatch(dotNetRef, method, id, {
                    type: e.type,
                    pointerId: e.pointerId ?? 0,
                    pointerType: e.pointerType ?? '',
                    isPrimary: e.isPrimary === true,
                    buttons: e.buttons ?? 0,
                    current: sample(e),
                    coalesced,
                    predicted
                });
            };

            const names = events?.length ? events : ['pointermove'];
            // Passive: this is a sampling listener, never one that cancels the gesture, and saying
            // so keeps it off the scrolling critical path.
            for (const name of names) element.addEventListener(name, handler as any, { passive: true });

            _trackers[id] = { element, events: names, handler };
            return true;
        },
        untrack(id: string) {
            const tracker = _trackers[id];
            if (!tracker) return;
            delete _trackers[id];
            for (const name of tracker.events) {
                try { tracker.element.removeEventListener(name, tracker.handler); } catch { /* element gone */ }
            }
        },
        disposeAll() {
            for (const id of Object.keys(_trackers)) butil.pointerTracker.untrack(id);
        }
    };
}(BitButil));
