namespace BitBlazorUI {
    export namespace Utils {

        // Every element currently held by one or more popups, against the inline values it carried before
        // the first of them took it over, and the keys still holding it.
        export const _scrollLocks = new Map<HTMLElement, { keys: Set<string>, overflow: string, paddingRight: string }>();
        // The element each key holds, so that releasing a key hands back the one that key actually took.
        export const _scrollLockOwners = new Map<string, HTMLElement>();

        // The scroller a caller named, as an element or as a selector; the page is what is meant when it
        // names neither.
        export function resolveScroller(scroller: string | HTMLElement | null) {
            return (scroller instanceof HTMLElement
                ? scroller
                : (scroller ? document.querySelector(scroller) : document.body)) as HTMLElement | null;
        }

        // The one place an element's overflow is taken over. Every popup that holds a scroller comes
        // through here - the counted lock below and the older toggle further down alike - because two
        // mechanisms writing element.style.overflow with bookkeeping of their own undo each other:
        // whichever hands the element back last wins, which leaves the page scrolling behind a popup that
        // is still open, or frozen after every popup has closed.
        // The holds are counted rather than toggled: two popups open at once both hold the page, and the
        // page is only handed back once the last of them lets go.
        // Taking the scrollbar away narrows the element by its width, which shifts the whole page sideways
        // in the same frame the popup appears in; the room it took is added back as padding so nothing
        // moves. Only the callers that ask for that compensation get it, so the older toggle keeps behaving
        // exactly as it always did.
        export function holdScroll(key: string, element: HTMLElement | null, compensate: boolean) {
            if (!element || Utils._scrollLockOwners.has(key)) return;

            Utils._scrollLockOwners.set(key, element);

            const held = Utils._scrollLocks.get(element);
            if (held) {
                held.keys.add(key);
                return;
            }

            const style = element.style;
            // What the element carried of its own, so that handing it back restores exactly that -
            // including the case of it having carried nothing, which is an empty string here.
            Utils._scrollLocks.set(element, { keys: new Set([key]), overflow: style.overflow, paddingRight: style.paddingRight });

            if (compensate) {
                const scrollbar = Utils.scrollbarWidth(element);
                if (scrollbar > 0) {
                    const current = parseFloat(getComputedStyle(element).paddingRight) || 0;
                    style.paddingRight = `${current + scrollbar}px`;
                }
            }

            style.overflow = 'hidden';
        }

        // Releases what the given key holds, and hands the element back what it carried before the first
        // hold took it over - but only once no other key is still holding it.
        export function releaseScroll(key: string) {
            const element = Utils._scrollLockOwners.get(key);
            if (!element) return;

            Utils._scrollLockOwners.delete(key);

            const held = Utils._scrollLocks.get(element);
            if (!held) return;

            held.keys.delete(key);
            if (held.keys.size > 0) return;

            Utils._scrollLocks.delete(element);

            element.style.overflow = held.overflow;
            element.style.paddingRight = held.paddingRight;
        }

        // The room the scrollbar takes from the element's content box. offsetWidth counts the borders along
        // with the scrollbar, so measuring by offsetWidth alone compensates a bordered scroller by its
        // border width on every hold - shifting the content sideways by the very amount the compensation
        // exists to prevent, and doing it even where there is no scrollbar to take away at all.
        export function scrollbarWidth(element: HTMLElement) {
            if (element === document.body) return window.innerWidth - document.documentElement.clientWidth;

            const style = getComputedStyle(element);
            const borders = (parseFloat(style.borderLeftWidth) || 0) + (parseFloat(style.borderRightWidth) || 0);
            return element.offsetWidth - element.clientWidth - borders;
        }

