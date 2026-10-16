namespace BitBlazorUI {
    export namespace Utils {

        // True when the target of a key sits on something inside the given container that consumes the
        // arrow keys (and Home/End) on its own: an editable field moves its caret with them, and a slider,
        // a list, a radio group or a grid moves its own selection. So does a dialog, a panel or a modal
        // rendered inside the container: every key pressed in one is its own (isInNestedSurface). The
        // container itself does not count, and neither does an element outside of it.
        export function isKeyConsumer(container: HTMLElement, target: HTMLElement) {
            if (target === container || !container.contains(target)) return false;

            if (target.isContentEditable) return true;

            if (Utils.isInNestedSurface(target, container)) return true;

            const tag = target.tagName;
            if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
            if (tag === 'INPUT') {
                const type = ((target as HTMLInputElement).type || '').toLowerCase();
                if (['button', 'submit', 'reset', 'checkbox', 'image', 'file', 'color'].indexOf(type) < 0) return true;
            }

            const owner = target.closest('[role="slider"],[role="spinbutton"],[role="textbox"],[role="searchbox"],[role="combobox"],' +
                                         '[role="listbox"],[role="menu"],[role="menubar"],[role="tablist"],[role="radiogroup"],' +
                                         '[role="grid"],[role="treegrid"],[role="tree"],[role="scrollbar"]');

            return owner != null && owner !== container && container.contains(owner);
        }

        // Registers a keydown listener on an element that navigates with the given keys itself (a carousel,
        // for one), which takes each of those keys away from the browser and hands it to the
        // OnNavigationKey method of the .NET object. Both halves are decided here, at once and from the
        // target of the event, so the element never moves without its default being suppressed or the
        // other way around, and a key costs a call to .NET only when it is actually one to act on.
        // A key is left alone when it carries a modifier (a browser shortcut), when a control inside the
        // element consumes it (isKeyConsumer), and when something deeper already took it (a nested
        // element of the same kind, whose listener runs first).
        // With the key, .NET is told where the focus was: the data-bit-key-origin value (and the id) of the
        // outermost element carrying one between the target and the element, which is how the element
        // tells its own controls apart without a focus event per control. Calling it again updates the
        // keys and the .NET object in place, and an empty key list turns it off, so no separate
        // unregister call is needed - the listener is garbage-collected with the element itself.
        export function registerNavigationKeys(element: HTMLElement, keys: string[], dotnetObj: DotNetObject) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitNavigationKeys = keys || [];
                el.__bitNavigationKeysDotnetObj = dotnetObj;

                if (el.__bitNavigationKeysRegistered) return;
                el.__bitNavigationKeysRegistered = true;

                element.addEventListener('keydown', (e: KeyboardEvent) => {
                    const el = element as any;
                    const currentKeys = el.__bitNavigationKeys as string[];

                    if (!currentKeys || currentKeys.indexOf(e.key) < 0) return;
                    if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;
                    if (e.defaultPrevented) return;

                    const target = e.target instanceof HTMLElement ? e.target : null;

                    if (target && Utils.isKeyConsumer(element, target)) return;

                    e.preventDefault();

                    let origin: HTMLElement | null = null;
                    for (let n = target; n && n !== element; n = n.parentElement) {
                        if (n.hasAttribute('data-bit-key-origin')) origin = n;
                    }

                    const dotnet = el.__bitNavigationKeysDotnetObj as DotNetObject | undefined;

                    dotnet?.invokeMethodAsync('OnNavigationKey', e.key,
                                              origin?.getAttribute('data-bit-key-origin') ?? null,
                                              origin?.id || null)
                          .catch(err => console.error("BitBlazorUI.Utils.registerNavigationKeys:", err));
                });
            } catch (e) { console.error("BitBlazorUI.Utils.registerNavigationKeys:", e); }
        }

        export const _preventedKeys = new Map<string, AbortController>();

        // Suppresses the default behavior (page scrolling) of the given keys on an element, for the
        // components whose keyboard logic runs in Blazor keydown handlers, which cannot decide to
        // preventDefault per key. Registering again on the same element replaces the previous keys.
        export function preventDefaultKeys(elementId: string, keys: string[]) {
            Utils.disposePreventDefaultKeys(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            // A modified key is a shortcut of the browser or of the operating system rather than the key
            // the component handles, so its default action is left alone.
            element.addEventListener('keydown', (e: KeyboardEvent) => {
                if (keys.indexOf(e.key) !== -1 && !e.shiftKey && !e.ctrlKey && !e.altKey && !e.metaKey) {
                    e.preventDefault();
                }
            }, { signal: controller.signal });

            Utils._preventedKeys.set(elementId, controller);
        }

        export function disposePreventDefaultKeys(elementId: string) {
            const controller = Utils._preventedKeys.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._preventedKeys.delete(elementId);
        }

        // Registers a wheel listener on the element that suppresses the browser's default scrolling
        // while the Shift key is held (used by spin controls that turn Shift+wheel into a value
        // change; without it a scrollable ancestor would also scroll horizontally). Passing active =
        // false turns the suppression off in place; the listener itself is garbage-collected with the
        // element. The listener must be registered non-passive to be allowed to call preventDefault.
        export function registerPreventShiftWheel(element: HTMLElement, active: boolean) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventShiftWheel = active;

                if (el.__bitPreventShiftWheelRegistered) return;
                el.__bitPreventShiftWheelRegistered = true;

                element.addEventListener('wheel', (e: WheelEvent) => {
                    if ((element as any).__bitPreventShiftWheel && e.shiftKey) {
                        e.preventDefault();
                    }
                }, { passive: false });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventShiftWheel:", e); }
        }

        // Registers a pointerdown listener on the element that suppresses the browser's default
        // action (dragging an image, selecting text) so the element can be dragged by the pointer
        // instead. A pointerdown on a control inside the element keeps its default action, since
        // preventing it would keep the control from taking the focus (and would keep the text of an
        // input inside it from being selected with the pointer). The event itself is always left to
        // travel on: Blazor dispatches pointerdown from a single listener on the document, so
        // stopping it here would take it away from every Blazor handler in the tree, including the
        // ones of the components sitting inside the element. Calling it again updates the flags in
        // place, so no separate unregister call is needed - the listeners are garbage-collected with
        // the element itself.
        // While it is active, the native drag of a link (or of anything else draggable) inside the
        // element is cancelled too, since it would swallow the pointer events of the drag the element
        // performs itself. And with a positive clickThreshold, the click that ends a drag which
        // travelled further than that is swallowed before anything else sees it, so letting go of a
        // slide that was dragged over a link (or a button) does not also follow it. With a clickAxis
        // ('x' or 'y') only the travel along that axis counts, and a drag that went further across it
        // than along it is not one either, which is how the element itself tells a drag from a scroll.
        export function registerPreventPointerDown(element: HTMLElement, active: boolean, clickThreshold?: number, clickAxis?: string) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventPointerDown = active;
                el.__bitPreventPointerDownClickThreshold = clickThreshold || 0;
                el.__bitPreventPointerDownClickAxis = clickAxis || null;

                if (el.__bitPreventPointerDownRegistered) return;
                el.__bitPreventPointerDownRegistered = true;

                // Where the pointer went down is recorded in the capture phase, so a control inside the
                // element that stops the pointerdown from bubbling cannot leave a stale position behind
                // for the click to be measured against.
                element.addEventListener('pointerdown', (e: PointerEvent) => {
                    const el = element as any;
                    el.__bitPointerDownX = e.clientX;
                    el.__bitPointerDownY = e.clientY;
                }, true);

                element.addEventListener('pointerdown', (e: PointerEvent) => {
                    if (!(element as any).__bitPreventPointerDown) return;

                    if (e.target instanceof Element) {
                        // The lookup is bounded by the element itself, since a control the element
                        // happens to sit inside of (a card that is a link, for one) says nothing
                        // about what was pressed within it. An explicit contenteditable="false"
                        // marks content that is not editable after all, so it is not a control here.
                        const control = e.target.closest(
                            'button,a,input,textarea,select,[contenteditable]:not([contenteditable="false"])');

                        if (control && element !== control && element.contains(control)) return;
                    }

                    e.preventDefault();
                });

                element.addEventListener('dragstart', (e: DragEvent) => {
                    if (!(element as any).__bitPreventPointerDown) return;

                    e.preventDefault();
                });

                element.addEventListener('click', (e: MouseEvent) => {
                    const el = element as any;
                    const threshold = el.__bitPreventPointerDownClickThreshold as number;
                    const downX = el.__bitPointerDownX as number | undefined;
                    const downY = el.__bitPointerDownY as number | undefined;

                    // The position belongs to the one click it started, so it is used up here.
                    el.__bitPointerDownX = el.__bitPointerDownY = undefined;

                    if (!el.__bitPreventPointerDown || !(threshold > 0)) return;
                    if (downX === undefined || downY === undefined) return;

                    // A click raised from the keyboard carries no pointer travel of its own, so only a
                    // click that ends a pointer drag longer than the threshold is taken away.
                    if (e.detail === 0) return;

                    const dx = Math.abs(e.clientX - downX);
                    const dy = Math.abs(e.clientY - downY);
                    const axis = el.__bitPreventPointerDownClickAxis as string | null;

                    const along = axis === 'x' ? dx : axis === 'y' ? dy : Math.max(dx, dy);
                    const across = axis === 'x' ? dy : axis === 'y' ? dx : 0;

                    if (along <= threshold || across > along) return;

                    e.preventDefault();
                    e.stopPropagation();
                }, true);
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventPointerDown:", e); }
        }

        // Registers a wheel listener on the element that suppresses the scrolling of the page while
        // the element handles the wheel itself. It is registered here rather than through Blazor's
        // preventDefault directive because that one goes through a delegated listener the browser
        // treats as passive, which makes preventing a wheel a no-op before net10.0. Calling it again
        // updates the flags in place, so no separate unregister call is needed - the listener is
        // garbage-collected with the element itself. An element that never turns the suppression
        // on gets no listener at all, since a non-passive wheel listener is not free to the browser.
        // Only the wheel the element actually consumes is taken from the page: with verticalOnly a
        // scroll that runs mostly sideways is left alone, the same way the element leaves it alone.
        export function registerPreventWheel(element: HTMLElement, active: boolean, verticalOnly: boolean) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventWheel = active;
                el.__bitPreventWheelVerticalOnly = verticalOnly;

                if (active === false) return;
                if (el.__bitPreventWheelRegistered) return;
                el.__bitPreventWheelRegistered = true;

                element.addEventListener('wheel', (e: WheelEvent) => {
                    const el = element as any;

                    if (!el.__bitPreventWheel) return;
                    if (el.__bitPreventWheelVerticalOnly && Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;

                    e.preventDefault();
                }, { passive: false });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventWheel:", e); }
        }

        // Registers a keydown listener on the element that suppresses the browser's default action
        // for the given keys (e.g. PageUp/PageDown scrolling the page while a spinbutton handles
        // them as value changes). Calling it again updates the key list in place, and an empty list
        // effectively disables the suppression, so no separate unregister call is needed - the
        // listener is garbage-collected with the element itself.
        // A key typed into an editable element inside the container (an input in a carousel slide,
        // for example) belongs to that element (the arrow keys move its caret), so its default
        // action is left alone. The event itself still travels on: Blazor dispatches keydown from a
        // single listener on the document, so stopping it here would take the key away from the
        // editable element's own handler too, which is the one it was being left to.
        export function registerPreventKeys(element: HTMLElement, keys: string[]) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventKeys = keys || [];

                if (el.__bitPreventKeysRegistered) return;
                el.__bitPreventKeysRegistered = true;

                element.addEventListener('keydown', (e: KeyboardEvent) => {
                    const currentKeys = (element as any).__bitPreventKeys as string[];
                    if (!currentKeys || currentKeys.indexOf(e.key) < 0 || e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;

                    const target = e.target;
                    if (target !== element && target instanceof Element &&
                        ((target as HTMLElement).isContentEditable || /^(input|textarea|select)$/i.test(target.tagName))) {
                        return;
                    }

                    e.preventDefault();
                });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventKeys:", e); }
        }

        // Makes an element carrying the button role activate from the keyboard the way a real button
        // does: Enter clicks it as the key goes down, Space as the key comes back up, and neither key
        // scrolls the page. Only a key pressed on the element itself counts - one pressed on a control
        // inside it (a button a template brought along) already clicks that control, and the click
        // bubbles up to the element's own handler, so answering the key here too would act twice.
        // The listeners are garbage-collected with the element, so no unregister call is needed.
        export function registerButtonKeys(element: HTMLElement) {
            if (!element) return;

            try {
                const el = element as any;
                if (el.__bitButtonKeysRegistered) return;
                el.__bitButtonKeysRegistered = true;

                const plain = (e: KeyboardEvent) => e.target === element && !e.ctrlKey && !e.altKey && !e.metaKey;

                element.addEventListener('keydown', (e: KeyboardEvent) => {
                    if (!plain(e)) return;

                    if (e.key === 'Enter') {
                        e.preventDefault();
                        element.click();
                    } else if (e.key === ' ') {
                        e.preventDefault();
                        el.__bitButtonKeysSpace = true;
                    }
                });

                element.addEventListener('keyup', (e: KeyboardEvent) => {
                    if (e.key !== ' ' || !el.__bitButtonKeysSpace) return;
                    el.__bitButtonKeysSpace = false;

                    if (!plain(e)) return;

                    e.preventDefault();
                    element.click();
                });

                element.addEventListener('blur', () => el.__bitButtonKeysSpace = false);
            } catch (e) { console.error("BitBlazorUI.Utils.registerButtonKeys:", e); }
        }
    }
}
