namespace BitBlazorUI {
    export class Stickies {
        // The physical edges of the scrollport an element can be pinned to, as the flags of the
        // BitStickyEdges enum the component reads the reported number back into.
        private static readonly EDGE_TOP = 1;
        private static readonly EDGE_BOTTOM = 2;
        private static readonly EDGE_LEFT = 4;
        private static readonly EDGE_RIGHT = 8;

        private static _entries = new Map<string, {
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
        // it, so the state is derived from two readings that together say what the browser itself
        // would: where the element is, and where it would be with nothing pinning it. The state only
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

            // Where the element sits in the flow of that scrollport, which is what the pinning moves
            // it away from and the one thing a pinned element cannot be measured back to. It is a
            // property of the layout rather than of the scroll, so it is read once and again whenever
            // the layout moves; null marks it as owed.
            let flow: { top: number, left: number } | null = null;

            // Starts negative - a value no set of flags can take - so the very first evaluation always
            // reports, settling the state of an element that is already pinned when it arrives (a
            // restored scroll position, a deep link).
            let edges = -1;

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            const evaluate = () => {
                frame.handle = 0;

                if (flow === null) {
                    flow = Stickies.flowPosition(element, scope.current);
                }

                let next = Stickies.stuckEdges(element, scope.current, flow);

                // A flip is where a flow position the content has moved on from would show, so it is
                // read again before any flip is believed, and the corrected reading settles the state
                // within the same frame. A reading that was right costs one more of the same reading
                // and still flips once; a stale one is replaced by the measurement that catches it.
                if (edges >= 0 && next !== edges) {
                    flow = Stickies.flowPosition(element, scope.current);

                    next = Stickies.stuckEdges(element, scope.current, flow);
                }

                if (next === edges) return;

                edges = next;

                // The reference is disposed before the listeners are, so a flip of the very last frame
                // can land on a dead reference. The rejection is consumed here rather than left to
                // surface as an unhandled one in the console of an application that did nothing wrong.
                dotnetObj.invokeMethodAsync('OnStuckChange', edges).catch(() => { });
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

                // Content that moved is content the flow position was read before, so the reading is
                // owed again. It is left to the frame the scroll handler below schedules rather than
                // taken here, since this also runs from a resize observer, and a measurement that
                // invalidates the very layout it reads is what makes an observer loop of one.
                flow = null;

                scrollHandler();
            };

            target.current.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', layoutHandler, { passive: true });

            let observer: ResizeObserver | undefined;

            const observe = () => {
                if (!observer) return;

                const watch = observer;

                const observeBox = (box: HTMLElement | Window | null) => {
                    if (!box || box === window) return;

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

                // The parent is the box the element travels within, so anything growing or shrinking
                // inside it moves the flow position the state is measured against.
                observeBox(element.parentElement);

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

            Stickies._entries.set(id, { scrollHandler, layoutHandler, target, scope, observer, frame });

            // The scroller can already be scrolled when the element arrives, so the state is settled
            // once up front instead of waiting for a scroll that may never come.
            evaluate();
        }

        // Reads everything the state is derived from again: which box scrolls the element, which one
        // it is pinned within, where it sits in the flow of that one, and the state itself. This is
        // what a layout change no observer can see - one that leaves every watched box the size it
        // was, such as content moved around inside the scrollport - is answered with.
        public static refresh(id: string) {
            Stickies._entries.get(id)?.layoutHandler();
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

        // The edges the element is currently pinned to, as the flags the component reads back. An edge
        // holds the element when two things are true of it at once, and neither of them says so alone:
        // the matching edge of the element sits on the boundary of the scrollport that its inset
        // measures from, and the element has been carried away from where the flow would have put it.
        // Without the first, an element pushed back out of the scrollport by the end of its containing
        // block - still offset, but on its way out of sight - would read as pinned; without the second,
        // so would one that has never moved at all and only happens to rest on that boundary, which is
        // every sticky header of a container nobody has scrolled yet.
        private static stuckEdges(element: HTMLElement, scope: HTMLElement | Window, flow: { top: number, left: number }): number {
            const style = getComputedStyle(element);

            if (style.position !== 'sticky' && style.position !== '-webkit-sticky') return 0;

            const rect = element.getBoundingClientRect();

            // An element that is not rendered at all (display:none, a collapsed ancestor) reports an
            // empty rect at the origin, which would otherwise read as pinned to the top left corner.
            if (rect.width === 0 && rect.height === 0) return 0;

            const origin = Stickies.scopeOrigin(scope);

            // The client sizes rather than the window's inner ones, which include the scrollbars -
            // an edge no sticky element can ever be pinned under.
            let top = 0;
            let left = 0;
            let width = document.documentElement.clientWidth;
            let height = document.documentElement.clientHeight;

            if (scope !== window) {
                const box = scope as HTMLElement;
                const boxStyle = getComputedStyle(box);

                const padTop = parseFloat(boxStyle.paddingTop) || 0;
                const padLeft = parseFloat(boxStyle.paddingLeft) || 0;
                const padRight = parseFloat(boxStyle.paddingRight) || 0;
                const padBottom = parseFloat(boxStyle.paddingBottom) || 0;

                // The origin is the border box, and the engine pins a sticky element within the content
                // box of its scroller - inside the border (the client offsets) and inside the padding
                // as well, as a pinned edge measured in a padded container confirms.
                top = origin.top + box.clientTop + padTop;
                left = origin.left + box.clientLeft + padLeft;
                width = box.clientWidth - padLeft - padRight;
                height = box.clientHeight - padTop - padBottom;
            }

            // How far the pinning has carried the element from its place in the flow, measured in the
            // frame of the content of the scrollport - so that scrolling alone, which moves the element
            // and its flow position together, never shows up in it and only the pinning does.
            const shiftY = (rect.top - origin.top + origin.scrollTop) - flow.top;
            const shiftX = (rect.left - origin.left + origin.scrollLeft) - flow.left;

            // A percentage inset stays a percentage in the computed style, resolved here against the
            // scrollport the way the sticky algorithm resolves it.
            const inset = (value: string, size: number) =>
                value.endsWith('%') ? (parseFloat(value) || 0) * size / 100 : (parseFloat(value) || 0);

            // Half a pixel of tolerance on each comparison: a pinned edge sits exactly on its
            // boundary, and sub-pixel layout puts it a fraction to either side of it. The same
            // tolerance on the shift, where it is what tells a pinned element from a resting one.
            let edges = 0;

            if (shiftY > 0.5 && style.top !== 'auto' && rect.top <= top + inset(style.top, height) + 0.5) {
                edges |= Stickies.EDGE_TOP;
            }

            if (shiftY < -0.5 && style.bottom !== 'auto' && rect.bottom >= top + height - inset(style.bottom, height) - 0.5) {
                edges |= Stickies.EDGE_BOTTOM;
            }

            if (shiftX > 0.5 && style.left !== 'auto' && rect.left <= left + inset(style.left, width) + 0.5) {
                edges |= Stickies.EDGE_LEFT;
            }

            if (shiftX < -0.5 && style.right !== 'auto' && rect.right >= left + width - inset(style.right, width) - 0.5) {
                edges |= Stickies.EDGE_RIGHT;
            }

            return edges;
        }

        // Where the element sits in the flow of its scrollport, which is the one thing about a pinned
        // element that cannot be read off it while it is pinned: every geometry it reports carries the
        // sticky offset already, down to offsetTop. So the offset is taken off for the length of a
        // single measurement - a sticky box and a static one are laid out in exactly the same place,
        // so nothing but the offset goes with it, and nothing is painted in between - and the reading
        // is normalized by the scroll offset, which is what makes it the same number at every scroll
        // position and lets it be taken while the element is already pinned.
        private static flowPosition(element: HTMLElement, scope: HTMLElement | Window): { top: number, left: number } {
            const position = element.style.position;

            element.style.position = 'static';

            const rect = element.getBoundingClientRect();
            const origin = Stickies.scopeOrigin(scope);

            // The property is put back rather than cleared: the inline style of the element may carry
            // a position of the page's own, and this one is only meant to last for the measurement.
            element.style.position = position;

            return {
                top: rect.top - origin.top + origin.scrollTop,
                left: rect.left - origin.left + origin.scrollLeft
            };
        }

        // The border box of the scrollport and how far its content is scrolled within it. The two
        // together are the fixed frame of reference the flow position is measured in: the box itself
        // does not move while its content scrolls, so adding the scroll offset back cancels the scroll
        // out of every reading taken from it.
        private static scopeOrigin(scope: HTMLElement | Window): { top: number, left: number, scrollTop: number, scrollLeft: number } {
            if (scope === window) {
                return { top: 0, left: 0, scrollTop: window.scrollY, scrollLeft: window.scrollX };
            }

            const box = scope as HTMLElement;
            const rect = box.getBoundingClientRect();

            return { top: rect.top, left: rect.left, scrollTop: box.scrollTop, scrollLeft: box.scrollLeft };
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
