namespace BitBlazorUI {
    export class Footers {
        private static _entries = new Map<string, {
            element: HTMLElement,
            scrollHandler: () => void,
            layoutHandler: () => void,
            focusHandler: () => void,
            // The scroller is held in a box as well, since it is re-resolved whenever the layout changes
            // and dispose has to take the scroll listener off the target it is bound to at that moment.
            target: { current: HTMLElement | Window },
            observer?: ResizeObserver,
            // Gives up the claim of this footer on the scroll padding of the scroller, whichever box is
            // carrying it at that moment, and puts the padding back once no footer is holding it anymore.
            clearPadding: () => void,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number }
        }>();

        // The scroll padding of a box is shared ground: two pinned footers over the same scroller (an app
        // shell whose pane carries both) would each otherwise capture the write of the other as "the
        // value it had before", so the first one to go would wipe the padding the second still needs and
        // the second would hand the box back a value the page never had. The value found before any of
        // them touched the box is kept here instead, together with the footers reserving padding on it,
        // and only the last one to let go puts it back.
        private static _paddings = new WeakMap<HTMLElement, { previous: string, owners: Set<string> }>();

        // Scroll deltas below this many pixels are ignored, so the rubber-banding of touch devices and
        // the sub-pixel jitter of a trackpad cannot flip the footer back and forth on every frame.
        private static readonly THRESHOLD = 4;

