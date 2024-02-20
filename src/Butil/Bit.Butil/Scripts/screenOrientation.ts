var BitButil = BitButil || {};

(function (butil: any) {
    butil.screenOrientation = {
        type() { return window.screen.orientation.type; },
        angle() { return window.screen.orientation.angle; },
        lock(type) { return (window.screen.orientation as any).lock(type); },
        unlock() { return window.screen.orientation.unlock(); },
    };
}(BitButil));