        // Stops the page behind a popup from scrolling while it is open, which is what keeps the wheel and
        // the touch drag on the surface the user is looking at instead of on the page they cannot reach.
        // The scroller is named by the caller, as an element or as a selector; the page is what is held when
        // it names neither. An application shell that scrolls a region of its own names that region, since
        // the body of such a page never scrolls and holding it would hold nothing.
        export function lockScroll(key: string, scroller: string | HTMLElement | null) {
            try {
                Utils.holdScroll(key, Utils.resolveScroller(scroller), true);
            } catch (e) { console.error("BitBlazorUI.Utils.lockScroll:", e); }
        }

        // Gives up the hold the given key took, if it still has one.
        export function unlockScroll(key: string) {
            try {
                Utils.releaseScroll(key);
            } catch (e) { console.error("BitBlazorUI.Utils.unlockScroll:", e); }
        }

        // Every popup currently handing its gestures on, against the listeners it registered to do so.
        export const _scrollForwards = new Map<string, AbortController>();

        // A popup that leaves the page scrolling still covers that page with a layer of its own, and the
        // layer is fixed to the viewport: the wheel and the touch drag that land on it are chained to the
        // document, which in an application shell - or in any layout that scrolls a region of its own -
        // is not the thing that scrolls. The gesture is handed to that region here, so that the page a
        // popup was told not to hold moves the way the user expects it to.
        // Only what the browser would drop on the floor is forwarded: anything inside the layer that can
        // take the gesture itself - content that overflows its own box - is left to take it.
        export function forwardScroll(key: string, rootId: string, scroller: string | HTMLElement | null) {
            try {
                Utils.stopForwardScroll(key);

                const root = document.getElementById(rootId);
                const target = (scroller instanceof HTMLElement
                    ? scroller
                    : (scroller ? document.querySelector(scroller) : null)) as HTMLElement | null;
                if (!root || !target) return;

                const controller = new AbortController();
                const signal = controller.signal;
                Utils._scrollForwards.set(key, controller);

                // Whether something between the gesture and the layer takes it, which is the thing the
                // browser would hand it to on its own.
                const taken = (from: EventTarget | null, x: number, y: number) => {
                    let element = from instanceof HTMLElement ? from : (from instanceof Node ? from.parentElement : null);
                    while (element && element !== root) {
                        if (Utils.consumesScroll(element, x, y)) return true;
                        element = element.parentElement;
                    }
                    return false;
                };

                const forward = (event: Event, x: number, y: number) => {
                    if (x === 0 && y === 0) return;
                    if (taken(event.target, x, y)) return;

                    // Instant rather than the default: the region may carry scroll-behavior:smooth, which
                    // would animate every notch of the wheel and leave the page lagging behind the gesture.
                    target.scrollBy({ left: x, top: y, behavior: 'instant' });
                };

                root.addEventListener('wheel', (e: WheelEvent) => {
                    // Lines and pages are turned into the pixels scrollBy takes, so that a wheel reporting
                    // either of them moves the region by what the browser would have moved it by.
                    const lines = e.deltaMode === 1;
                    const pages = e.deltaMode === 2;
                    const x = e.deltaX * (lines ? 16 : (pages ? target.clientWidth : 1));
                    const y = e.deltaY * (lines ? 16 : (pages ? target.clientHeight : 1));
                    forward(e, x, y);
                }, { signal, passive: true });

                let lastX = 0, lastY = 0, tracking = false;

                root.addEventListener('touchstart', (e: TouchEvent) => {
                    // A single finger is a drag; anything else is a pinch, which is not a scroll.
                    tracking = e.touches.length === 1;
                    if (!tracking) return;

                    lastX = e.touches[0].clientX;
                    lastY = e.touches[0].clientY;
                }, { signal, passive: true });

                root.addEventListener('touchmove', (e: TouchEvent) => {
                    if (!tracking || e.touches.length !== 1) return;

                    const touch = e.touches[0];
                    // The finger and the content move opposite ways: dragging up scrolls down.
                    const x = lastX - touch.clientX;
                    const y = lastY - touch.clientY;
                    lastX = touch.clientX;
                    lastY = touch.clientY;
                    forward(e, x, y);
                }, { signal, passive: true });

                const release = () => { tracking = false; };
                root.addEventListener('touchend', release, { signal, passive: true });
                root.addEventListener('touchcancel', release, { signal, passive: true });
            } catch (e) { console.error("BitBlazorUI.Utils.forwardScroll:", e); }
        }

