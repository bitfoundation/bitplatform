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

        // Moves the focus to the first focusable element inside the given container, falling back to the
        // container itself (which the caller makes programmatically focusable with tabindex="-1") when it
        // holds nothing focusable, so the focus never stays behind on the element that opened the popup.
        public static focusFirstElement(elementId: string, selector?: string | null) {
            const container = document.getElementById(elementId);
            if (!container) return;

            // A caller-supplied selector says where the focus belongs when the first focusable element is
            // not it. It is tried on its own so a selector that is invalid, or that matches nothing
            // visible, falls through to the default rather than leaving the focus behind on the page.
            if (selector) {
                try {
                    const preferred = Array.from(container.querySelectorAll<HTMLElement>(selector)).find(Utils.isFocusable);
                    if (preferred) {
                        preferred.focus();
                        return;
                    }
                } catch (e) { console.error("BitBlazorUI.Utils.focusFirstElement:", e); }
            }

            try {
                // The same set the focus trap cycles through, so the element the focus lands on when the
                // popup opens is the same one Shift+Tab wraps back to from the end of it.
                const candidates = Array.from(container.querySelectorAll<HTMLElement>(Utils._focusables));

                // The consumer naming the element the focus should land on, for the popups whose first
                // focusable element is not the one worth starting at - a dismiss button ahead of the field
                // the popup was opened to fill in. The first focusable element is the fallback.
                // The standard autofocus attribute says the same thing and is what a native dialog reads,
                // so it is honoured alongside the data- one: the browser only ever acts on it for markup
                // that was in the document when it was parsed, which a popup's content never is.
                const requested = candidates.find(el =>
                    (el.hasAttribute('data-autofocus') || el.hasAttribute('autofocus')) && Utils.isFocusable(el));

                (requested ?? candidates.find(Utils.isFocusable) ?? container).focus();
            } catch (e) { console.error("BitBlazorUI.Utils.focusFirstElement:", e); }
        }

        // Mirrors the popup relationship onto the element the user actually reaches. A callout renders its
        // anchor as a plain container around the consumer's own trigger, and aria-haspopup, aria-controls
        // and aria-expanded on a container that is neither focusable nor interactive are attributes no
        // screen reader ever reads: the button inside it is what the user lands on. The first focusable
        // descendant is that button; a container that holds none keeps the attributes on itself, where they
        // are at least on the element the relationship was declared for.
        // An empty hasPopup takes the attribute away again - but only where this is the code that put it
        // there, so a trigger that names a popup of its own (a dropdown used as an anchor) keeps its own.
        public static syncAriaPopup(anchorId: string, popupId: string, isOpen: boolean, hasPopup: string) {
            try {
                const anchor = document.getElementById(anchorId);
                if (!anchor) return;

                const trigger = anchor.querySelector<HTMLElement>(Utils._focusables) ?? anchor;

                trigger.setAttribute('aria-controls', popupId);
                trigger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');

                if (hasPopup) {
                    trigger.setAttribute('aria-haspopup', hasPopup);
                    trigger.setAttribute('data-bit-haspopup', '');
                } else if (trigger.hasAttribute('data-bit-haspopup')) {
                    trigger.removeAttribute('aria-haspopup');
                    trigger.removeAttribute('data-bit-haspopup');
                }
            } catch (e) { console.error("BitBlazorUI.Utils.syncAriaPopup:", e); }
        }

        // Mirrors the relationship a tooltip declares onto the element the reader actually lands on. The
        // tooltip renders the consumer's anchor inside a plain container of its own, and an aria-describedby
        // or an aria-labelledby on a container that is neither focusable nor interactive is an attribute no
        // screen reader ever reads: the control inside it is what the user reaches. The first focusable
        // descendant that is not part of the tooltip surface itself is that control.
        // An empty attribute takes the mirrored one away again - and only ever the one this code wrote, so
        // an anchor that names a description of its own keeps it.
        public static syncAriaDescription(rootId: string, tooltipId: string, attribute: string) {
            try {
                const root = document.getElementById(rootId);
                if (!root) return;

                // An interactive tooltip may hold something focusable of its own, which sits inside the same
                // root and would otherwise be taken for the anchor whenever the anchor holds none itself.
                const target = Array.from(root.querySelectorAll<HTMLElement>(Utils._focusables))
                    .find(el => el.closest('.bit-ttp-wrp') === null);

                // Nothing focusable to mirror onto: the markup has already declared the relationship on the
                // root, which is where it stays.
                if (!target) return;

                const mirrored = target.getAttribute('data-bit-ttp-aria');

                if (mirrored && mirrored !== attribute) {
                    target.removeAttribute(mirrored);
                    target.removeAttribute('data-bit-ttp-aria');
                }

                if (!attribute) return;

                // The anchor names a description or a label of its own, which is the consumer's to decide.
                if (mirrored !== attribute && target.hasAttribute(attribute)) return;

                target.setAttribute(attribute, tooltipId);
                target.setAttribute('data-bit-ttp-aria', attribute);
            } catch (e) { console.error("BitBlazorUI.Utils.syncAriaDescription:", e); }
        }

        // True when the focus currently sits inside the given container. The popup components ask before
        // they close, since handing the focus back to the element that opened them is only correct when
        // the focus was theirs to hand back - moving it out of wherever the user put it otherwise.
        public static containsActiveElement(elementId: string) {
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

        // Whether the pointer of the device is one that can actually hover, which the interactions that
        // are driven by hovering have to know: a touch screen reports a mouseover for a tap, so a popup
        // opening on hover would fight the tap that is also meant to toggle it.
        public static isHoverDevice() {
            try {
                return window.matchMedia('(hover: hover) and (pointer: fine)').matches;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isHoverDevice:", e);
                return false;
            }
        }

        private static _focusTraps = new Map<string, AbortController>();

        // Keeps Tab and Shift+Tab cycling inside the given container for as long as it is registered, which
        // is what a popup that takes the keyboard over has to do: the tab order runs on into the page behind
        // it otherwise, leaving the focus somewhere an overlay swallows every click that could bring it back.
        // Registering again on the same element replaces the previous registration.
        public static setupFocusTrap(elementId: string) {
            Utils.disposeFocusTrap(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            element.addEventListener('keydown', e => {
                if (e.key !== 'Tab') return;

                // A trap registered on something nested inside this one - a dialog opened from inside this
                // dialog - owns the key first, and the event carries on bubbling up to here afterwards.
                // Without this the outer trap would wrap the focus a second time, over the decision the
                // inner one has already made, and land it somewhere neither of them meant.
                if (Utils.hasNearerFocusTrap(element, e.target as Element | null)) return;

                Utils.wrapFocus(element, e);
            }, { signal: controller.signal });

            Utils._focusTraps.set(elementId, controller);
        }

        // Whether a trap is registered on something between the given container and the element the key was
        // pressed on - the container itself excluded, since that is the trap asking.
        private static hasNearerFocusTrap(root: HTMLElement, target: Element | null) {
            let node = target;

            while (node && node !== root) {
                if (node.id && Utils._focusTraps.has(node.id)) return true;

                node = node.parentElement;
            }

            return false;
        }

        public static disposeFocusTrap(elementId: string) {
            const controller = Utils._focusTraps.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._focusTraps.delete(elementId);
        }

        private static _focusOrigins = new Map<string, HTMLElement>();

        // Remembers the element the focus was on at the moment a popup took it over, keyed by the popup, so
        // that closing the popup can hand the keyboard back to where it came from. A popup that moves the
        // focus into itself and then takes its content away leaves the focus on the body, which sends the
        // keyboard back to the top of the page - the one thing the WAI-ARIA dialog pattern asks not to happen.
        public static captureFocusOrigin(elementId: string) {
            try {
                const active = document.activeElement as HTMLElement | null;

                // The body is not somewhere the focus can be handed back to, and neither is an element that
                // is inside the popup itself: the focus was already there, so there is nothing to restore.
                if (!active || active === document.body) return;

                const container = document.getElementById(elementId);
                if (container?.contains(active)) return;

                Utils._focusOrigins.set(elementId, active);
            } catch (e) { console.error("BitBlazorUI.Utils.captureFocusOrigin:", e); }
        }

        // Hands the focus back to the element captureFocusOrigin remembered, and forgets it either way, so a
        // popup that is opened again captures anew. The focus is only ours to hand back while it is still in
        // the popup - or was dropped to the body by the popup being hidden - so a focus the user has since
        // moved somewhere else of their own accord is left alone.
        public static restoreFocusOrigin(elementId: string) {
            try {
                const origin = Utils._focusOrigins.get(elementId);
                if (!origin) return;

                Utils._focusOrigins.delete(elementId);

                // The element that held the focus may have been taken off the page while the popup was open.
                if (!origin.isConnected) return;

                const active = document.activeElement;
                const container = document.getElementById(elementId);
                const ours = active == null || active === document.body || (container?.contains(active) ?? false);
                if (!ours) return;

                origin.focus();
            } catch (e) { console.error("BitBlazorUI.Utils.restoreFocusOrigin:", e); }
        }

        public static disposeFocusOrigin(elementId: string) {
            Utils._focusOrigins.delete(elementId);
        }

        private static _transitionEnds = new Map<string, AbortController>();

        // Tells .NET when a surface has finished sliding in or out. What a component knows on its own is the
        // frame the state changed on, which is the start of the animation rather than the end of it: the
        // content of a closed surface cannot be taken out of the page before it has finished sliding away, and
        // whatever is measured or focused after an opening has to wait for the surface to have arrived.
        // Only the transform is listened for - a surface transitions its opacity and its visibility as well,
        // and all three would report the same one movement - and only on the element itself, so a transition
        // running somewhere in the content is not mistaken for the surface arriving.
        public static setupTransitionEnd(elementId: string, dotnetObj: DotNetObject) {
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

        public static disposeTransitionEnd(elementId: string) {
            const controller = Utils._transitionEnds.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._transitionEnds.delete(elementId);
        }

        // Remembers the element the focus was on before a popup took it over, so that closing the popup can
        // hand the focus back to whatever opened it. A popup that leaves the focus behind on an element it is
        // about to remove drops the keyboard user back at the top of the page, which is the one place they
        // never navigated to. The body is not an element worth handing anything back to, so it is recorded
        // as "nothing to restore" rather than as an origin.
        public static storeFocus(key: string) {
            try {
                const active = document.activeElement as HTMLElement | null;

                if (!active || active === document.body || active === document.documentElement || typeof active.focus !== 'function') {
                    Utils._focusOrigins.delete(key);
                    return;
                }

                Utils._focusOrigins.set(key, active);
            } catch (e) { console.error("BitBlazorUI.Utils.storeFocus:", e); }
        }

        // Hands the focus back to the element storeFocus recorded under the same key, and forgets it either
        // way - a stored origin is only ever restored once. `onlyWhenLost` is the guard for the usual case:
        // the focus is only the popup's to hand back while it is still where the popup left it, which after
        // the popup is taken out of the page means nowhere (the browser drops it on the body). A focus that
        // has since moved somewhere else belongs to whoever moved it.
        public static restoreFocus(key: string, onlyWhenLost: boolean) {
            const element = Utils._focusOrigins.get(key);
            Utils._focusOrigins.delete(key);

            if (!element) return;

            try {
                if (onlyWhenLost) {
                    const active = document.activeElement;
                    if (active && active !== document.body && active !== document.documentElement) return;
                }

                if (!element.isConnected) return;

                element.focus();
            } catch (e) { console.error("BitBlazorUI.Utils.restoreFocus:", e); }
        }

        // Drops a stored origin without focusing it, for a component that is disposed while its popup is
        // still open: there is no close for the focus to be handed back on, and the map would otherwise keep
        // the element alive for as long as the page lives.
        public static forgetFocus(key: string) {
            Utils._focusOrigins.delete(key);
        }

        // Every element currently held by one or more popups, against the inline values it carried before
        // the first of them took it over, and the keys still holding it.
        private static _scrollLocks = new Map<HTMLElement, { keys: Set<string>, overflow: string, paddingRight: string }>();
        // The element each key holds, so that releasing a key hands back the one that key actually took.
        private static _scrollLockOwners = new Map<string, HTMLElement>();

        // The scroller a caller named, as an element or as a selector; the page is what is meant when it
        // names neither.
        private static resolveScroller(scroller: string | HTMLElement | null) {
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
        private static holdScroll(key: string, element: HTMLElement | null, compensate: boolean) {
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
        private static releaseScroll(key: string) {
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
        private static scrollbarWidth(element: HTMLElement) {
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
        public static lockScroll(key: string, scroller: string | HTMLElement | null) {
            try {
                Utils.holdScroll(key, Utils.resolveScroller(scroller), true);
            } catch (e) { console.error("BitBlazorUI.Utils.lockScroll:", e); }
        }

        // Gives up the hold the given key took, if it still has one.
        public static unlockScroll(key: string) {
            try {
                Utils.releaseScroll(key);
            } catch (e) { console.error("BitBlazorUI.Utils.unlockScroll:", e); }
        }

        // Every popup currently handing its gestures on, against the listeners it registered to do so.
        private static _scrollForwards = new Map<string, AbortController>();

        // A popup that leaves the page scrolling still covers that page with a layer of its own, and the
        // layer is fixed to the viewport: the wheel and the touch drag that land on it are chained to the
        // document, which in an application shell - or in any layout that scrolls a region of its own -
        // is not the thing that scrolls. The gesture is handed to that region here, so that the page a
        // popup was told not to hold moves the way the user expects it to.
        // Only what the browser would drop on the floor is forwarded: anything inside the layer that can
        // take the gesture itself - content that overflows its own box - is left to take it.
        public static forwardScroll(key: string, rootId: string, scroller: string | HTMLElement | null) {
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
        public static stopForwardScroll(key: string) {
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
        private static consumesScroll(element: HTMLElement, x: number, y: number) {
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

        private static _preventedKeys = new Map<string, AbortController>();

        // Suppresses the default behavior (page scrolling) of the given keys on an element, for the
        // components whose keyboard logic runs in Blazor keydown handlers, which cannot decide to
        // preventDefault per key. Registering again on the same element replaces the previous keys.
        public static preventDefaultKeys(elementId: string, keys: string[]) {
            Utils.disposePreventDefaultKeys(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            // A modified key is a shortcut of the browser or of the operating system rather than the key
            // the component handles, so its default action is left alone.
            element.addEventListener('keydown', (e: KeyboardEvent) => {
                if (keys.indexOf(e.key) !== -1 && !e.shiftKey && !e.ctrlKey && !e.altKey && !e.metaKey) {
                    e.preventDefault();
                }
            }, { signal: controller.signal });

            Utils._preventedKeys.set(elementId, controller);
        }

        public static disposePreventDefaultKeys(elementId: string) {
            const controller = Utils._preventedKeys.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._preventedKeys.delete(elementId);
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

        // Scrolls a scroll container to an absolute offset on its scrolling axis. The axis is passed in
        // rather than guessed, since a container can be scrollable on both and only the component knows
        // which one its items are laid out along.
        public static scrollTo(element: HTMLElement, offset: number, horizontal: boolean, smooth: boolean) {
            if (!element) return;

            try {
                element.scrollTo({
                    [horizontal ? 'left' : 'top']: offset,
                    behavior: smooth ? 'smooth' : 'auto'
                });
            } catch (e) { console.error("BitBlazorUI.Utils.scrollTo:", e); }
        }

        // Scrolls a scroll container to its far end. scrollHeight/scrollWidth overshoot the maximum
        // scroll offset, which the browser clamps, so no measuring of the viewport is needed here.
        // The far end of an RTL container sits at a negative scrollLeft, so the offset is negated there.
        public static scrollToEnd(element: HTMLElement, horizontal: boolean, smooth: boolean) {
            if (!element) return;

            try {
                const rtl = horizontal && getComputedStyle(element).direction === 'rtl';
                const offset = horizontal
                    ? (rtl ? -element.scrollWidth : element.scrollWidth)
                    : element.scrollHeight;

                Utils.scrollTo(element, offset, horizontal, smooth);
            } catch (e) { console.error("BitBlazorUI.Utils.scrollToEnd:", e); }
        }

        // Scrolls a scroll container to a position measured off one of its children rather than off the
        // container itself, which is what a list that renders anything before its items (a header) needs:
        // the child is the one the items start at, and extraOffset is how far into them to go. A list of
        // items of differing sizes points at the item itself and passes no extra offset; a virtualized one
        // points at the spacer the items start after and passes the offset it calculated from its item size.
        public static scrollToChild(element: HTMLElement, index: number, extraOffset: number, horizontal: boolean, smooth: boolean) {
            if (!element) return;

            try {
                const child = element.children[index] as HTMLElement;
                if (!child) return;

                const offset = horizontal
                    ? child.getBoundingClientRect().left - element.getBoundingClientRect().left + element.scrollLeft
                    : child.getBoundingClientRect().top - element.getBoundingClientRect().top + element.scrollTop;

                Utils.scrollTo(element, offset + extraOffset, horizontal, smooth);
            } catch (e) { console.error("BitBlazorUI.Utils.scrollToChild:", e); }
        }

        // Brings the element with the given id into view. The smooth scroll is a courtesy rather than a
        // requirement, so it is dropped for a reader who has asked for less motion - a page that slides
        // under someone with a vestibular disorder is worse than one that simply arrives. Passing focus
        // moves the keyboard along with the viewport, which is what an in-page link owes a reader who is
        // not looking at the scrollbar.
        public static scrollElementIntoView(targetElementId: string, focus: boolean = false) {
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
        // preventing it would keep the control from taking the focus (and would keep the text of an
        // input inside it from being selected with the pointer). The event itself is always left to
        // travel on: Blazor dispatches pointerdown from a single listener on the document, so
        // stopping it here would take it away from every Blazor handler in the tree, including the
        // ones of the components sitting inside the element. Calling it again updates the active
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

                    if (e.target instanceof Element) {
                        // The lookup is bounded by the element itself, since a control the element
                        // happens to sit inside of (a card that is a link, for one) says nothing
                        // about what was pressed within it. An explicit contenteditable="false"
                        // marks content that is not editable after all, so it is not a control here.
                        const control = e.target.closest(
                            'button,a,input,textarea,select,[contenteditable]:not([contenteditable="false"])');

                        if (control && element !== control && element.contains(control)) return;
                    }

                    e.preventDefault();
                });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventPointerDown:", e); }
        }

        // Registers a wheel listener on the element that suppresses the scrolling of the page while
        // the element handles the wheel itself. It is registered here rather than through Blazor's
        // preventDefault directive because that one goes through a delegated listener the browser
        // treats as passive, which makes preventing a wheel a no-op before net10.0. Calling it again
        // updates the flags in place, so no separate unregister call is needed - the listener is
        // garbage-collected with the element itself. An element that never turns the suppression
        // on gets no listener at all, since a non-passive wheel listener is not free to the browser.
        // Only the wheel the element actually consumes is taken from the page: with verticalOnly a
        // scroll that runs mostly sideways is left alone, the same way the element leaves it alone.
        public static registerPreventWheel(element: HTMLElement, active: boolean, verticalOnly: boolean) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventWheel = active;
                el.__bitPreventWheelVerticalOnly = verticalOnly;

                if (active === false) return;
                if (el.__bitPreventWheelRegistered) return;
                el.__bitPreventWheelRegistered = true;

                element.addEventListener('wheel', (e: WheelEvent) => {
                    const el = element as any;

                    if (!el.__bitPreventWheel) return;
                    if (el.__bitPreventWheelVerticalOnly && Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;

                    e.preventDefault();
                }, { passive: false });
            } catch (e) { console.error("BitBlazorUI.Utils.registerPreventWheel:", e); }
        }

        // Registers a keydown listener on the element that suppresses the browser's default action
        // for the given keys (e.g. PageUp/PageDown scrolling the page while a spinbutton handles
        // them as value changes). Calling it again updates the key list in place, and an empty list
        // effectively disables the suppression, so no separate unregister call is needed - the
        // listener is garbage-collected with the element itself.
        // A key typed into an editable element inside the container (an input in a carousel slide,
        // for example) belongs to that element (the arrow keys move its caret), so its default
        // action is left alone. The event itself still travels on: Blazor dispatches keydown from a
        // single listener on the document, so stopping it here would take the key away from the
        // editable element's own handler too, which is the one it was being left to.
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

        // Whether an element matching the set above is a place the focus can actually land. It is the one
        // answer both the initial focus and the focus trap ask for, so the element the focus is moved to
        // when a popup opens is the same one Shift+Tab wraps back to from the end of it.
        // A hidden element has no box at all - which is how a display:none subtree (e.g. a collapsed
        // section) is skipped without measuring every ancestor - and visibility:hidden leaves a box the
        // focus still cannot land in, so it is asked about separately, and only for the elements that got
        // past the cheap measurement.
        private static isFocusable(el: HTMLElement) {
            const hasBox = el.offsetWidth > 0 || el.offsetHeight > 0 || el.getClientRects().length > 0;

            return hasBox && getComputedStyle(el).visibility !== 'hidden';
        }

        // Keeps Tab and Shift+Tab cycling inside a container, which is what a popup that reports itself a modal
        // dialog has to do: the tab order runs on into the page behind it otherwise, leaving the focus somewhere
        // an overlay swallows every click that could bring it back. The keydown is left alone where the focus is
        // not on the edge of the container, so tabbing within it moves as it normally would.
        public static wrapFocus(root: HTMLElement, e: KeyboardEvent) {
            if (!root) return;

            // A hidden element is not a place the focus can land, and a callout carries parts that are only
            // rendered for some of its states.
            const focusables = Array.from(root.querySelectorAll<HTMLElement>(Utils._focusables))
                .filter(Utils.isFocusable);

            if (focusables.length === 0) {
                // Nothing inside the container can take the focus, which leaves the container itself
                // holding it - the components that trap the focus make it programmatically focusable for
                // exactly this case. Tabbing on from there would walk straight out of the trap and into
                // the page behind it, so the key is swallowed instead of being left to the browser.
                if (document.activeElement === root) {
                    e.preventDefault();
                }
                return;
            }

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
        public static getOverflowMetrics(containerId: string, childSelector: string) {
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

        // Moves the focus between the items of a popup (a menu, an overflow list, ...) that a component
        // drives from its keydown handlers. The items are the elements of `container` matching `selector`,
        // in DOM order, minus the disabled ones. `mode` is one of first/last/next/prev/char, where next
        // and prev wrap around and char jumps to the next item whose text starts with `char`.
        public static focusItem(containerId: string, selector: string, mode: string, char: string | null) {
            const container = document.getElementById(containerId);
            if (!container) return;

            try {
                const items = (Array.from(container.querySelectorAll(selector)) as HTMLElement[])
                    .filter(el => !(el as HTMLButtonElement).disabled && el.getAttribute('aria-disabled') !== 'true');
                if (items.length === 0) return;

                const current = items.indexOf(document.activeElement as HTMLElement);
                let index = -1;

                if (mode === 'first') {
                    index = 0;
                } else if (mode === 'last') {
                    index = items.length - 1;
                } else if (mode === 'next') {
                    index = current < 0 ? 0 : (current + 1) % items.length;
                } else if (mode === 'prev') {
                    index = current < 0 ? items.length - 1 : (current - 1 + items.length) % items.length;
                } else if (mode === 'char' && char) {
                    const c = char.toLowerCase();
                    const start = current < 0 ? 0 : current + 1;
                    for (let i = 0; i < items.length; i++) {
                        const candidate = (start + i) % items.length;
                        if ((items[candidate].textContent || '').trim().toLowerCase().indexOf(c) === 0) {
                            index = candidate;
                            break;
                        }
                    }
                }

                if (index > -1) {
                    items[index].focus();
                }
            } catch (e) {
                console.error("BitBlazorUI.Utils.focusItem:", e);
            }
        }

        public static setStyle(element: HTMLElement, key: string, value: string) {
            if (!element || !element.style) return;

            try {
                (element.style as any)[key] = value;
            } catch (e) { console.error("BitBlazorUI.Utils.setStyle:", e); }
        }

        // The older shape of the hold above, for the components that take a scroller for as long as they
        // are open and want its scroll offset back. It goes through the same counted registry, so one of
        // these can no longer hand back a scroller that a lock - or another one of these - is still
        // holding. The scrollbar room is only compensated for where the caller asks for it, so the callers
        // that have always let the page shift by the width of the scrollbar carry on doing exactly that.
        public static toggleOverflow(key: string, selector: string | HTMLElement, isOpen: boolean, compensate?: boolean) {
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