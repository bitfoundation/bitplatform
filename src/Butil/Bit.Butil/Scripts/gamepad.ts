var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    interface ConnectionListener { onConnect: () => void; onDisconnect: () => void; }
    interface PollListener { frame: number; last: string; minInterval: number; lastSentAt: number; }

    const _connections: { [id: string]: ConnectionListener } = {};
    const _polls: { [id: string]: PollListener } = {};

    function read(pad: any) {
        if (!pad) return null;
        return {
            index: pad.index,
            id: pad.id ?? '',
            connected: !!pad.connected,
            mapping: pad.mapping ?? '',
            timestamp: pad.timestamp ?? 0,
            // Axes are floats in [-1, 1]; buttons carry an analogue value as well as the two flags,
            // because triggers on a standard-mapping pad are buttons, not axes.
            axes: Array.from(pad.axes ?? []),
            buttons: Array.from(pad.buttons ?? [], (b: any) =>
                typeof b === 'number'
                    ? { pressed: b > 0.5, touched: b > 0, value: b }
                    : { pressed: !!b.pressed, touched: !!b.touched, value: b.value ?? 0 }),
            hasVibration: !!(pad.vibrationActuator)
        };
    }

    function snapshot() {
        const nav = window.navigator as any;
        if (!nav.getGamepads) return [];
        // getGamepads() returns a sparse array with a null per empty port.
        return Array.from(nav.getGamepads(), read).filter(p => !!p);
    }

    butil.gamepad = {
        isSupported() { return typeof (window.navigator as any).getGamepads === 'function'; },
        getGamepads() { return snapshot(); },
        subscribeConnection(dotNetRef: any, listenerId: string) {
            const onConnect = () => butil.utils.dispatch(dotNetRef, 'InvokeGamepadConnected', listenerId, snapshot());
            const onDisconnect = () => butil.utils.dispatch(dotNetRef, 'InvokeGamepadDisconnected', listenerId, snapshot());
            _connections[listenerId] = { onConnect, onDisconnect };
            window.addEventListener('gamepadconnected', onConnect);
            window.addEventListener('gamepaddisconnected', onDisconnect);
        },
        unsubscribeConnection(listenerId: string) {
            const entry = _connections[listenerId];
            if (!entry) return;
            delete _connections[listenerId];
            window.removeEventListener('gamepadconnected', entry.onConnect);
            window.removeEventListener('gamepaddisconnected', entry.onDisconnect);
        },
        subscribePoll(dotNetRef: any, listenerId: string, minInterval: number) {
            // Gamepads have no input events - the only way to read them is to poll. Polling on
            // requestAnimationFrame keeps that in step with the browser's own frame loop and stops
            // it entirely while the tab is hidden. Crossing into .NET 60 times a second would be
            // far too chatty, so a frame is only forwarded when the state actually changed and at
            // most once per minInterval.
            const entry: PollListener = { frame: 0, last: '', minInterval, lastSentAt: 0 };
            _polls[listenerId] = entry;

            const tick = () => {
                if (!_polls[listenerId]) return;
                entry.frame = requestAnimationFrame(tick);

                const pads = snapshot();
                const serialized = JSON.stringify(pads);
                if (serialized === entry.last) return;

                const now = performance.now();
                if (now - entry.lastSentAt < entry.minInterval) return;

                entry.last = serialized;
                entry.lastSentAt = now;
                butil.utils.dispatch(dotNetRef, 'InvokeGamepadChanged', listenerId, pads);
            };

            entry.frame = requestAnimationFrame(tick);
        },
        unsubscribePoll(listenerId: string) {
            const entry = _polls[listenerId];
            if (!entry) return;
            delete _polls[listenerId];
            cancelAnimationFrame(entry.frame);
        },
        async vibrate(index: number, duration: number, strongMagnitude: number, weakMagnitude: number, startDelay: number) {
            const nav = window.navigator as any;
            if (!nav.getGamepads) return false;
            const pad = nav.getGamepads()[index];
            const actuator = pad?.vibrationActuator;
            if (!actuator?.playEffect) return false;
            try {
                await actuator.playEffect('dual-rumble', { duration, strongMagnitude, weakMagnitude, startDelay });
                return true;
            } catch {
                // The pad has an actuator but no dual-rumble effect, or the effect was pre-empted.
                return false;
            }
        },
        async resetVibration(index: number) {
            const nav = window.navigator as any;
            const actuator = nav.getGamepads?.()[index]?.vibrationActuator;
            if (!actuator?.reset) return;
            try { await actuator.reset(); } catch { /* nothing was playing */ }
        },
        disposeAll() {
            for (const id of Object.keys(_connections)) butil.gamepad.unsubscribeConnection(id);
            for (const id of Object.keys(_polls)) butil.gamepad.unsubscribePoll(id);
        }
    };
}(BitButil));
