var butil = (function () {
    const _handlers = {};

    return {
        addEventListener,
        removeEventListener
    };

    function addEventListener(elementName, eventName, dotnetMethodName, dotnetListenerId, selectedMembers, options) {
        const handler = e => {
            DotNet.invokeMethodAsync('Bit.Tooling.Butil', dotnetMethodName, dotnetListenerId, selectedMembers && selectedMembers.reduce((pre, cur) => (pre[cur] = e[cur], pre), {}));
        };

        _handlers[dotnetListenerId] = handler;

        window[elementName].addEventListener(eventName, handler, options);
    }

    function removeEventListener(elementName, eventName, dotnetListenerId, options) {
        const handler = _handlers[dotnetListenerId];

        window[elementName].removeEventListener(eventName, handler, options);
    }
}());