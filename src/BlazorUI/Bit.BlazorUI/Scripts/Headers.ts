namespace BitBlazorUI {
    export class Headers {
        private static _entries = new Map<string, {
            element: HTMLElement,
            scrollHandler: () => void,
            layoutHandler: () => void,
            focusHandler: () => void,
            // The scroller is held in a box as well, since it is re-resolved whenever the layout changes
            // and dispose has to take the scroll listener off the target it is bound to at that moment.
            target: { current: HTMLElement | Window },
            observer?: ResizeObserver,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number }
        }>();

        // Scroll deltas below this many pixels are ignored, so the rubber-banding of touch devices and
        // the sub-pixel jitter of a trackpad cannot flip the header back and forth on every frame.
        private static readonly THRESHOLD = 4;

        // Drives the two scroll behaviors of the header: sliding it out of the view while the page is
        // scrolled down and bringing it back while it is scrolled up (the classic "reveal" of an app bar),
        // and lifting it off the content once the page has left its top (the "elevate on scroll" shadow).
        // Both states live here and only cross the interop boundary when they actually flip, so a scroll
        // never costs more than a comparison.
        public static setup(id: string, dotnetObj: DotNetObject, revealOffset: number, elevateOffset: number, reveal: boolean, elevate: boolean) {
            Headers.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // The scroller is not always the window: an app shell (and any pane with its own overflow)
            // scrolls its own box, and a scroll event on an element does not bubble to the window.
            const target = { current: Headers.scrollParent(element) };

            // A negative offset would keep the header hidden at the very top of the scroller, where the
            // rule below is what has to win.
            const offset = Math.max(0, revealOffset || 0);
            const elevation = Math.max(0, elevateOffset || 0);

            let hidden = false;
            let scrolled = false;
            let lastY = Headers.scrollTop(target.current);

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            const applyHidden = (next: boolean) => {
                if (!reveal || next === hidden) return;

                hidden = next;

                // The reference is disposed before the listeners are, so a flip of the very last frame can
                // land on a dead reference. The rejection is consumed here rather than left to surface as an
                // unhandled one in the console of an application that did nothing wrong.
                dotnetObj.invokeMethodAsync('OnRevealChange', hidden).catch(() => { });
            };

            const applyScrolled = (next: boolean) => {
                if (!elevate || next === scrolled) return;

                scrolled = next;

                dotnetObj.invokeMethodAsync('OnScrollChange', scrolled).catch(() => { });
            };

            const evaluate = () => {
                frame.handle = 0;

                const y = Headers.scrollTop(target.current);
                const delta = y - lastY;

                let next = hidden;

                if (Math.abs(delta) >= Headers.THRESHOLD) {
                    next = delta > 0;
                    lastY = y;
                }

                // The top always shows the header: within the offset there is nothing to make room for yet,
                // which is also what keeps a header from flickering away on the first few pixels of a scroll
                // that has barely started. Unlike a footer there is no matching rule at the far end - a header
                // that dropped back over the content the moment the page bottomed out would cover the very
                // thing the user scrolled down to reach.
                if (y <= offset) {
                    next = false;
                    lastY = y;
                }

                applyHidden(next);
                applyScrolled(y > elevation);
            };

            // rAF coalescing keeps a burst of scroll events down to one evaluation per painted frame.
            const scrollHandler = () => {
                if (frame.handle) return;

                frame.handle = requestAnimationFrame(evaluate);
            };

            // A hidden header is only translated out of the view, so everything inside it is still in the
            // tab order: a keyboard user tabbing into it lands on a control they cannot see. Revealing the
            // header as soon as anything inside it takes the focus keeps that control visible, and the
            // scroll baseline is re-read so the next scroll is measured from here.
            const focusHandler = () => {
                lastY = Headers.scrollTop(target.current);

                applyHidden(false);
            };

            let observer: ResizeObserver | undefined;

            // The scroller is watched as well as the page, since a pane only becomes the scroller of the
            // header once its own content overflows it.
            const observe = () => {
                if (!observer) return;

                observer.disconnect();
                observer.observe(document.documentElement);

                if (target.current !== window) {
                    observer.observe(target.current as HTMLElement);
                }
            };

            // Which box scrolls the header is not settled once and for all: a pane that had nothing to
            // scroll at setup time (so the walk landed on the window) becomes the scroller as soon as its
            // content outgrows it, and the other way round. The scroller is re-resolved whenever the
            // layout or the content moves, and the scroll listener follows it.
            const layoutHandler = () => {
                const next = Headers.scrollParent(element);

                if (next !== target.current) {
                    target.current.removeEventListener('scroll', scrollHandler);

                    target.current = next;

                    lastY = Headers.scrollTop(next);

                    next.addEventListener('scroll', scrollHandler, { passive: true });

                    observe();
                }

                scrollHandler();
            };

            target.current.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', layoutHandler, { passive: true });
            element.addEventListener('focusin', focusHandler);

            // Content that grows or shrinks on its own (a list that loads more rows, an expanding panel)
            // moves the scroll of its box without any scroll event to announce it. Watching the box that
            // holds the content picks those up, so a header does not stay lifted above a page that just
            // became short enough not to scroll at all.
            if (typeof ResizeObserver !== 'undefined') {
                observer = new ResizeObserver(layoutHandler);

                observe();
            }

            Headers._entries.set(id, { element, scrollHandler, layoutHandler, focusHandler, target, observer, frame });

            // The scroller can already be scrolled when the header arrives (a restored scroll position, a
            // deep link to an anchor, a header rendered into a pane the user has been reading), so the
            // states are settled once up front instead of waiting for a scroll that may never come.
            evaluate();
        }

        public static dispose(id: string) {
            const entry = Headers._entries.get(id);
            if (!entry) return;

            entry.target.current.removeEventListener('scroll', entry.scrollHandler);
            window.removeEventListener('resize', entry.layoutHandler);
            entry.element.removeEventListener('focusin', entry.focusHandler);

            entry.observer?.disconnect();

            // A frame scheduled by the last scroll before the disposal would still evaluate and call back
            // into a component that is on its way out, so it is dropped along with the listeners.
            if (entry.frame.handle) {
                cancelAnimationFrame(entry.frame.handle);

                entry.frame.handle = 0;
            }

            Headers._entries.delete(id);
        }

        private static scrollParent(element: HTMLElement): HTMLElement | Window {
            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const overflowY = getComputedStyle(node).overflowY;

                // A scrollable overflow that has nothing to scroll is not the scroller of the header: it
                // never fires a scroll event and it reads as sitting at its top forever, which would pin
                // the header revealed and flat. Walking past it lands on the box that really scrolls.
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
    }
}
