var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.screenOrientation = {
        type() { return window.screen.orientation.type; },
        angle() { return window.screen.orientation.angle; },
        lock(type) { return (window.screen.orientation as any).lock(type); },
        unlock() { return window.screen.orientation.unlock(); },
        addChange, removeChange,
    };

    function addChange(methodName, listenerId) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId, { angle: e.target.angle, type: e.target.type });
        };

        _handlers[listenerId] = handler;
        window.screen.orientation.addEventListener('change', handler);
    }
    function removeChange(ids) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.screen.orientation.removeEventListener('change', handler);
        });
    }
}(BitButil));