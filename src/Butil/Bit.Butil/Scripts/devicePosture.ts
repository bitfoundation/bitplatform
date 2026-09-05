var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.devicePosture = {
        isSupported() { return !!(navigator as any).devicePosture; },
        getPosture,
        addChange,
        removeChange
    };

    function posture() { return (navigator as any).devicePosture; }

    // 'continuous' on everything that is not a foldable, which is the honest answer rather than
    // an error - a layout keyed on the posture wants a value on every device.
    function getPosture() { return posture()?.type ?? 'continuous'; }

    function addChange(dotNetRef: any, listenerId: string) {
        const target = posture();
        if (!target) return false;
        const handler: EventListener = () => butil.utils.dispatch(dotNetRef, 'InvokeDevicePostureChange', listenerId, target.type);
        _handlers[listenerId] = handler;
        target.addEventListener('change', handler);
        return true;
    }

    function removeChange(ids: string[]) {
        const target = posture();
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            if (handler && target) target.removeEventListener('change', handler);
        });
    }
}(BitButil));
