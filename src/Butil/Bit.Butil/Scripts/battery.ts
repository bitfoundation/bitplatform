var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The four events that make up "the battery changed"; the spec has no single change event.
    const CHANGE_EVENTS = ['chargingchange', 'levelchange', 'chargingtimechange', 'dischargingtimechange'];

    const _listeners: { [id: string]: { battery: any, handler: () => void } } = {};

    function batterySeconds(value: number) {
        return Number.isFinite(value) ? value : null;
    }

    function snapshot(b: any) {
        return {
            charging: !!b.charging,
            chargingTime: batterySeconds(b.chargingTime),
            dischargingTime: batterySeconds(b.dischargingTime),
            level: b.level
        };
    }

    butil.battery = {
        isSupported() { return typeof (window.navigator as any).getBattery === 'function'; },
        async getStatus() {
            const nav = window.navigator as any;
            if (typeof nav.getBattery !== 'function') {
                return { charging: true, chargingTime: 0, dischargingTime: null, level: 1 };
            }
            return snapshot(await nav.getBattery());
        },
        async subscribe(dotNetRef: any, listenerId: string) {
            const nav = window.navigator as any;
            if (typeof nav.getBattery !== 'function') return false;

            const b = await nav.getBattery();
            const handler = () => butil.utils.dispatch(dotNetRef, 'InvokeBatteryChange', listenerId, snapshot(b));
            _listeners[listenerId] = { battery: b, handler };
            for (const e of CHANGE_EVENTS) b.addEventListener(e, handler);
            return true;
        },
        unsubscribe(listenerId: string) {
            const entry = _listeners[listenerId];
            if (!entry) return;
            delete _listeners[listenerId];
            for (const e of CHANGE_EVENTS) entry.battery.removeEventListener(e, entry.handler);
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.battery.unsubscribe(id);
        }
    };
}(BitButil));
