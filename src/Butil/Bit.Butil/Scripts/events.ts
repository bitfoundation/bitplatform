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
                    // A DOM node can't be marshaled to .NET, so we surface only its id.
                    // Empty string when there's no related target or it has no id - this matches
                    // the string contract of ButilMouseEventArgs.RelatedTarget.
                    out[m] = e.relatedTarget?.id ?? '';
                    break;
                default:
                    out[m] = e[m];
            }
        }
        return out;
    }

    function resolveTarget(elementName: string): EventTarget | undefined {
        const target = (window as any)[elementName];
        if (target && typeof target.addEventListener === 'function') return target;
        // The C# side controls elementName ("window"/"document"), so reaching here means the
        // target isn't available yet (or an unexpected name was passed). Warn instead of throwing
        // an unhandled error from inside the interop call.
        console.warn(`BitButil.events: '${elementName}' is not an available EventTarget; listener skipped.`);
        return undefined;
    }

    function addEventListener(elementName, eventName, methodName, dotNetRef, listenerId, argsMembers, options, preventDefault, stopPropagation) {
        const target = resolveTarget(elementName);
        if (!target) return;

        const handler = e => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            dotNetRef.invokeMethodAsync(methodName, listenerId, mapEvent(e, argsMembers));
        };

        _handlers[listenerId] = handler;

        target.addEventListener(eventName, handler, options);
    }

    function removeEventListener(elementName, eventName, dotnetListenerIds, options) {
        const target = resolveTarget(elementName);

        dotnetListenerIds.forEach(id => {
            const handler = _handlers[id];
            delete _handlers[id];
            if (target && handler) {
                target.removeEventListener(eventName, handler, options);
            }
        });
    }
}(BitButil));