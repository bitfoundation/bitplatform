var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.events = {
        addEventListener,
        removeEventListener
    };

    function addEventListener(elementName, eventName, methodName, listenerId, argsMembers, options, preventDefault, stopPropagation) {
        const argsMap = e => (argsMembers || []).reduce((pre, cur) => (pre[cur] = e[cur], pre), {});
        const handler = e => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId, argsMap(e));
        };

        _handlers[listenerId] = handler;

        window[elementName].addEventListener(eventName, handler, options);
    }

    function removeEventListener(elementName, eventName, dotnetListenerIds, options) {
        dotnetListenerIds.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            window[elementName].removeEventListener(eventName, handler, options);
        });
    }
}(BitButil));