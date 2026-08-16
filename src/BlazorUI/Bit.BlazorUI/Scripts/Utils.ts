namespace BitBlazorUI {
    export class Utils {
        public static MIN_MOBILE_WIDTH = 320;
        public static MAX_MOBILE_WIDTH = 600;

        public static getBodyWidth() {
            return document.body.offsetWidth;
        }

        public static throttle(fn: Function, delay: number) {
            let timeoutItd: number | null = null;

            return (...args: any[]) => {
                if (timeoutItd === null) {
                    try { fn(...args); } catch (e) { console.error("BitBlazorUI.Utils.throttle:", e); }
                    if (delay > 0) {
                        timeoutItd = setTimeout(() => {
                            timeoutItd = null;
                        }, delay);
                    }
                }
            };
        }

        public static isTouchDevice() {
            try {
                const matchMedia = window.matchMedia("(pointer: coarse)").matches;
                const maxTouchPoints = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
                return matchMedia || maxTouchPoints;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isTouchDevice:", e);
                return false;
            }
        }

        // Returns the currently visible region of the page. On iOS the on-screen keyboard
        // shrinks the visual viewport without changing window.innerHeight, so relying on
        // window.inner* mispositions fixed elements (e.g. callouts) behind the keyboard.
        // window.visualViewport reflects the real visible area, which we fall back from
        // gracefully on browsers that don't support it.
        public static getViewport() {
            const vv = window.visualViewport;
            return {
                width: vv?.width ?? window.innerWidth,
                height: vv?.height ?? window.innerHeight,
                offsetLeft: vv?.offsetLeft ?? 0,
                offsetTop: vv?.offsetTop ?? 0,
                layoutHeight: window.innerHeight,
            };
        }

        // Detects whether an editable element (input/textarea/contenteditable) currently has
        // focus. Used to avoid dismissing an open callout when iOS fires a scroll event as a
        // side effect of showing the virtual keyboard.
        public static isEditableElementFocused() {
            try {
                const el = document.activeElement as HTMLElement | null;
                if (!el) return false;
                const tag = el.tagName;
                return tag === 'INPUT' || tag === 'TEXTAREA' || el.isContentEditable === true;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isEditableElementFocused:", e);
                return false;
            }
        }

        public static setProperty(element: Record<string, any>, property: string, value: any): void {
            if (!element) return;

            try {
                element[property] = value;
            } catch (e) { console.error("BitBlazorUI.Utils.setProperty:", e); }
        }

        public static getProperty(element: Record<string, any>, property: string): string | null {
            if (!element) return null;

            try {
                return element[property].toString();
            } catch (e) {
                console.error("BitBlazorUI.Utils.getProperty:", e);
                return '';
            }
        }

        public static getChildrenAttributes(containerId: string, attribute: string): string[] {
            const container = document.getElementById(containerId);
            if (!container) return [];

            try {
                return Array.from(container.querySelectorAll(`[${attribute}]`)).map(e => e.getAttribute(attribute) || '');
            } catch (e) {
                console.error("BitBlazorUI.Utils.getChildrenAttributes:", e);
                return [];
            }
        }

        public static getBoundingClientRect(element: HTMLElement): Partial<DOMRect> {
            if (!element) return {};

            try {
                return element.getBoundingClientRect?.();
            } catch (e) {
                console.error("BitBlazorUI.Utils.getBoundingClientRect:", e);
                return {};
            }
        }

        public static scrollElementIntoView(targetElementId: string) {
            const element = document.getElementById(targetElementId);
            if (!element) return;

            try {
                element.scrollIntoView({
                    behavior: "smooth",
                    block: "start",
                    inline: "nearest"
                });
            } catch (e) { console.error("BitBlazorUI.Utils.scrollElementIntoView:", e); }
        }

        // Registers a wheel listener on the element that suppresses the browser's default scrolling
        // while the Shift key is held (used by spin controls that turn Shift+wheel into a value
        // change; without it a scrollable ancestor would also scroll horizontally). Passing active =
        // false turns the suppression off in place; the listener itself is garbage-collected with the
        // element. The listener must be registered non-passive to be allowed to call preventDefault.
        public static registerPreventShiftWheel(element: HTMLElement, active: boolean) {
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
        // ones of the components sitting inside the element. Calling it again updates the active
        // flag in place, so no separate unregister call is needed - the listener is
        // garbage-collected with the element itself.
        public static registerPreventPointerDown(element: HTMLElement, active: boolean) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventPointerDown = active;

                if (el.__bitPreventPointerDownRegistered) return;
                el.__bitPreventPointerDownRegistered = true;

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
        public static registerPreventWheel(element: HTMLElement, active: boolean, verticalOnly: boolean) {
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
        public static registerPreventKeys(element: HTMLElement, keys: string[]) {
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

        public static selectText(element: HTMLInputElement) {
            if (!element) return;

            try {
                element.select();
            } catch (e) { console.error("BitBlazorUI.Utils.selectText:", e); }
        }

        // Everything that can hold the focus inside a container. A roving tabindex takes every item of a grid
        // but one out of the tab sequence, which is why tabindex="-1" is excluded here. The controls a header
        // or footer template brings along are part of the container too, so the whole set of natively
        // focusable elements is listed rather than only the ones the components render themselves.
        private static readonly _focusables =
            'a[href]:not([tabindex="-1"]), button:not([disabled]):not([tabindex="-1"]), ' +
            'input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), ' +
            'textarea:not([disabled]):not([tabindex="-1"]), ' +
            '[contenteditable]:not([contenteditable="false"]):not([tabindex="-1"]), ' +
            '[tabindex]:not([tabindex="-1"])';

        // Keeps Tab and Shift+Tab cycling inside a container, which is what a popup that reports itself a modal
        // dialog has to do: the tab order runs on into the page behind it otherwise, leaving the focus somewhere
        // an overlay swallows every click that could bring it back. The keydown is left alone where the focus is
        // not on the edge of the container, so tabbing within it moves as it normally would.
        public static wrapFocus(root: HTMLElement, e: KeyboardEvent) {
            if (!root) return;

            // A hidden element is not a place the focus can land, and a callout carries parts that are only
            // rendered for some of its states.
            const focusables = Array.from(root.querySelectorAll<HTMLElement>(Utils._focusables))
                .filter(el => el.offsetWidth > 0 || el.offsetHeight > 0 || el.getClientRects().length > 0);

            if (focusables.length === 0) return;

            const first = focusables[0];
            const last = focusables[focusables.length - 1];
            const active = document.activeElement;

            if (e.shiftKey && active === first) {
                last.focus();
                e.preventDefault();
            } else if (!e.shiftKey && active === last) {
                first.focus();
                e.preventDefault();
            }
        }

        // Measures how much room a single-line list of children needs against the room it has, so a
        // component can decide how many of them to keep. `content` is what the children take in total
        // and `available` is the width of the container, both in pixels, and `widths` carries the
        // children in DOM order so the caller can tell how much room dropping one of them frees.
        public static getOverflowMetrics(containerId: string, childSelector: string) {
            const container = document.getElementById(containerId);
            if (!container) return null;

            try {
                return {
                    available: container.clientWidth,
                    content: container.scrollWidth,
                    widths: (Array.from(container.querySelectorAll(childSelector)) as HTMLElement[])
                        .map(el => el.getBoundingClientRect().width)
                };
            } catch (e) {
                console.error("BitBlazorUI.Utils.getOverflowMetrics:", e);
                return null;
            }
        }

        // Moves the focus between the items of a popup (a menu, an overflow list, ...) that a component
        // drives from its keydown handlers. The items are the elements of `container` matching `selector`,
        // in DOM order, minus the disabled ones. `mode` is one of first/last/next/prev/char, where next
        // and prev wrap around and char jumps to the next item whose text starts with `char`.
        public static focusItem(containerId: string, selector: string, mode: string, char: string | null) {
            const container = document.getElementById(containerId);
            if (!container) return;

            try {
                const items = (Array.from(container.querySelectorAll(selector)) as HTMLElement[])
                    .filter(el => !(el as HTMLButtonElement).disabled && el.getAttribute('aria-disabled') !== 'true');
                if (items.length === 0) return;

                const current = items.indexOf(document.activeElement as HTMLElement);
                let index = -1;

                if (mode === 'first') {
                    index = 0;
                } else if (mode === 'last') {
                    index = items.length - 1;
                } else if (mode === 'next') {
                    index = current < 0 ? 0 : (current + 1) % items.length;
                } else if (mode === 'prev') {
                    index = current < 0 ? items.length - 1 : (current - 1 + items.length) % items.length;
                } else if (mode === 'char' && char) {
                    const c = char.toLowerCase();
                    const start = current < 0 ? 0 : current + 1;
                    for (let i = 0; i < items.length; i++) {
                        const candidate = (start + i) % items.length;
                        if ((items[candidate].textContent || '').trim().toLowerCase().indexOf(c) === 0) {
                            index = candidate;
                            break;
                        }
                    }
                }

                if (index > -1) {
                    items[index].focus();
                }
            } catch (e) {
                console.error("BitBlazorUI.Utils.focusItem:", e);
            }
        }

        public static setStyle(element: HTMLElement, key: string, value: string) {
            if (!element || !element.style) return;

            try {
                (element.style as any)[key] = value;
            } catch (e) { console.error("BitBlazorUI.Utils.setStyle:", e); }
        }

        public static toggleOverflow(selector: string | HTMLElement, isOpen: boolean) {
            const element = selector instanceof HTMLElement ? selector : document.querySelector(selector) as HTMLElement;

            if (!element) return 0;

            try {
                element.style.overflow = isOpen ? "hidden" : "";
                return element.scrollTop;
            } catch (e) {
                console.error("BitBlazorUI.Utils.toggleOverflow:", e);
                return 0;
            }
        }

        public static uuidv4(): string {
            try {
                const result = this.guidTemplate.replace(/[018]/g, (c) => {
                    const n = +c;
                    const random = crypto.getRandomValues(new Uint8Array(1));
                    const result = (n ^ random[0] & 15 >> n / 4);
                    return result.toString(16);
                });
                return result;
            } catch (e) {
                console.error("BitBlazorUI.Utils.uuidv4:", e);
                return '';
            }
        }
        // https://stackoverflow.com/questions/105034/how-to-create-a-guid-uuid/#2117523
        private static guidTemplate = '10000000-1000-4000-8000-100000000000';
    }
}