        // Slides the footer out of the view while the page is scrolled down and brings it back while it is
        // scrolled up (the classic "reveal" behavior of an app bar). The state lives here and only crosses
        // the interop boundary when it actually flips, so a scroll never costs more than a comparison.
        // It also keeps the bottom scroll padding of the scroller in step with the height of the footer,
        // so nothing scrolled to lands underneath a pinned one.
        public static setup(id: string, dotnetObj: DotNetObject, revealOffset: number, reveal: boolean, scrollTarget: string | null, scrollPadding: boolean) {
            Footers.dispose(id);

            const element = document.getElementById(id);
            if (!element) return;

            // An explicitly named scroller wins over the walk up the tree, which is what a footer that
            // does not sit inside the box it reacts to (an app shell whose footer and content are
            // siblings) needs. A selector that matches nothing falls back to the walk rather than
            // leaving the footer without a scroller at all.
            const resolveTarget = (): HTMLElement | Window => {
                if (scrollTarget) {
                    // A selector that is not valid CSS makes querySelector throw rather than return
                    // nothing, and this runs again on every resize and every layout change, so a typo in
                    // the parameter would otherwise break the setup and then every re-resolution after it.
                    try {
                        const found = document.querySelector(scrollTarget);

                        if (found) return found as HTMLElement;
                    } catch { }
                }

                return Footers.scrollParent(element);
            };

            // The scroller is not always the window: an app shell (and any pane with its own overflow)
            // scrolls its own box, and a scroll event on an element does not bubble to the window.
            const target = { current: resolveTarget() };

            // A negative offset would keep the footer hidden at the very top of the scroller, where the
            // first rule below is what has to win.
            const offset = Math.max(0, revealOffset || 0);

            let hidden = false;
            let lastY = Footers.scrollTop(target.current);

            // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
            const frame = { handle: 0 };

            // The box this footer is reserving scroll padding on, if it is reserving any.
            let padded: HTMLElement | undefined;

            const clearPadding = () => {
                if (!padded) return;

                const shared = Footers._paddings.get(padded);

                if (shared) {
                    shared.owners.delete(id);

                    // The footer leaving is not necessarily the one that wrote the padding that is on the
                    // box right now, so the value is only put back once nothing is reserving any anymore.
                    if (shared.owners.size === 0) {
                        padded.style.scrollPaddingBlockEnd = shared.previous;

                        Footers._paddings.delete(padded);
                    }
                }

                padded = undefined;
            };

            // A pinned footer covers the bottom of the scroller, so a control the browser brings into the
            // view as the focus moves on, an anchor, or scrollIntoView, lands underneath it (WCAG 2.4.11).
            // Reserving the height of the footer as the bottom scroll padding of the scroller is what stops
            // the scroll short of it. The value is on the element that scrolls - the documentElement when
            // that is the page - and it is re-read whenever the footer or the layout changes size.
            const applyPadding = () => {
                if (!scrollPadding) return;

                const box = target.current === window
                    ? document.documentElement
                    : target.current as HTMLElement;

                if (padded && padded !== box) {
                    clearPadding();
                }

                if (!padded) {
                    let shared = Footers._paddings.get(box);

                    if (!shared) {
                        shared = { previous: box.style.scrollPaddingBlockEnd, owners: new Set() };

                        Footers._paddings.set(box, shared);
                    }

                    shared.owners.add(id);

                    padded = box;
                }

                box.style.scrollPaddingBlockEnd = `${element.offsetHeight}px`;
            };

            const apply = (next: boolean) => {
                if (!reveal || next === hidden) return;

                hidden = next;

                // The reference is disposed before the listeners are, so a flip of the very last frame can
                // land on a dead reference. The rejection is consumed here rather than left to surface as an
                // unhandled one in the console of an application that did nothing wrong.
                dotnetObj.invokeMethodAsync('OnRevealChange', hidden).catch(() => { });
            };

            const evaluate = () => {
                frame.handle = 0;

                if (!reveal) return;

                const y = Footers.scrollTop(target.current);
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
                if (y <= offset || Footers.atEnd(target.current)) {
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

            // A footer slid away by the scroll is only translated out of the view, so everything inside it
            // is still in the tab order: a keyboard user tabbing past the content lands on a control they
            // cannot see. Revealing the footer as soon as anything inside it takes the focus keeps that
            // control visible, and the scroll baseline is re-read so the next scroll is measured from here.
            const focusHandler = () => {
                lastY = Footers.scrollTop(target.current);

                apply(false);
            };

            let observer: ResizeObserver | undefined;

            // The scroller is watched as well as the page, since a pane only becomes the scroller of the
            // footer once its own content overflows it. The footer itself is watched only when its height
            // is being mirrored into the scroll padding of that scroller.
            const observe = () => {
                if (!observer) return;

                observer.disconnect();
                observer.observe(document.documentElement);

                if (target.current !== window) {
                    const box = target.current as HTMLElement;

                    observer.observe(box);

                    // A pane of a fixed height keeps the same border box however much content is put into
                    // it, so watching the box alone never reports the growth that turns it into the scroller
                    // of the footer. Its content wrapper is the box that actually grows with the content.
                    if (box.firstElementChild) {
                        observer.observe(box.firstElementChild);
                    }
                }

                if (scrollPadding) {
                    observer.observe(element);
                }
            };

            // Which box scrolls the footer is not settled once and for all: a pane that had nothing to
            // scroll at setup time (so the walk landed on the window) becomes the scroller as soon as its
            // content outgrows it, and the other way round. The scroller is re-resolved whenever the
            // layout or the content moves, and the scroll listener follows it.
            const layoutHandler = () => {
                const next = resolveTarget();

                if (next !== target.current) {
                    target.current.removeEventListener('scroll', scrollHandler);

                    target.current = next;

                    lastY = Footers.scrollTop(next);

                    next.addEventListener('scroll', scrollHandler, { passive: true });

                    observe();
                }

                applyPadding();

                scrollHandler();
            };

            target.current.addEventListener('scroll', scrollHandler, { passive: true });
            window.addEventListener('resize', layoutHandler, { passive: true });
            element.addEventListener('focusin', focusHandler);

            // Whether the scroller sits at its end is a function of how tall its content is, and content that
            // grows or shrinks on its own (a list that loads more rows, an expanding panel) moves that end
            // without any scroll event to announce it. Watching the box that holds the content picks those up,
            // so a footer does not stay hidden below a page that just became shorter than the scroll it had.
            if (typeof ResizeObserver !== 'undefined') {
                observer = new ResizeObserver(layoutHandler);

                observe();
            }

            Footers._entries.set(id, { element, scrollHandler, layoutHandler, focusHandler, target, observer, clearPadding, frame });

            applyPadding();
        }

        public static dispose(id: string) {
            const entry = Footers._entries.get(id);
            if (!entry) return;

            entry.target.current.removeEventListener('scroll', entry.scrollHandler);
            window.removeEventListener('resize', entry.layoutHandler);
            entry.element.removeEventListener('focusin', entry.focusHandler);

            entry.observer?.disconnect();

            entry.clearPadding();

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
