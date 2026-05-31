var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers = {};

    butil.events = {
        addEventListener,
        removeEventListener,
        mapEvent
    };

    function mapTouchList(list: any): any[] {
        if (!list) return [];
        const out = [];
        for (let i = 0; i < list.length; i++) {
            const t = list[i];
            out.push({
                identifier: t.identifier,
                clientX: t.clientX,
                clientY: t.clientY,
                pageX: t.pageX,
                pageY: t.pageY,
                screenX: t.screenX,
                screenY: t.screenY,
                radiusX: t.radiusX ?? 0,
                radiusY: t.radiusY ?? 0,
                rotationAngle: t.rotationAngle ?? 0,
                force: t.force ?? 0
            });
        }
        return out;
    }

    function mapEvent(e: any, members: string[]) {
        const out: any = {};
        for (const m of (members || [])) {
            switch (m) {
                case 'touches':
                case 'targetTouches':
                case 'changedTouches':
                    out[m] = mapTouchList(e[m]);
                    break;
                case 'clipboardText':
                    out[m] = e.clipboardData?.getData?.('text/plain') ?? null;
                    break;
                case 'relatedTarget':
                    // RelatedTarget is a DOM node — we can only safely send a stringy id.
                    out[m] = e.relatedTarget?.id ?? '';
                    break;
                default:
                    out[m] = e[m];
            }
        }
        return out;
    }

    function addEventListener(elementName, eventName, methodName, listenerId, argsMembers, options, preventDefault, stopPropagation) {
        const handler = e => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            DotNet.invokeMethodAsync('Bit.Butil', methodName, listenerId, mapEvent(e, argsMembers));
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