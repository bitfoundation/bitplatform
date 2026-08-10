namespace BitBlazorUI {
    export class Footers {
        private static _entries = new Map<string, {
            element: HTMLElement,
            scrollHandler: () => void,
            focusHandler: () => void,
            target: HTMLElement | Window,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number }
        }>();

        // Scroll deltas below this many pixels are ignored, so the rubber-banding of touch devices and
        // the sub-pixel jitter of a trackpad cannot flip the footer back and forth on every frame.
        private static readonly THRESHOLD = 4;

        // Slides the footer out of the view while the page is scrolled down and brings it back while it is
        // scrolled up (the classic "reveal" behavior of an app bar). The state lives here and only crosses
        // the interop boundary when it actually flips, so a scroll never costs more than a comparison.
        public static setup(id: string, dotnetObj: DotNetObject, revealOffset: number) {
            Footers.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // The scroller is not always the window: an app shell (and any pane with its own overflow)
            // scrolls its own box, and a scroll event on an element does not bubble to the window.
            const target = Footers.scrollParent(element);

            // A negative offset would keep the footer hidden at the very top of the scroller, where the
            // first rule below is what has to win.
            const offset = Math.max(0, revealOffset || 0);

            let hidden = false;
            let lastY = Footers.scrollTop(target);

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            const apply = (next: boolean) => {
                if (next === hidden) return;

                hidden = next;

                dotnetObj.invokeMethodAsync('OnRevealChange', hidden);
            };

            const evaluate = () => {
                frame.handle = 0;

                const y = Footers.scrollTop(target);
                const delta = y - lastY;

                let next = hidden;

                if (Math.abs(delta) >= Footers.THRESHOLD) {
                    next = delta > 0;
                    lastY = y;
                }

                // The two ends always show the footer: within the offset at the top there is nothing to
                // make room for yet, and at the end the footer is the content the user scrolled down to
                // reach. The offset is what keeps a footer from flickering away on the first few pixels
                // of a scroll that has barely started.
                if (y <= offset || Footers.atEnd(target)) {
                    next = false;
                    lastY = y;
                }

                apply(next);
            };

            // rAF coalescing keeps a burst of scroll events down to one evaluation per painted frame.
            const scrollHandler = () => {
                if (frame.handle) return;

                frame.handle = requestAnimationFrame(evaluate);
            };

            // A hidden footer is only translated out of the view, so everything inside it is still in the
            // tab order: a keyboard user tabbing past the content lands on a control they cannot see.
            // Revealing the footer as soon as anything inside it takes the focus keeps that control
            // visible, and the scroll baseline is re-read so the next scroll is measured from here.
            const focusHandler = () => {
                lastY = Footers.scrollTop(target);

                apply(false);
            };

            target.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', scrollHandler, { passive: true });
            element.addEventListener('focusin', focusHandler);

            Footers._entries.set(id, { element, scrollHandler, focusHandler, target, frame });
        }

        public static dispose(id: string) {
            const entry = Footers._entries.get(id);
            if (!entry) return;

            entry.target.removeEventListener('scroll', entry.scrollHandler);
            window.removeEventListener('resize', entry.scrollHandler);
            entry.element.removeEventListener('focusin', entry.focusHandler);

            // A frame scheduled by the last scroll before the disposal would still evaluate and call back
            // into a component that is on its way out, so it is dropped along with the listeners.
            if (entry.frame.handle) {
                cancelAnimationFrame(entry.frame.handle);

                entry.frame.handle = 0;
            }

            Footers._entries.delete(id);
        }

        private static scrollParent(element: HTMLElement): HTMLElement | Window {
            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const overflowY = getComputedStyle(node).overflowY;

                // A scrollable overflow that has nothing to scroll is not the scroller of the footer: it
                // never fires a scroll event and it reads as scrolled to its end forever, which would pin
                // the footer revealed. Walking past it lands on the box that really scrolls.
                if ((overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') &&
                    node.scrollHeight > node.clientHeight) return node;

                node = node.parentElement;
            }

            return window;
        }

        private static scrollTop(target: HTMLElement | Window): number {
            if (target === window) return window.scrollY || document.documentElement.scrollTop || 0;

            return (target as HTMLElement).scrollTop;
        }

        private static atEnd(target: HTMLElement | Window): boolean {
            // A one pixel slack absorbs the rounding of a fractional device pixel ratio, which otherwise
            // keeps a viewport that is scrolled all the way down one hair short of its own height.
            if (target === window) {
                const doc = document.documentElement;
                return window.scrollY + window.innerHeight >= doc.scrollHeight - 1;
            }

            const el = target as HTMLElement;
            return el.scrollTop + el.clientHeight >= el.scrollHeight - 1;
        }
    }
}
