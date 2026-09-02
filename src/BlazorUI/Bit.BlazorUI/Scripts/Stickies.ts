namespace BitBlazorUI {
    export class Stickies {
        private static _entries = new Map<string, {
            element: HTMLElement,
            scrollHandler: () => void,
            layoutHandler: () => void,
            // The scroller is held in a box, since it is re-resolved whenever the layout changes and
            // dispose has to take the scroll listener off the target it is bound to at that moment.
            target: { current: HTMLElement | Window },
            // The box the element is pinned within, held in a box of its own for the same reason: it
            // is not always the one the scroll events come from, and it is re-resolved just as often.
            scope: { current: HTMLElement | Window },
            observer?: ResizeObserver,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number }
        }>();

        // Watches a position:sticky element for the moment it actually pins to an edge of its
        // scrolling container and reports the flips of that state back to .NET. CSS has no event for
        // it, so the state is read off the geometry: a pinned edge sits exactly on the boundary its
        // inset names, and an element in the normal flow sits somewhere past it. The state only
        // crosses the interop boundary when it flips, so a scroll never costs more than a comparison.
        public static setup(id: string, dotnetObj: DotNetObject) {
            Stickies.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // The scroller is not always the window: any pane with its own overflow scrolls its own
            // box, and a scroll event on an element does not bubble to the window.
            const target = { current: Stickies.scrollSource(element) };

            // Which box the element is pinned within is a separate question from which box the scroll
            // events come from: a pane that clips its overflow without having anything to scroll is
            // still the scrollport the element is pinned in, but it never fires a scroll event.
            const scope = { current: Stickies.stickyParent(element) };

            // Starts undefined so the very first evaluation always reports, settling the state of an
            // element that is already pinned when it arrives (a restored scroll position, a deep link).
            let stuck: boolean | undefined;

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            const evaluate = () => {
                frame.handle = 0;

                const next = Stickies.isStuck(element, scope.current);

                if (next === stuck) return;

                stuck = next;

                // The reference is disposed before the listeners are, so a flip of the very last frame
                // can land on a dead reference. The rejection is consumed here rather than left to
                // surface as an unhandled one in the console of an application that did nothing wrong.
                dotnetObj.invokeMethodAsync('OnStuckChange', stuck).catch(() => { });
            };

            // rAF coalescing keeps a burst of scroll events down to one evaluation per painted frame.
            const scrollHandler = () => {
                if (frame.handle) return;

                frame.handle = requestAnimationFrame(evaluate);
            };

            // Which box scrolls the element is not settled once and for all: a pane that had nothing
            // to scroll at setup time (so the walk landed on the window) becomes the scroller as soon
            // as its content outgrows it, and the other way round. The scroller is re-resolved
            // whenever the layout moves, and the scroll listener follows it. The scrollport is
            // re-resolved with it, since a stylesheet can give an ancestor an overflow it did not have.
            const layoutHandler = () => {
                const next = Stickies.scrollSource(element);
                const nextScope = Stickies.stickyParent(element);

                if (next !== target.current || nextScope !== scope.current) {
                    if (next !== target.current) {
                        target.current.removeEventListener('scroll', scrollHandler);

                        target.current = next;

                        next.addEventListener('scroll', scrollHandler, { passive: true });
                    }

                    scope.current = nextScope;

                    observe();
                }

                scrollHandler();
            };

            target.current.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', layoutHandler, { passive: true });

            let observer: ResizeObserver | undefined;

            const observe = () => {
                if (!observer) return;

                const watch = observer;

                const observeBox = (box: HTMLElement | Window) => {
                    if (box === window) return;

                    const pane = box as HTMLElement;

                    watch.observe(pane);

                    // A pane of a fixed height keeps the same border box however much content is put
                    // into it, so watching the box alone never reports the growth that turns it into
                    // the scroller. Its content wrapper is the box that actually grows with the content.
                    if (pane.firstElementChild) {
                        watch.observe(pane.firstElementChild);
                    }
                };

                watch.disconnect();
                watch.observe(document.documentElement);
                watch.observe(element);

                // The two are the same box whenever the scrollport has something to scroll, and
                // observing one twice is what the second call already means to the observer.
                observeBox(target.current);
                observeBox(scope.current);
            };

            // Content that grows or shrinks on its own (a list that loads more rows, an expanding
            // panel) moves the geometry without any scroll event to announce it.
            if (typeof ResizeObserver !== 'undefined') {
                observer = new ResizeObserver(layoutHandler);

                observe();
            }

            Stickies._entries.set(id, { element, scrollHandler, layoutHandler, target, scope, observer, frame });

            // The scroller can already be scrolled when the element arrives, so the state is settled
            // once up front instead of waiting for a scroll that may never come.
            evaluate();
        }

        public static dispose(id: string) {
            const entry = Stickies._entries.get(id);
            if (!entry) return;

            entry.target.current.removeEventListener('scroll', entry.scrollHandler);
            window.removeEventListener('resize', entry.layoutHandler);

            entry.observer?.disconnect();

            // A frame scheduled by the last scroll before the disposal would still evaluate and call
            // back into a component that is on its way out, so it is dropped along with the listeners.
            if (entry.frame.handle) {
                cancelAnimationFrame(entry.frame.handle);

                entry.frame.handle = 0;
            }

            Stickies._entries.delete(id);
        }

        // Whether the element is currently pinned to any of the edges its insets name. Each inset that
        // is not auto is a sticky constraint, and a constraint is binding when the matching edge of
        // the element sits on (or has been pushed past, by the end of its containing block) the
        // boundary of the scrollport that inset measures from. The insets are read off the computed
        // style, so the class-based positions and the inline offsets are seen the same way, already
        // resolved to physical sides and to pixels.
        private static isStuck(element: HTMLElement, scope: HTMLElement | Window): boolean {
            const style = getComputedStyle(element);

            if (style.position !== 'sticky' && style.position !== '-webkit-sticky') return false;

            const rect = element.getBoundingClientRect();

            // An element that is not rendered at all (display:none, a collapsed ancestor) reports an
            // empty rect at the origin, which would otherwise read as pinned to the top left corner.
            if (rect.width === 0 && rect.height === 0) return false;

            // The client sizes rather than the window's inner ones, which include the scrollbars -
            // an edge no sticky element can ever be pinned under.
            let top = 0;
            let left = 0;
            let width = document.documentElement.clientWidth;
            let height = document.documentElement.clientHeight;

            if (scope !== window) {
                const box = scope as HTMLElement;
                const boxRect = box.getBoundingClientRect();
                const boxStyle = getComputedStyle(box);

                const padTop = parseFloat(boxStyle.paddingTop) || 0;
                const padLeft = parseFloat(boxStyle.paddingLeft) || 0;
                const padRight = parseFloat(boxStyle.paddingRight) || 0;
                const padBottom = parseFloat(boxStyle.paddingBottom) || 0;

                // The rect is the border box, and the engine pins a sticky element within the content
                // box of its scroller - inside the border (the client offsets) and inside the padding
                // as well, as a pinned edge measured in a padded container confirms.
                top = boxRect.top + box.clientTop + padTop;
                left = boxRect.left + box.clientLeft + padLeft;
                width = box.clientWidth - padLeft - padRight;
                height = box.clientHeight - padTop - padBottom;
            }

            // A percentage inset stays a percentage in the computed style, resolved here against the
            // scrollport the way the sticky algorithm resolves it.
            const inset = (value: string, size: number) =>
                value.endsWith('%') ? (parseFloat(value) || 0) * size / 100 : (parseFloat(value) || 0);

            // Half a pixel of tolerance on each comparison: a pinned edge sits exactly on its
            // boundary, and sub-pixel layout puts it a fraction to either side of it.
            if (style.top !== 'auto' && rect.top <= top + inset(style.top, height) + 0.5) return true;
            if (style.bottom !== 'auto' && rect.bottom >= top + height - inset(style.bottom, height) - 0.5) return true;
            if (style.left !== 'auto' && rect.left <= left + inset(style.left, width) + 0.5) return true;
            if (style.right !== 'auto' && rect.right >= left + width - inset(style.right, width) - 0.5) return true;

            return false;
        }

        // The scrollport the element is pinned within: its nearest ancestor that is a scroll
        // container, whether or not there is anything to scroll in it right now. Every overflow but
        // visible and clip makes one, so a pane that clips its content is a box the element can only
        // ever be pinned inside of - measured against the viewport behind such a pane instead, an
        // element that merely scrolls out of sight with the page reads as pinned to an edge of it.
        private static stickyParent(element: HTMLElement): HTMLElement | Window {
            const scrolls = (overflow: string) => overflow !== 'visible' && overflow !== 'clip';

            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const style = getComputedStyle(node);

                if (scrolls(style.overflowY) || scrolls(style.overflowX)) return node;

                node = node.parentElement;
            }

            return window;
        }

        // Where the scroll events come from, which is the nearest ancestor that actually scrolls on
        // either axis: a scrollport whose scrollable overflow has nothing to scroll never fires a
        // scroll event, so a listener on it would be a listener for nothing. Such a box moves with
        // whatever scrolls it instead, which is the box this walk goes on to.
        private static scrollSource(element: HTMLElement): HTMLElement | Window {
            const scrolls = (overflow: string) => overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay' || overflow === 'hidden';

            let node = element.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const style = getComputedStyle(node);

                if ((scrolls(style.overflowY) && node.scrollHeight > node.clientHeight) ||
                    (scrolls(style.overflowX) && node.scrollWidth > node.clientWidth)) return node;

                node = node.parentElement;
            }

            return window;
        }
    }
}
