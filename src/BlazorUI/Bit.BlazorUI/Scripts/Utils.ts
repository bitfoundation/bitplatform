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
        // preventing it would keep the control from taking the focus, but it is stopped from
        // bubbling on to the element's own handling: pressing a button inside a slide is a press of
        // that button, not the start of a drag of the slide. Calling it again updates the active
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
                    if (e.target instanceof Element && e.target.closest('button,a,input,textarea,select,[contenteditable]')) {
                        e.stopPropagation();
                        return;
                    }
                    e.preventDefault();
                });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventPointerDown:", e); }
        }

        // Registers a wheel listener on the element that suppresses the scrolling of the page while
        // the element handles the wheel itself. It is registered here rather than through Blazor's
        // preventDefault directive because that one goes through a delegated listener the browser
        // treats as passive, which makes preventing a wheel a no-op before net10.0. Calling it again
        // updates the active flag in place, so no separate unregister call is needed - the listener
        // is garbage-collected with the element itself.
        public static registerPreventWheel(element: HTMLElement, active: boolean) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventWheel = active;

                if (el.__bitPreventWheelRegistered) return;
                el.__bitPreventWheelRegistered = true;

                element.addEventListener('wheel', (e: WheelEvent) => {
                    if ((element as any).__bitPreventWheel) {
                        e.preventDefault();
                    }
                }, { passive: false });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventWheel:", e); }
        }

        // Registers a keydown listener on the element that suppresses the browser's default action
        // for the given keys (e.g. PageUp/PageDown scrolling the page while a spinbutton handles
        // them as value changes). Calling it again updates the key list in place, and an empty list
        // effectively disables the suppression, so no separate unregister call is needed - the
        // listener is garbage-collected with the element itself.
        // A key typed into an editable element inside the container (an input in a carousel slide,
        // for example) belongs to that element (the arrow keys move its caret), so it is neither
        // suppressed nor allowed to bubble on to the container's own key handling.
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
                        e.stopPropagation();
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