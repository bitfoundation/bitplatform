var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.screenOrientation = {
        type() { return window.screen.orientation.type; },
        angle() { return window.screen.orientation.angle; },
        lock(type: string) { return (window.screen.orientation as any).lock(type); },
        unlock() { return window.screen.orientation.unlock(); },
        addChange, removeChange,
    };

    function addChange(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler: EventListener = e => {
            const orientation = e.target as ScreenOrientation;
            butil.utils.dispatch(dotNetRef, 'InvokeScreenOrientationChange', listenerId, { angle: orientation.angle, type: orientation.type });
        };

        _handlers[listenerId] = handler;
        window.screen.orientation.addEventListener('change', handler);
    }
    function removeChange(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window.screen.orientation.removeEventListener('change', handler);
        });
    }
}(BitButil));