        // Takes back the forwarding registered under the given key, listeners and all.
        export function stopForwardScroll(key: string) {
            const controller = Utils._scrollForwards.get(key);
            if (!controller) return;

            Utils._scrollForwards.delete(key);
            controller.abort();
        }

        // Whether the walk from the gesture up to the popup's own layer ends at the given element. It does
        // when the element scrolls in the direction the gesture is going and still has room to do it in -
        // the thing the browser hands the gesture to on its own - and it also does when the element is a
        // scroller told to keep its overscroll to itself. That second case is what stops a gesture at the
        // end of a popup's content rather than carrying it on into the page behind: the browser honours
        // overscroll-behavior on its own everywhere else, but the layer that swallowed this gesture is
        // fixed to the viewport, so the chaining is being done by hand here and has to honour it too.
        export function consumesScroll(element: HTMLElement, x: number, y: number) {
            const style = getComputedStyle(element);
            const scrolls = (overflow: string) => overflow === 'auto' || overflow === 'scroll' || overflow === 'overlay';
            const contained = (behavior: string) => behavior === 'contain' || behavior === 'none';

            if (y !== 0 && scrolls(style.overflowY)) {
                const room = element.scrollHeight - element.clientHeight;
                if (room > 1 && (y < 0 ? element.scrollTop > 1 : element.scrollTop < room - 1)) return true;
                if (contained(style.overscrollBehaviorY)) return true;
            }

            if (x !== 0 && scrolls(style.overflowX)) {
                const room = element.scrollWidth - element.clientWidth;
                if (room > 1) {
                    const left = element.scrollLeft;
                    // Which way the offset runs depends on the writing direction. A left-to-right scroller
                    // reports 0 at its start and grows to the room it has; a right-to-left one reports 0 at
                    // its start and falls to minus that room. Taking the distance alone would read the two
                    // ends of a right-to-left scroller the wrong way round, so the range itself is what is
                    // worked out here. A right-to-left scroller reporting a positive offset is one of the
                    // older engines that never took up the negative range, and reads as the other case.
                    const max = (style.direction === 'rtl' && left <= 0) ? 0 : room;
                    const min = max - room;
                    // scrollBy moves the offset the way the delta points, whichever range it runs in.
                    if (x < 0 ? left > min + 1 : left < max - 1) return true;
                }
                if (contained(style.overscrollBehaviorX)) return true;
            }

            return false;
        }

        // The older shape of the hold above, for the components that take a scroller for as long as they
        // are open and want its scroll offset back. It goes through the same counted registry, so one of
        // these can no longer hand back a scroller that a lock - or another one of these - is still
        // holding. The scrollbar room is only compensated for where the caller asks for it, so the callers
        // that have always let the page shift by the width of the scrollbar carry on doing exactly that.
        export function toggleOverflow(key: string, selector: string | HTMLElement, isOpen: boolean, compensate?: boolean) {
            const element = Utils.resolveScroller(selector);

            if (!element) return 0;

            try {
                if (isOpen) {
                    Utils.holdScroll(key, element, compensate === true);
                } else {
                    Utils.releaseScroll(key);
                }

                return element.scrollTop;
            } catch (e) {
                console.error("BitBlazorUI.Utils.toggleOverflow:", e);
                return 0;
            }
        }

