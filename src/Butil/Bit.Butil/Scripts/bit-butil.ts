var butil = (function () {
    const _handlers = {};

    return {
        addEventListener,
        removeEventListener
    };

    function addEventListener(elementName, eventName, dotnetMethodName, dotnetListenerId, selectedMembers, options) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Butil', dotnetMethodName, dotnetListenerId, selectedMembers && selectedMembers.reduce((pre, cur) => (pre[cur] = e[cur], pre), {}));
        };

        _handlers[dotnetListenerId] = handler;

        window[elementName].addEventListener(eventName, handler, options);
    }

    function removeEventListener(elementName, eventName, dotnetListenerIds, options) {
        dotnetListenerIds.forEach(id => {
            const handler = _handlers[id];
            window[elementName].removeEventListener(eventName, handler, options);
        });
    }
}());