var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _listeners: { [id: string]: { element: HTMLElement, handler: any } } = {};

    butil.invokerCommands = {
        isSupported() {
            return typeof HTMLButtonElement !== 'undefined'
                && 'command' in HTMLButtonElement.prototype
                && 'CommandEvent' in window;
        },
        setCommandFor(invoker: HTMLElement, target: HTMLElement, command: string) {
            if (!invoker || !target) return false;
            if (!('commandForElement' in invoker)) return false;
            try {
                // The property, not the `commandfor` attribute: the attribute takes an id, and a
                // Blazor-rendered target may not have one. The property points straight at the node.
                (invoker as any).commandForElement = target;
                (invoker as any).command = command;
                return true;
            } catch {
                return false;
            }
        },
        clearCommandFor(invoker: HTMLElement) {
            if (!invoker || !('commandForElement' in invoker)) return false;
            (invoker as any).commandForElement = null;
            invoker.removeAttribute('command');
            return true;
        },
        getCommand(invoker: HTMLElement) { return (invoker as any)?.command ?? ''; },
        onCommand(element: HTMLElement, id: string, dotNetRef: any, method: string) {
            if (!element) return false;

            const handler = (e: any) => {
                butil.utils.dispatch(dotNetRef, method, id, {
                    command: e?.command ?? '',
                    // The invoking element itself can't cross interop, so report what identifies it.
                    sourceId: e?.source?.id ?? '',
                    sourceTag: (e?.source?.tagName ?? '').toLowerCase()
                });
            };

            // 'command' fires on the *target*, not on the button - which is what lets one handler
            // serve however many invokers point at it.
            element.addEventListener('command', handler);
            _listeners[id] = { element, handler };
            return true;
        },
        offCommand(id: string) {
            const entry = _listeners[id];
            if (!entry) return;
            delete _listeners[id];
            try { entry.element.removeEventListener('command', entry.handler); } catch { /* element gone */ }
        },
        disposeAll() {
            for (const id of Object.keys(_listeners)) butil.invokerCommands.offCommand(id);
        }
    };
}(BitButil));
