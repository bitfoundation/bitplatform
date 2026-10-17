namespace BitBlazorUI {
    export namespace Utils {
        export const MIN_MOBILE_WIDTH = 320;
        export const MAX_MOBILE_WIDTH = 600;

        export function getBodyWidth() {
            return document.body.offsetWidth;
        }

        // Calls fn at most once per delay. The first call of a window goes at once; with trailing, the latest call
        // made during the window is held and made when the window closes (opening a new one), so a value that comes
        // to rest is reported where it rests rather than where the window began. cancel() drops a held call and
        // closes the window, so the next call goes at once again.
        export function throttle(fn: Function, delay: number, options?: { trailing?: boolean }) {
            const trailing = options?.trailing === true;
            let timeoutId: number | null = null;
            let pendingArgs: any[] | null = null;

            const call = (args: any[]) => {
                try { fn(...args); } catch (e) { console.error("BitBlazorUI.Utils.throttle:", e); }
            };

            const openWindow = () => {
                timeoutId = setTimeout(() => {
                    timeoutId = null;
                    if (!pendingArgs) return;

                    const args = pendingArgs;
                    pendingArgs = null;
                    openWindow();
                    call(args);
                }, delay);
            };

            const throttled = (...args: any[]) => {
                if (timeoutId === null) {
                    if (delay > 0) openWindow();
                    call(args);
                } else if (trailing) {
                    pendingArgs = args;
                }
            };

            throttled.cancel = () => {
                if (timeoutId !== null) clearTimeout(timeoutId);
                timeoutId = null;
                pendingArgs = null;
            };

            return throttled;
        }

        export function isTouchDevice() {
            try {
                const matchMedia = window.matchMedia("(pointer: coarse)").matches;
                const maxTouchPoints = ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
                return matchMedia || maxTouchPoints;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isTouchDevice:", e);
                return false;
            }
        }

        // Whether the viewport can be shrunk by an on-screen keyboard, so that a resize or a short visible band
        // may be the keyboard's doing rather than the window's. It is the PRIMARY pointer being coarse that says
        // so, not touch merely being available: a touch-screen laptop has a touch screen and a physical keyboard,
        // and a window resized there is the window being resized. A phone, a tablet and a convertible folded
        // into its tablet posture all report a coarse primary pointer, whatever the width of their screen.
        export function hasOnScreenKeyboard() {
            try {
                return window.matchMedia("(pointer: coarse)").matches;
            } catch (e) {
                console.error("BitBlazorUI.Utils.hasOnScreenKeyboard:", e);
                return false;
            }
        }

        // Returns the currently visible region of the page. On iOS the on-screen keyboard
        // shrinks the visual viewport without changing window.innerHeight, so relying on
        // window.inner* mispositions fixed elements (e.g. callouts) behind the keyboard.
        // window.visualViewport reflects the real visible area, which we fall back from
        // gracefully on browsers that don't support it.
        export function getViewport() {
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
        export function isEditableElementFocused() {
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

        // True when the focus currently sits inside the given container. The popup components ask before
        // they close, since handing the focus back to the element that opened them is only correct when
        // the focus was theirs to hand back - moving it out of wherever the user put it otherwise.
        export function containsActiveElement(elementId: string) {
            try {
                const container = document.getElementById(elementId);
                if (!container) return false;

                const active = document.activeElement;
                return active != null && active !== document.body && container.contains(active);
            } catch (e) {
                console.error("BitBlazorUI.Utils.containsActiveElement:", e);
                return false;
            }
        }

        // True when the reader has asked for less motion ('prefers-reduced-motion: reduce') and the given
        // element has not been opted back into it: an element inside a subtree marked with bit-fam (which is
        // what BitComponentBase.ForceAnimation renders) keeps its motion, the same way the stylesheets
        // restore the motion tokens there.
        export function prefersReducedMotion(element: HTMLElement) {
            try {
                if (typeof window.matchMedia !== "function") return false;
                if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches) return false;

                return !(element && element.closest && element.closest(".bit-fam"));
            } catch (e) {
                console.error("BitBlazorUI.Utils.prefersReducedMotion:", e);
                return false;
            }
        }

        // Whether the pointer of the device is one that can actually hover, which the interactions that
        // are driven by hovering have to know: a touch screen reports a mouseover for a tap, so a popup
        // opening on hover would fight the tap that is also meant to toggle it.
        export function isHoverDevice() {
            try {
                return window.matchMedia('(hover: hover) and (pointer: fine)').matches;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isHoverDevice:", e);
                return false;
            }
        }

        // Resolves once the exit animation of a surface has played out, so that it is only taken out of the page
        // after it: the animations running on the element and on its direct children (an overlay, a content
        // box), which is where a surface's own movement is. Anything deeper is the content's own business - a
        // spinner inside it runs forever - and so is anything that repeats. `timeout` bounds the wait, so a
        // surface is never kept in the page by an animation that does not end.
        export async function waitForAnimations(elementId: string, timeout: number = 1000) {
            const element = document.getElementById(elementId);
            if (!element || typeof element.getAnimations !== 'function') return;

            const animations = [element, ...Array.from(element.children)]
                .flatMap(e => e.getAnimations())
                .filter(a => a.effect?.getTiming().iterations !== Infinity);

            if (animations.length === 0) return;

            await Promise.race([
                Promise.all(animations.map(a => a.finished.catch(() => { }))),
                new Promise(resolve => setTimeout(resolve, timeout)),
            ]);
        }

        export const _transitionEnds = new Map<string, AbortController>();

        // Tells .NET when a surface has finished sliding in or out. What a component knows on its own is the
        // frame the state changed on, which is the start of the animation rather than the end of it: the
        // content of a closed surface cannot be taken out of the page before it has finished sliding away, and
        // whatever is measured or focused after an opening has to wait for the surface to have arrived.
        // Only the transform is listened for - a surface transitions its opacity and its visibility as well,
        // and all three would report the same one movement - and only on the element itself, so a transition
        // running somewhere in the content is not mistaken for the surface arriving.
        export function setupTransitionEnd(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeTransitionEnd(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            element.addEventListener('transitionend', (e: TransitionEvent) => {
                if (e.target !== element || e.propertyName !== 'transform') return;

                dotnetObj.invokeMethodAsync('OnTransitionEnd');
            }, { signal: controller.signal });

            Utils._transitionEnds.set(elementId, controller);
        }

        export function disposeTransitionEnd(elementId: string) {
            const controller = Utils._transitionEnds.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._transitionEnds.delete(elementId);
        }

        export function setProperty(element: Record<string, any>, property: string, value: any): void {
            if (!element) return;

            try {
                element[property] = value;
            } catch (e) { console.error("BitBlazorUI.Utils.setProperty:", e); }
        }

        export function getProperty(element: Record<string, any>, property: string): string | null {
            if (!element) return null;

            try {
                return element[property].toString();
            } catch (e) {
                console.error("BitBlazorUI.Utils.getProperty:", e);
                return '';
            }
        }

        // How far the browser has got with an img: 2 when it has finished it, 1 while it is still painting one it
        // already knows the size of, 0 when there is nothing of it on screen yet (or it is broken - complete too,
        // but with nothing decoded).
        export function getImageProgress(element: HTMLImageElement): number {
            if (!element) return 0;

            if (element.naturalWidth > 0) return element.complete ? 2 : 1;

            return 0;
        }

        export function getChildrenAttributes(containerId: string, attribute: string): string[] {
            const container = document.getElementById(containerId);
            if (!container) return [];

            try {
                return Array.from(container.querySelectorAll(`[${attribute}]`)).map(e => e.getAttribute(attribute) || '');
            } catch (e) {
                console.error("BitBlazorUI.Utils.getChildrenAttributes:", e);
                return [];
            }
        }

        export function getBoundingClientRect(element: HTMLElement): Partial<DOMRect> {
            if (!element) return {};

            try {
                return element.getBoundingClientRect?.();
            } catch (e) {
                console.error("BitBlazorUI.Utils.getBoundingClientRect:", e);
                return {};
            }
        }

        export function setStyle(element: HTMLElement, key: string, value: string) {
            if (!element || !element.style) return;

            try {
                (element.style as any)[key] = value;
            } catch (e) { console.error("BitBlazorUI.Utils.setStyle:", e); }
        }

        export function selectText(element: HTMLInputElement) {
            if (!element) return;

            try {
                element.select();
            } catch (e) { console.error("BitBlazorUI.Utils.selectText:", e); }
        }

        // Measures how much room a single-line list of children needs against the room it has, so a
        // component can decide how many of them to keep. `content` is what the children take in total
        // and `available` is the width of the container, both in pixels, and `widths` carries the
        // children in DOM order so the caller can tell how much room dropping one of them frees.
        // The content is measured off the children rather than off the scrollWidth of the container,
        // since that one never drops below the clientWidth: a trail that fits would report no room to
        // spare and the caller could never tell that a child it dropped has room to come back to.
        // It is the extent from the leftmost edge to the rightmost one rather than the sum of the
        // widths, so that whatever sits between the children (a gap, a margin, a whitespace text node)
        // is counted as the room it takes; the sum would report a trail that overflows as one that fits.
        export function getOverflowMetrics(containerId: string, childSelector: string) {
            const container = document.getElementById(containerId);
            if (!container) return null;

            try {
                const rects = (Array.from(container.querySelectorAll(childSelector)) as HTMLElement[])
                    .map(el => el.getBoundingClientRect());

                const left = Math.min(...rects.map(rect => rect.left));
                const right = Math.max(...rects.map(rect => rect.right));

                return {
                    available: container.clientWidth,
                    content: rects.length === 0 ? 0 : right - left,
                    widths: rects.map(rect => rect.width)
                };
            } catch (e) {
                console.error("BitBlazorUI.Utils.getOverflowMetrics:", e);
                return null;
            }
        }
    }
}
