var BitButil = BitButil || {};

(function (butil: any) {
    butil.battery = {
        isSupported() { return typeof (window.navigator as any).getBattery === 'function'; },
        async getStatus() {
            const nav = window.navigator as any;
            if (typeof nav.getBattery !== 'function') {
                return { charging: true, chargingTime: 0, dischargingTime: Infinity, level: 1 };
            }
            const b = await nav.getBattery();
            return {
                charging: !!b.charging,
                chargingTime: b.chargingTime,
                dischargingTime: b.dischargingTime,
                level: b.level
            };
        }
    };
}(BitButil));
