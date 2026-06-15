var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

    butil.screen = {
        availHeight() { return window.screen.availHeight },
        availWidth() { return window.screen.availWidth },
        colorDepth() { return window.screen.colorDepth },
        height() { return window.screen.height },
        isExtended() { return (window.screen as any).isExtended },
        pixelDepth() { return window.screen.pixelDepth },
        width() { return window.screen.width },
        addChange,
        removeChange
    };

    function addChange(dotNetRef: DotNet.DotNetObject, listenerId: string) {
        const handler: EventListener = () => {
            butil.utils.dispatch(dotNetRef, 'InvokeScreenChange', listenerId);
        };

        _handlers[listenerId] = handler;
        (window.screen as any).addEventListener('change', handler);
    }

    function removeChange(ids: string[]) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            (window.screen as any).removeEventListener('change', handler);
        });
    }
}(BitButil));