var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _contexts: { [id: string]: { context: any, element: any } } = {};

    butil.editContext = {
        isSupported() { return typeof (window as any).EditContext === 'function'; },
        attach(element: HTMLElement, id: string, options: any, dotNetRef: any,
               textMethod: string, compositionMethod: string, formatMethod: string) {
            const EC = (window as any).EditContext;
            if (typeof EC !== 'function' || !element) return false;

            let context: any;
            try {
                context = new EC({
                    text: options?.text ?? '',
                    selectionStart: options?.selectionStart ?? 0,
                    selectionEnd: options?.selectionEnd ?? 0
                });
            } catch {
                return false;
            }

            // The edit context takes over text input for this element: the element stops being
            // edited directly and becomes a surface the app paints, which is the whole point.
            (element as any).editContext = context;

            context.addEventListener('textupdate', (e: any) => {
                butil.utils.dispatch(dotNetRef, textMethod, id, {
                    text: e.text ?? '',
                    updateRangeStart: e.updateRangeStart ?? 0,
                    updateRangeEnd: e.updateRangeEnd ?? 0,
                    selectionStart: e.selectionStart ?? 0,
                    selectionEnd: e.selectionEnd ?? 0,
                    // The context's whole text after the update - what a simple consumer renders.
                    value: context.text ?? ''
                });
            });

            context.addEventListener('compositionstart', () =>
                butil.utils.dispatch(dotNetRef, compositionMethod, id, true));
            context.addEventListener('compositionend', () =>
                butil.utils.dispatch(dotNetRef, compositionMethod, id, false));

            // IME underlines and highlights: the ranges the input method wants drawn while a
            // composition is in flight. Nothing draws them for us - that is the app's job.
            context.addEventListener('textformatupdate', (e: any) => {
                const formats = (e.getTextFormats?.() ?? []).map((f: any) => ({
                    rangeStart: f.rangeStart ?? 0,
                    rangeEnd: f.rangeEnd ?? 0,
                    underlineStyle: f.underlineStyle ?? '',
                    underlineThickness: f.underlineThickness ?? ''
                }));
                butil.utils.dispatch(dotNetRef, formatMethod, id, formats);
            });

            _contexts[id] = { context, element };
            return true;
        },
        getText(id: string) { return _contexts[id]?.context?.text ?? null; },
        getSelection(id: string) {
            const context = _contexts[id]?.context;
            if (!context) return null;
            return { start: context.selectionStart ?? 0, end: context.selectionEnd ?? 0 };
        },
        updateText(id: string, rangeStart: number, rangeEnd: number, text: string) {
            const context = _contexts[id]?.context;
            if (!context) return;
            // Tells the input method what the text is now - a programmatic edit the IME did not
            // make. Without this the IME's idea of the buffer drifts from the app's.
            try { context.updateText(rangeStart, rangeEnd, text); } catch { /* out of range */ }
        },
        updateSelection(id: string, start: number, end: number) {
            const context = _contexts[id]?.context;
            if (!context) return;
            try { context.updateSelection(start, end); } catch { /* out of range */ }
        },
        updateControlBounds(id: string, x: number, y: number, width: number, height: number) {
            const context = _contexts[id]?.context;
            if (!context?.updateControlBounds) return;
            // Where the editing surface is on screen, so the IME candidate window can be placed
            // next to it rather than in the corner of the page.
            try { context.updateControlBounds(new DOMRect(x, y, width, height)); } catch { /* unsupported */ }
        },
        updateSelectionBounds(id: string, x: number, y: number, width: number, height: number) {
            const context = _contexts[id]?.context;
            if (!context?.updateSelectionBounds) return;
            try { context.updateSelectionBounds(new DOMRect(x, y, width, height)); } catch { /* unsupported */ }
        },
        detach(id: string) {
            const entry = _contexts[id];
            if (!entry) return;
            delete _contexts[id];
            // Handing the element back to the DOM's own editing is what makes detaching complete;
            // leaving the context attached would keep swallowing every keystroke.
            try { entry.element.editContext = null; } catch { /* element already gone */ }
        },
        disposeAll() {
            for (const id of Object.keys(_contexts)) butil.editContext.detach(id);
        }
    };
}(BitButil));
