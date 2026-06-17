var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: EventListener } = {};

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

    function addEventListener(elementName: string, eventName: string, methodName: string, dotNetRef: DotNet.DotNetObject, listenerId: string, argsMembers: string[], options: AddEventListenerOptions | boolean, preventDefault: boolean, stopPropagation: boolean) {
        const target = resolveTarget(elementName);
        if (!target) return;

        // When { once: true } is requested the browser auto-detaches after the first call; mirror
        // that by dropping our own map entry so the listenerId doesn't linger after it fires.
        const once = typeof options === 'object' && options.once === true;

        const handler: EventListener = e => {
            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();
            if (once) delete _handlers[listenerId];
            butil.utils.dispatch(dotNetRef, methodName, listenerId, mapEvent(e, argsMembers));
        };

        _handlers[listenerId] = handler;

        target.addEventListener(eventName, handler, options);
    }

    function removeEventListener(elementName: string, eventName: string, dotnetListenerIds: string[], options: EventListenerOptions | boolean) {
        const target = resolveTarget(elementName);

        dotnetListenerIds.forEach(id => {
            const handler = _handlers[id];
            if (!handler) return;
            // A handler is only ever stored after a successful add (which requires the target to be
            // available), and the only targets are window/document - both live for the page's
            // lifetime. So we always drop the map entry here to keep it from growing unbounded;
            // detach from the target when it's resolvable (it normally is).
            if (target) target.removeEventListener(eventName, handler, options);
            delete _handlers[id];
        });
    }
}(BitButil));