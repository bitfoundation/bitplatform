namespace BitBlazorUI {
    type BitStickySides = { top: number, bottom: number, left: number, right: number };
    type BitStickyPaddingSide = 'scrollPaddingBlockStart' | 'scrollPaddingBlockEnd' | 'scrollPaddingInlineStart' | 'scrollPaddingInlineEnd';

    export class Stickies {
        // The physical edges of the scrollport an element can be pinned to, as the flags of the
        // BitStickyEdges enum the component reads the reported number back into.
        private static readonly EDGE_TOP = 1;
        private static readonly EDGE_BOTTOM = 2;
        private static readonly EDGE_LEFT = 4;
        private static readonly EDGE_RIGHT = 8;

        // The scroll padding is written through its logical properties, which are the very ones a BitHeader
        // (block start) and a BitFooter (block end) write on the same box: a physical side and its logical
        // counterpart are two declarations of one value, and which of the two the browser follows would
        // depend on the order they happened to be written in.
        private static readonly PADDING_SIDES: BitStickyPaddingSide[] = ['scrollPaddingBlockStart', 'scrollPaddingBlockEnd', 'scrollPaddingInlineStart', 'scrollPaddingInlineEnd'];

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
            mutations?: MutationObserver,
            // Gives up the claim of this element on the scroll padding of its scrollport, whichever box is
            // carrying it at that moment.
            clearPadding: () => void,
            // The pending frame is held in a box rather than in a plain field so the handler can keep
            // writing to the same object that dispose reads from.
            frame: { handle: number }
        }>();

        // The scroll padding of a box is shared ground: two stickies of the same scrollport (a header and
        // a sub-header, a frozen header row and a frozen first column) would each otherwise capture the
        // write of the other as "the value it had before". So the stickies of a box only register here,
        // and one pass per frame measures all of them together and writes the box once: every side
        // carries the largest of their claims, or the value the page gives it itself where that one is
        // larger. The page's own value is whatever is on the box that this script did not write - kept
        // up to date on every pass, so a value written since (by the page, or by a BitHeader or a
        // BitFooter claiming the same box) is the one combined with and the one put back once nobody
        // claims the side anymore.
        private static _paddings = new WeakMap<HTMLElement, {
            owners: Map<string, { element: HTMLElement, mutations?: MutationObserver }>,
            own: Record<BitStickyPaddingSide, string>,
            // What this script last left on each side, which is what tells its own write from anyone else's.
            written: Record<BitStickyPaddingSide, string | null>,
            frame: number
        }>();

        // Watches a position:sticky element for the moment it actually pins to an edge of its
        // scrolling container and reports the flips of that state back to .NET. CSS has no event for
        // it, so the state is derived from two readings that together say what the browser itself
        // would: where the element is, and where it would be with nothing pinning it. The state only
        // crosses the interop boundary when it flips, so a scroll never costs more than a comparison.
        //
        // With scrollPadding it also reserves the room the element covers as the scroll padding of its
        // scrollport, so a control the browser brings into view as the focus moves on (or an anchor, or
        // scrollIntoView) never lands underneath it (WCAG 2.4.11). That half needs no scroll listener at
        // all, so a sticky that only asks for it (report false) attaches none.
        public static setup(id: string, dotnetObj: DotNetObject, report: boolean = true, scrollPadding: boolean = false) {
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

            let mutations: MutationObserver | undefined;

            const evaluate = () => {
                frame.handle = 0;

                if (!report) return;

                if (flow === null) {
                    flow = Stickies.flowPosition(element, scope.current, mutations);
                }

                let next = Stickies.stuckEdges(element, scope.current, flow);

                // A flip is where a flow position the content has moved on from would show, so it is
                // read again before any flip is believed, and the corrected reading settles the state
                // within the same frame. A reading that was right costs one more of the same reading
                // and still flips once; a stale one is replaced by the measurement that catches it.
                if (edges >= 0 && next !== edges) {
                    flow = Stickies.flowPosition(element, scope.current, mutations);

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
                if (!report || frame.handle) return;

                frame.handle = requestAnimationFrame(evaluate);
            };

            // The box this element is reserving scroll padding on, if it is reserving any.
            let padded: HTMLElement | undefined;

            const clearPadding = () => {
                if (!padded) return;

                Stickies.releasePadding(padded, id);

                padded = undefined;
            };

            // Only registers the claim: what it amounts to is measured in the pass of the box, together with
            // the claims of every other sticky of the same box, so this is cheap enough to run from the
            // resize observer below for every one of them.
            const applyPadding = () => {
                if (!scrollPadding) return;

                const box = Stickies.paddingBox(scope.current);

                if (padded === box) {
                    Stickies.schedulePadding(box);

                    return;
                }

                clearPadding();

                Stickies.claimPadding(box, id, { element, mutations });

                padded = box;
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

                        if (report) {
                            next.addEventListener('scroll', scrollHandler, { passive: true });
                        }
                    }

                    scope.current = nextScope;

                    observe();
                }

                applyPadding();

                // Content that moved is content the flow position was read before, so the reading is
                // owed again. It is left to the frame the scroll handler below schedules rather than
                // taken here, since this also runs from a resize observer, and a measurement that
                // invalidates the very layout it reads is what makes an observer loop of one.
                flow = null;

                scrollHandler();
            };

            if (report) {
                target.current.addEventListener('scroll', scrollHandler, { passive: true });
            }
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

            // What pins the element - its position, its insets and the direction that maps Start and End to
            // a side - changes with no box resizing whenever a class or an inline style of its own does: a
            // Position or an offset changed, a Dir, a StuckClass bringing an inset of its own. Only a change
            // of the outcome counts, so the stuck classes and styles toggled on every flip - and the
            // measurements above, which toggle the position for an instant - cost a style read and no more.
            if (typeof MutationObserver !== 'undefined') {
                let geometry = Stickies.geometry(element);

                mutations = new MutationObserver(() => {
                    const next = Stickies.geometry(element);

                    if (next === geometry) return;

                    geometry = next;

                    layoutHandler();
                });

                mutations.observe(element, { attributes: true, attributeFilter: ['class', 'style', 'dir'] });
            }

            Stickies._entries.set(id, { scrollHandler, layoutHandler, target, scope, observer, mutations, clearPadding, frame });

            applyPadding();

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
            entry.mutations?.disconnect();

            entry.clearPadding();

            // A frame scheduled by the last scroll before the disposal would still evaluate and call
            // back into a component that is on its way out, so it is dropped along with the listeners.
            if (entry.frame.handle) {
                cancelAnimationFrame(entry.frame.handle);

                entry.frame.handle = 0;
            }

            Stickies._entries.delete(id);
        }

        // The box the scroll padding of a scrollport is written on: the element itself, or the root
        // element when that is the page.
        private static paddingBox(scope: HTMLElement | Window): HTMLElement {
            return scope === window ? document.documentElement : scope as HTMLElement;
        }

        private static claimPadding(box: HTMLElement, id: string, owner: { element: HTMLElement, mutations?: MutationObserver }) {
            let shared = Stickies._paddings.get(box);

            if (!shared) {
                shared = {
                    owners: new Map(),
                    own: {
                        scrollPaddingBlockStart: box.style.scrollPaddingBlockStart,
                        scrollPaddingBlockEnd: box.style.scrollPaddingBlockEnd,
                        scrollPaddingInlineStart: box.style.scrollPaddingInlineStart,
                        scrollPaddingInlineEnd: box.style.scrollPaddingInlineEnd
                    },
                    written: {
                        scrollPaddingBlockStart: null,
                        scrollPaddingBlockEnd: null,
                        scrollPaddingInlineStart: null,
                        scrollPaddingInlineEnd: null
                    },
                    frame: 0
                };

                Stickies._paddings.set(box, shared);
            }

            shared.owners.set(id, owner);

            Stickies.schedulePadding(box);
        }

        private static releasePadding(box: HTMLElement, id: string) {
            const shared = Stickies._paddings.get(box);
            if (!shared) return;

            shared.owners.delete(id);

            Stickies.schedulePadding(box);
        }

        // One pass per box and frame, however many of its stickies asked for one: a resize reaches every
        // one of them, and a write per sticky, each followed by the reading of the next, would lay the
        // page out once per sticky.
        private static schedulePadding(box: HTMLElement) {
            const shared = Stickies._paddings.get(box);
            if (!shared || shared.frame) return;

            shared.frame = requestAnimationFrame(() => Stickies.writePadding(box));
        }

        private static writePadding(box: HTMLElement) {
            const shared = Stickies._paddings.get(box);
            if (!shared) return;

            shared.frame = 0;

            // A side carrying anything but what this script left on it was written by someone else since,
            // and that value is the page's own from now on. Whatever this script wrote is taken off, so the
            // computed value read below is the one the page gives the side by itself - an inline value or
            // a stylesheet's.
            for (const side of Stickies.PADDING_SIDES) {
                const current = box.style[side];

                if (current !== shared.written[side]) {
                    shared.own[side] = current;
                }

                if (current !== shared.own[side]) {
                    box.style[side] = shared.own[side];
                }

                shared.written[side] = shared.own[side];
            }

            if (shared.owners.size === 0) {
                Stickies._paddings.delete(box);

                return;
            }

            const style = getComputedStyle(box);
            const sides = Stickies.logicalSides(style);
            const claim = Stickies.paddingClaim(shared.owners, box, sides);

            // A percentage is one of the scrollport, which is the viewport for the root element. A value the
            // computed style still holds unresolved (a calc() mixing units) reads as nothing, so the claim
            // is what the side gets there.
            const width = box.clientWidth;
            const height = box.clientHeight;

            for (const side of Stickies.PADDING_SIDES) {
                const physical = sides[side];
                const needed = claim[physical];

                if (needed <= 0) continue;

                const page = Stickies.pixels(style[side], physical === 'top' || physical === 'bottom' ? height : width);

                if (needed <= page) continue;

                const value = `${needed}px`;

                box.style[side] = value;

                // Read back rather than kept as written, since the comparison on the next pass is against
                // the serialization the declaration hands back.
                shared.written[side] = box.style[side];
            }
        }

        // The physical side each logical scroll padding property lands on in the writing mode and the
        // direction of the box. The start sides are also where the scroll of each axis begins.
        private static logicalSides(style: CSSStyleDeclaration): Record<BitStickyPaddingSide, keyof BitStickySides> {
            const writingMode = style.writingMode;
            const ltr = style.direction !== 'rtl';

            if (writingMode.startsWith('vertical') === false && writingMode.startsWith('sideways') === false) {
                return {
                    scrollPaddingBlockStart: 'top',
                    scrollPaddingBlockEnd: 'bottom',
                    scrollPaddingInlineStart: ltr ? 'left' : 'right',
                    scrollPaddingInlineEnd: ltr ? 'right' : 'left'
                };
            }

            const blockStartsLeft = writingMode === 'vertical-lr' || writingMode === 'sideways-lr';
            const inlineStartsTop = writingMode === 'sideways-lr' ? !ltr : ltr;

            return {
                scrollPaddingBlockStart: blockStartsLeft ? 'left' : 'right',
                scrollPaddingBlockEnd: blockStartsLeft ? 'right' : 'left',
                scrollPaddingInlineStart: inlineStartsTop ? 'top' : 'bottom',
                scrollPaddingInlineEnd: inlineStartsTop ? 'bottom' : 'top'
            };
        }

        // The room the stickies of a box cover on each of its edges, the largest of them per edge, measured
        // from the edge of the scrollport the way scroll padding is: the padding of the box, the inset, and
        // the size of the element. An element claims only an edge it can actually be pinned to: one it has
        // an inset on, that its containing block leaves it room to travel toward, and - for the edge on the
        // far side of where the scroll of that axis begins - one it starts out beyond, since the scroll can
        // only ever carry it away from that edge otherwise (the bottom of a TopAndBottom bar at the top of
        // a pane). Where the scroll of an axis can carry it is left out on purpose: that grows with the
        // content without anything resizing, and a claim on an axis that has nothing to scroll costs nothing.
        // A section header whose section has scrolled away keeps its claim too, since scrolling back into
        // that section is what brings it over the target again.
        private static paddingClaim(owners: Map<string, { element: HTMLElement, mutations?: MutationObserver }>, box: HTMLElement, sides: Record<BitStickyPaddingSide, keyof BitStickySides>): BitStickySides {
            const claim: BitStickySides = { top: 0, bottom: 0, left: 0, right: 0 };

            // Every style is read before any element is touched, the computed values copied out of the live
            // declarations, which would answer for the static position once it is set.
            const items: { element: HTMLElement, inline: string, insets: { top: string, bottom: string, left: string, right: string }, block: HTMLElement, blockEdges: BitStickySides, mutations?: MutationObserver }[] = [];

            owners.forEach(owner => {
                const element = owner.element;

                if (element.isConnected === false) return;

                const style = getComputedStyle(element);

                if (style.position !== 'sticky' && style.position !== '-webkit-sticky') return;

                const block = Stickies.containingBlock(element, style);
                const blockStyle = getComputedStyle(block);

                items.push({
                    element,
                    inline: element.style.position,
                    insets: { top: style.top, bottom: style.bottom, left: style.left, right: style.right },
                    block,
                    blockEdges: {
                        top: (parseFloat(blockStyle.borderTopWidth) || 0) + (parseFloat(blockStyle.paddingTop) || 0),
                        bottom: (parseFloat(blockStyle.borderBottomWidth) || 0) + (parseFloat(blockStyle.paddingBottom) || 0),
                        left: (parseFloat(blockStyle.borderLeftWidth) || 0) + (parseFloat(blockStyle.paddingLeft) || 0),
                        right: (parseFloat(blockStyle.borderRightWidth) || 0) + (parseFloat(blockStyle.paddingRight) || 0)
                    },
                    mutations: owner.mutations
                });
            });

            if (items.length === 0) return claim;

            const scope = box === document.documentElement ? window : box;
            const frame = Stickies.scrollportFrame(scope);
            const origin = Stickies.scopeOrigin(scope);
            const pad = frame.padding;

            // The padding box, which is what scroll padding is measured from and what the scroll offsets
            // count in: every reading below is in its coordinates at the initial scroll position.
            const boxTop = frame.top - pad.top;
            const boxLeft = frame.left - pad.left;
            const boxWidth = frame.width + pad.left + pad.right;
            const boxHeight = frame.height + pad.top + pad.bottom;

            const y = (value: number) => value - boxTop + origin.scrollTop;
            const x = (value: number) => value - boxLeft + origin.scrollLeft;

            const startSides = [sides.scrollPaddingBlockStart, sides.scrollPaddingInlineStart];
            const scrollsFromBottom = startSides.includes('bottom');
            const scrollsFromRight = startSides.includes('right');

            // All of them are measured in the flow in one layout: set static together, read, and put back
            // together, rather than one layout per element.
            items.forEach(item => item.element.style.position = 'static');

            items.forEach(item => {
                const rect = item.element.getBoundingClientRect();

                if (rect.width === 0 && rect.height === 0) return;

                const flow = { top: y(rect.top), bottom: y(rect.bottom), left: x(rect.left), right: x(rect.right) };

                // A child of the scroller itself travels within everything the scroller holds, which is
                // the scrollable overflow rather than the box.
                let block: BitStickySides;

                if (item.block === box) {
                    const scrollWidth = box.scrollWidth;
                    const scrollHeight = box.scrollHeight;

                    block = {
                        top: scrollsFromBottom ? boxHeight - scrollHeight + pad.top : pad.top,
                        bottom: scrollsFromBottom ? boxHeight - pad.bottom : scrollHeight - pad.bottom,
                        left: scrollsFromRight ? boxWidth - scrollWidth + pad.left : pad.left,
                        right: scrollsFromRight ? boxWidth - pad.right : scrollWidth - pad.right
                    };
                } else {
                    const blockRect = item.block.getBoundingClientRect();

                    block = {
                        top: y(blockRect.top) + item.blockEdges.top,
                        bottom: y(blockRect.bottom) - item.blockEdges.bottom,
                        left: x(blockRect.left) + item.blockEdges.left,
                        right: x(blockRect.right) - item.blockEdges.right
                    };
                }

                const inset = (value: string, size: number) => value === 'auto' ? null : Stickies.pixels(value, size);

                const top = inset(item.insets.top, frame.height);
                const bottom = inset(item.insets.bottom, frame.height);
                const left = inset(item.insets.left, frame.width);
                const right = inset(item.insets.right, frame.width);

                // Rounded up, so a sub-pixel element never leaves a hairline of the next control under it.
                // The half pixel of tolerance is the one the stuck detection allows sub-pixel layout too.
                if (top !== null &&
                    block.bottom - flow.bottom > 0.5 &&
                    (scrollsFromBottom === false || flow.top < pad.top + top - 0.5)) {
                    claim.top = Math.max(claim.top, Math.ceil(pad.top + top + rect.height));
                }

                if (bottom !== null &&
                    flow.top - block.top > 0.5 &&
                    (scrollsFromBottom || flow.bottom > boxHeight - pad.bottom - bottom + 0.5)) {
                    claim.bottom = Math.max(claim.bottom, Math.ceil(pad.bottom + bottom + rect.height));
                }

                if (left !== null &&
                    block.right - flow.right > 0.5 &&
                    (scrollsFromRight === false || flow.left < pad.left + left - 0.5)) {
                    claim.left = Math.max(claim.left, Math.ceil(pad.left + left + rect.width));
                }

                if (right !== null &&
                    flow.left - block.left > 0.5 &&
                    (scrollsFromRight || flow.right > boxWidth - pad.right - right + 0.5)) {
                    claim.right = Math.max(claim.right, Math.ceil(pad.right + right + rect.width));
                }
            });

            items.forEach(item => {
                item.element.style.position = item.inline;

                // The measurement changed nothing the observer of the element is there for.
                item.mutations?.takeRecords();
            });

            return claim;
        }

        // The box a sticky element travels within: its parent, past any that generates no box of its own,
        // and the table for the parts of one, which pin within the whole table rather than their row.
        private static containingBlock(element: HTMLElement, style: CSSStyleDeclaration): HTMLElement {
            if (style.display.startsWith('table-')) {
                const table = element.closest('table');

                if (table) return table;
            }

            let node = element.parentElement ?? document.documentElement;

            while (node.parentElement && getComputedStyle(node).display === 'contents') {
                node = node.parentElement;
            }

            return node;
        }

        // What decides the edges the element pins to and the insets it pins at, as one comparable string.
        private static geometry(element: HTMLElement): string {
            const style = getComputedStyle(element);

            return `${style.position}|${style.top}|${style.bottom}|${style.left}|${style.right}|${style.direction}|${style.writingMode}`;
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
            const frame = Stickies.scrollportFrame(scope);

            // How far the pinning has carried the element from its place in the flow, measured in the
            // frame of the content of the scrollport - so that scrolling alone, which moves the element
            // and its flow position together, never shows up in it and only the pinning does.
            const shiftY = (rect.top - origin.top + origin.scrollTop) - flow.top;
            const shiftX = (rect.left - origin.left + origin.scrollLeft) - flow.left;

            // Half a pixel of tolerance on each comparison: a pinned edge sits exactly on its
            // boundary, and sub-pixel layout puts it a fraction to either side of it. The same
            // tolerance on the shift, where it is what tells a pinned element from a resting one.
            let edges = 0;

            if (shiftY > 0.5 && style.top !== 'auto' && rect.top <= frame.top + Stickies.pixels(style.top, frame.height) + 0.5) {
                edges |= Stickies.EDGE_TOP;
            }

            if (shiftY < -0.5 && style.bottom !== 'auto' && rect.bottom >= frame.top + frame.height - Stickies.pixels(style.bottom, frame.height) - 0.5) {
                edges |= Stickies.EDGE_BOTTOM;
            }

            if (shiftX > 0.5 && style.left !== 'auto' && rect.left <= frame.left + Stickies.pixels(style.left, frame.width) + 0.5) {
                edges |= Stickies.EDGE_LEFT;
            }

            if (shiftX < -0.5 && style.right !== 'auto' && rect.right >= frame.left + frame.width - Stickies.pixels(style.right, frame.width) - 0.5) {
                edges |= Stickies.EDGE_RIGHT;
            }

            return edges;
        }

        // The box the engine pins a sticky element within, in viewport coordinates: the content box of its
        // scroller - inside the border (the client offsets) and inside the padding as well, as a pinned edge
        // measured in a padded container confirms - or the viewport when that is the page. The client sizes
        // rather than the window's inner ones, which include the scrollbars - an edge no sticky element can
        // ever be pinned under.
        private static scrollportFrame(scope: HTMLElement | Window): { top: number, left: number, width: number, height: number, padding: BitStickySides } {
            if (scope === window) {
                return {
                    top: 0,
                    left: 0,
                    width: document.documentElement.clientWidth,
                    height: document.documentElement.clientHeight,
                    padding: { top: 0, bottom: 0, left: 0, right: 0 }
                };
            }

            const box = scope as HTMLElement;
            const style = getComputedStyle(box);
            const rect = box.getBoundingClientRect();

            const padding = {
                top: parseFloat(style.paddingTop) || 0,
                bottom: parseFloat(style.paddingBottom) || 0,
                left: parseFloat(style.paddingLeft) || 0,
                right: parseFloat(style.paddingRight) || 0
            };

            return {
                top: rect.top + box.clientTop + padding.top,
                left: rect.left + box.clientLeft + padding.left,
                width: box.clientWidth - padding.left - padding.right,
                height: box.clientHeight - padding.top - padding.bottom,
                padding
            };
        }

        // A length out of a computed style, in pixels. A percentage stays a percentage in the computed
        // style, resolved here against the size the sticky algorithm (or the scroll padding) resolves it
        // against; anything else unresolved (auto, a calc() mixing units) reads as nothing.
        private static pixels(value: string, size: number): number {
            return value.endsWith('%') ? (parseFloat(value) || 0) * size / 100 : (parseFloat(value) || 0);
        }

        // Where the element sits in the flow of its scrollport, which is the one thing about a pinned
        // element that cannot be read off it while it is pinned: every geometry it reports carries the
        // sticky offset already, down to offsetTop. So the offset is taken off for the length of a
        // single measurement - a sticky box and a static one are laid out in exactly the same place,
        // so nothing but the offset goes with it, and nothing is painted in between - and the reading
        // is normalized by the scroll offset, which is what makes it the same number at every scroll
        // position and lets it be taken while the element is already pinned.
        private static flowPosition(element: HTMLElement, scope: HTMLElement | Window, mutations?: MutationObserver): { top: number, left: number } {
            const position = element.style.position;

            element.style.position = 'static';

            const rect = element.getBoundingClientRect();
            const origin = Stickies.scopeOrigin(scope);

            // The property is put back rather than cleared: the inline style of the element may carry
            // a position of the page's own, and this one is only meant to last for the measurement.
            element.style.position = position;

            // The measurement changed nothing the observer of the element is there for.
            mutations?.takeRecords();

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
