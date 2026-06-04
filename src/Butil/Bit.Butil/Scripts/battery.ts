var BitButil = BitButil || {};

(function (butil: any) {
    function batterySeconds(value: number) {
        return Number.isFinite(value) ? value : null;
    }

    butil.battery = {
        isSupported() { return typeof (window.navigator as any).getBattery === 'function'; },
        async getStatus() {
            const nav = window.navigator as any;
            if (typeof nav.getBattery !== 'function') {
                return { charging: true, chargingTime: 0, dischargingTime: null, level: 1 };
            }
            const b = await nav.getBattery();
            return {
                charging: !!b.charging,
                chargingTime: batterySeconds(b.chargingTime),
                dischargingTime: batterySeconds(b.dischargingTime),
                level: b.level
            };
        }
    };
}(BitButil));
