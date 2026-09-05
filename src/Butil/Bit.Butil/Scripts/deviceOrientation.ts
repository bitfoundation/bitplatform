var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface Listener { event: string; handler: (e: any) => void; }

    const _listeners: { [id: string]: Listener } = {};

    // iOS 13+ gates both event streams behind an explicit, gesture-initiated grant. Everywhere else
    // the classes exist without the static, which counts as already-granted.
    function permissionHolder(kind: 'orientation' | 'motion') {
        const W = window as any;
        const ctor = kind === 'motion' ? W.DeviceMotionEvent : W.DeviceOrientationEvent;
        return typeof ctor?.requestPermission === 'function' ? ctor : null;
    }

    function readOrientation(e: any, absolute: boolean) {
        return {
            // A device flat on a table reports alpha/beta/gamma; a browser that fires the event
            // without sensor data reports nulls, which become 0 rather than crossing as null.
            alpha: e.alpha ?? 0,
            beta: e.beta ?? 0,
            gamma: e.gamma ?? 0,
            absolute: absolute || !!e.absolute
        };
    }

    function readMotion(e: any) {
        const acc = e.acceleration ?? {};
        const accG = e.accelerationIncludingGravity ?? {};
        const rot = e.rotationRate ?? {};
        return {
            accelerationX: acc.x ?? 0, accelerationY: acc.y ?? 0, accelerationZ: acc.z ?? 0,
            accelerationIncludingGravityX: accG.x ?? 0,
            accelerationIncludingGravityY: accG.y ?? 0,
            accelerationIncludingGravityZ: accG.z ?? 0,
            rotationAlpha: rot.alpha ?? 0, rotationBeta: rot.beta ?? 0, rotationGamma: rot.gamma ?? 0,
            interval: e.interval ?? 0
        };
    }

    // Both streams fire far faster than a Blazor render can keep up with (iOS motion defaults to
    // 60 Hz), so every subscription is throttled in JS rather than flooding the interop channel.
    const throttle = butil.utils.throttle;

    butil.deviceOrientation = {
        // "Is any of this here at all", in the spelling the rest of Butil uses. The two below
        // answer the finer question of which of the two event streams exists.
        isSupported() { return 'DeviceOrientationEvent' in window || 'DeviceMotionEvent' in window; },
        isOrientationSupported() { return 'DeviceOrientationEvent' in window; },
        isMotionSupported() { return 'DeviceMotionEvent' in window; },
        needsPermission() { return !!(permissionHolder('orientation') || permissionHolder('motion')); },
        async requestPermission() {
            const holders = [permissionHolder('orientation'), permissionHolder('motion')].filter(h => !!h);
            // No static means no gate: the events are available as soon as the device has sensors.
            if (holders.length === 0) return 'granted';
            try {
                const results = await Promise.all(holders.map(h => h.requestPermission()));
                return results.every(r => r === 'granted') ? 'granted' : 'denied';
            } catch {
                // Thrown when the call didn't come from a user gesture.
                return 'denied';
            }
        },
        subscribeOrientation(dotNetRef: any, listenerId: string, absolute: boolean, minInterval: number) {
            // 'deviceorientationabsolute' reports against the earth's frame rather than the
            // device's arbitrary starting heading - the only one useful for a compass.
            const event = absolute && 'ondeviceorientationabsolute' in window
                ? 'deviceorientationabsolute'
                : 'deviceorientation';
            const handler = throttle(minInterval, (e: any) =>
                butil.utils.dispatch(dotNetRef, 'InvokeOrientation', listenerId, readOrientation(e, absolute)));
            _listeners[listenerId] = { event, handler };
            window.addEventListener(event, handler);
        },
        subscribeMotion(dotNetRef: any, listenerId: string, minInterval: number) {
            const handler = throttle(minInterval, (e: any) =>
                butil.utils.dispatch(dotNetRef, 'InvokeMotion', listenerId, readMotion(e)));
            _listeners[listenerId] = { event: 'devicemotion', handler };
            window.addEventListener('devicemotion', handler);
        },
        unsubscribe(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            window.removeEventListener(entry.event, entry.handler);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.deviceOrientation.unsubscribe(id);
        }
    };
}(BitButil));
