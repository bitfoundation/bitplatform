var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: { target: EventTarget, handler: any } } = {};

    butil.keyboard = {
        add,
        addOn,
        remove
    };

    // What Keyboard.cs sends (JsKeyboardShortcut): the modifiers split into flags, and ignoreWhenEditable
    // already resolved from its default.
    interface Shortcut {
        code: string; alt: boolean; ctrl: boolean; meta: boolean; shift: boolean;
        preventDefault: boolean; stopPropagation: boolean; repeat: boolean; ignoreWhenEditable: boolean;
    }

    function makeHandler(dotNetRef: any, listenerId: string, shortcut: Shortcut, scope?: EventTarget) {
        const { code, alt, ctrl, meta, shift, preventDefault, stopPropagation, repeat, ignoreWhenEditable } = shortcut;
        return (e: KeyboardEvent) => {
            if (e.code !== code) return;

            if ((!alt && e.altKey) || (alt && !e.altKey)) return;
            if ((!ctrl && e.ctrlKey) || (ctrl && !e.ctrlKey)) return;
            if ((!meta && e.metaKey) || (meta && !e.metaKey)) return;
            if ((!shift && e.shiftKey) || (shift && !e.shiftKey)) return;

            if (!repeat && e.repeat) return;

            // Checked before preventDefault, so a key the focused control acts on keeps its own action -
            // a page's Delete shortcut must not stop Delete from deleting text in an input. The element a
            // shortcut is scoped to never counts: scoping one to a text field means it is meant to fire there.
            if (ignoreWhenEditable) {
                const target = butil.editableTarget.of(e);
                if (target !== scope && butil.editableTarget.takesKey(target, e)) return;
            }

            preventDefault && e.preventDefault();
            stopPropagation && e.stopPropagation();

            butil.utils.dispatch(dotNetRef, 'InvokeKeyboard', listenerId);
        };
    }

    function add(dotNetRef: any, listenerId: string, shortcut: Shortcut) {
        const handler = makeHandler(dotNetRef, listenerId, shortcut);
        _handlers[listenerId] = { target: document, handler };
        document.addEventListener('keydown', handler);
    }

    function addOn(dotNetRef: any, listenerId: string, element: HTMLElement, shortcut: Shortcut) {
        if (!element) {
            // Fall back to document so callers don't lose the listener silently when the
            // element ref isn't ready yet.
            return add(dotNetRef, listenerId, shortcut);
        }
        const handler = makeHandler(dotNetRef, listenerId, shortcut, element);
        _handlers[listenerId] = { target: element, handler };
        element.addEventListener('keydown', handler);
    }

    function remove(ids: string[]) {
        ids.forEach(id => {
            const entry = _handlers[id];
            if (!entry) return;
            delete _handlers[id];
            try { entry.target.removeEventListener('keydown', entry.handler); } catch { /* detached */ }
        });
    }
}(BitButil));