        // Scrolls a scroll container to an absolute offset on its scrolling axis. The axis is passed in
        // rather than guessed, since a container can be scrollable on both and only the component knows
        // which one its items are laid out along. The offset is measured from the start edge the content
        // flows from, so a horizontal RTL container (whose scrollLeft runs from 0 towards the negative)
        // is handed its negation. The smooth scroll is dropped for a reader who has asked for less motion.
        export function scrollTo(element: HTMLElement, offset: number, horizontal: boolean, smooth: boolean) {
            if (!element) return;

            try {
                const rtl = horizontal && getComputedStyle(element).direction === 'rtl';
                const reduce = smooth && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;

                element.scrollTo({
                    [horizontal ? 'left' : 'top']: rtl ? -offset : offset,
                    behavior: smooth && !reduce ? 'smooth' : 'auto'
                });
            } catch (e) { console.error("BitBlazorUI.Utils.scrollTo:", e); }
        }

        // Scrolls a scroll container to its far end. scrollHeight/scrollWidth overshoot the maximum
        // scroll offset, which the browser clamps, so no measuring of the viewport is needed here.
        export function scrollToEnd(element: HTMLElement, horizontal: boolean, smooth: boolean) {
            if (!element) return;

            try {
                Utils.scrollTo(element, horizontal ? element.scrollWidth : element.scrollHeight, horizontal, smooth);
            } catch (e) { console.error("BitBlazorUI.Utils.scrollToEnd:", e); }
        }

        // Scrolls a scroll container to a position measured off one of the children of an element inside
        // it rather than off the container itself, which is what a list that renders anything before its
        // items (a header) needs: the child is the one the items start at, and extraOffset is how far into
        // them to go. A list of items of differing sizes points at the item itself and passes no extra
        // offset; a virtualized one points at the spacer the items start after and passes the offset it
        // calculated from its item size. The offset is measured from the inner (padding) edge of the
        // container, so neither its border nor, in RTL, its right-hand side throws the item off its edge.
        export function scrollToChild(element: HTMLElement, container: HTMLElement, index: number, extraOffset: number, horizontal: boolean, smooth: boolean) {
            if (!element || !container) return;

            try {
                const child = container.children[index] as HTMLElement;
                if (!child) return;

                const box = element.getBoundingClientRect();
                const rect = child.getBoundingClientRect();

                let offset: number;
                if (horizontal) {
                    const innerLeft = box.left + element.clientLeft;
                    offset = getComputedStyle(element).direction === 'rtl'
                        ? (innerLeft + element.clientWidth) - rect.right - element.scrollLeft
                        : rect.left - innerLeft + element.scrollLeft;
                } else {
                    offset = rect.top - (box.top + element.clientTop) + element.scrollTop;
                }

                Utils.scrollTo(element, offset + extraOffset, horizontal, smooth);
            } catch (e) { console.error("BitBlazorUI.Utils.scrollToChild:", e); }
        }

        // Brings the element with the given id into view. The smooth scroll is a courtesy rather than a
        // requirement, so it is dropped for a reader who has asked for less motion - a page that slides
        // under someone with a vestibular disorder is worse than one that simply arrives. Passing focus
        // moves the keyboard along with the viewport, which is what an in-page link owes a reader who is
        // not looking at the scrollbar.
        export function scrollElementIntoView(targetElementId: string, focus: boolean = false) {
            const element = document.getElementById(targetElementId);
            if (!element) return;

            try {
                const reduced = typeof window.matchMedia === "function"
                    && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

                element.scrollIntoView({
                    behavior: reduced ? "auto" : "smooth",
                    block: "start",
                    inline: "nearest"
                });

                if (!focus) return;

                // An element that cannot take focus of its own is given a tab stop that only code can
                // reach, so the destination becomes focusable without becoming one more stop for everyone
                // tabbing through the page. One that is already focusable, or that was already given a
                // tabindex of its own, is left exactly as it is.
                if (element.tabIndex < 0 && !element.hasAttribute("tabindex")) {
                    element.setAttribute("tabindex", "-1");
                }

                // The scroll above has already put the element where it belongs; letting the focus scroll
                // to it as well would undo the alignment it was just given.
                element.focus({ preventScroll: true });
            } catch (e) { console.error("BitBlazorUI.Utils.scrollElementIntoView:", e); }
        }
    }
}
