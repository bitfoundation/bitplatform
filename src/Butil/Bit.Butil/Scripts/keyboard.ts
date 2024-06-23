var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.keyboard = {
        add,
        remove
    };

    function add(methodName, listenerId, code, alt, ctrl, meta, shift, preventDefault, stopPropagation, repeat) {
        const handler = e => {
            if (e.code !== code) return;

            if ((!alt && e.altKey) || (alt && !e.altKey)) return;
            if ((!ctrl && e.ctrlKey) || (ctrl && !e.ctrlKey)) return;
            if ((!meta && e.metaKey) || (meta && !e.metaKey)) return;
            if ((!shift && e.shiftKey) || (shift && !e.shiftKey)) return;

            if (!repeat && e.repeat) return;

            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();

            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId);
        };

        _handlers[listenerId] = handler;

        document.addEventListener('keydown', handler);
    }

    function remove(ids) {
        ids.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            document.removeEventListener('keydown', handler);
        });
    }
}(BitButil));