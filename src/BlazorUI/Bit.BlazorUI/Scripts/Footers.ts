namespace BitBlazorUI {
    export class Footers {
        private static _entries = new Map<string, { handler: () => void, target: HTMLElement | Window }>();

        // Scroll deltas below this many pixels are ignored, so the rubber-banding of touch devices and
        // the sub-pixel jitter of a trackpad cannot flip the footer back and forth on every frame.
        private static readonly THRESHOLD = 4;

        // Slides the footer out of the view while the page is scrolled down and brings it back while it is
        // scrolled up (the classic "reveal" behavior of an app bar). The state lives here and only crosses
        // the interop boundary when it actually flips, so a scroll never costs more than a comparison.
        public static setup(id: string, dotnetObj: DotNetObject) {
            Footers.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // The scroller is not always the window: an app shell (and any pane with its own overflow)
            // scrolls its own box, and a scroll event on an element does not bubble to the window.
            const target = Footers.scrollParent(element);

            let hidden = false;
            let lastY = Footers.scrollTop(target);
            let ticking = false;

            const evaluate = () => {
                ticking = false;

                const y = Footers.scrollTop(target);
                const delta = y - lastY;

                let next = hidden;

                if (Math.abs(delta) >= Footers.THRESHOLD) {
                    next = delta > 0;
                    lastY = y;
                }

                // The two ends always show the footer: at the top there is nothing to make room for, and
                // at the end the footer is the content the user scrolled down to reach.
                if (y <= 0 || Footers.atEnd(target)) {
                    next = false;
                    lastY = y;
                }

                if (next === hidden) return;

                hidden = next;

                dotnetObj.invokeMethodAsync('OnRevealChange', hidden);
            };

            // rAF coalescing keeps a burst of scroll events down to one evaluation per painted frame.
            const handler = () => {
                if (ticking) return;

                ticking = true;
                requestAnimationFrame(evaluate);
            };

            target.addEventListener('scroll', handler, { passive: true });
            window.addEventListener('resize', handler, { passive: true });

            Footers._entries.set(id, { handler, target });
        }

        public static dispose(id: string) {
            const entry = Footers._entries.get(id);
            if (!entry) return;

            entry.target.removeEventListener('scroll', entry.handler);
            window.removeEventListener('resize', entry.handler);

            Footers._entries.delete(id);
        }

        private static scrollParent(element: HTMLElement): HTMLElement | Window {
            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const overflowY = getComputedStyle(node).overflowY;

                if (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') return node;

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
