namespace BitBlazorUI {
    export class Utils {
        public static MIN_MOBILE_WIDTH = 320;
        public static MAX_MOBILE_WIDTH = 600;

        public static getBodyWidth() {
            return document.body.offsetWidth;
        }

        // Calls fn at most once per delay. The first call of a window goes at once; with trailing, the latest call
        // made during the window is held and made when the window closes (opening a new one), so a value that comes
        // to rest is reported where it rests rather than where the window began. cancel() drops a held call and
        // closes the window, so the next call goes at once again.
        public static throttle(fn: Function, delay: number, options?: { trailing?: boolean }) {
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

        // Whether the viewport can be shrunk by an on-screen keyboard, so that a resize or a short visible band
        // may be the keyboard's doing rather than the window's. It is the PRIMARY pointer being coarse that says
        // so, not touch merely being available: a touch-screen laptop has a touch screen and a physical keyboard,
        // and a window resized there is the window being resized. A phone, a tablet and a convertible folded
        // into its tablet posture all report a coarse primary pointer, whatever the width of their screen.
        public static hasOnScreenKeyboard() {
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
        // Reports whether the attributes landed on a trigger inside the container, which is what tells the
        // component to stop declaring them on the container too: aria-expanded and aria-haspopup are not
        // allowed on an element without a role, so a copy left there is invalid as well as redundant.
        // The content of the container is the consumer's, and it can swap the trigger for another one - or
        // for nothing focusable at all - without the popup changing state, which is the only time the
        // component calls this again. So the container is watched for as long as it is registered, and the
        // relationship is moved onto whatever the trigger has become, taken off the one it was on before.
        public static syncAriaPopup(anchorId: string, popupId: string, isOpen: boolean, hasPopup: string): boolean {
            try {
                const anchor = document.getElementById(anchorId);
                if (!anchor) return false;

                let state = Utils._ariaPopups.get(anchorId);

                // The container was rendered anew (the anchor taken away and given back), so the one the
                // watch was on is gone.
                if (state && state.anchor !== anchor) {
                    state.observer?.disconnect();
                    state = undefined;
                }

                if (!state) {
                    state = { anchor, popupId, isOpen, hasPopup, target: null, observer: null };

                    if (typeof MutationObserver !== 'undefined') {
                        const watched = state;
                        watched.observer = new MutationObserver(() => {
                            if (!watched.anchor.isConnected) {
                                Utils.disposeAriaPopup(anchorId);
                                return;
                            }

                            Utils.applyAriaPopup(watched);
                        });
                        watched.observer.observe(anchor, { childList: true, subtree: true });
                    }

                    Utils._ariaPopups.set(anchorId, state);
                }

                state.popupId = popupId;
                state.isOpen = isOpen;
                state.hasPopup = hasPopup;

                return Utils.applyAriaPopup(state) !== anchor;
            } catch (e) { console.error("BitBlazorUI.Utils.syncAriaPopup:", e); return false; }
        }

        public static disposeAriaPopup(anchorId: string) {
            const state = Utils._ariaPopups.get(anchorId);
            if (!state) return;

            state.observer?.disconnect();
            Utils._ariaPopups.delete(anchorId);
        }

        private static _ariaPopups = new Map<string, {
            anchor: HTMLElement,
            popupId: string,
            isOpen: boolean,
            hasPopup: string,
            target: HTMLElement | null,
            observer: MutationObserver | null
        }>();

        private static applyAriaPopup(state: { anchor: HTMLElement, popupId: string, isOpen: boolean, hasPopup: string, target: HTMLElement | null }) {
            const anchor = state.anchor;
            const trigger = anchor.querySelector<HTMLElement>(Utils._focusables) ?? anchor;

            // The element that carried the relationship before is no longer the trigger - another control took
            // its place, or it lost the last focusable element to the container itself - so what this code put
            // on it is taken back off, leaving one element in the container that claims the popup.
            const previous = state.target;
            if (previous && previous !== trigger && previous.isConnected) {
                previous.removeAttribute('aria-controls');
                previous.removeAttribute('aria-expanded');

                if (previous.hasAttribute('data-bit-haspopup')) {
                    previous.removeAttribute('aria-haspopup');
                    previous.removeAttribute('data-bit-haspopup');
                }
            }

            state.target = trigger;

            trigger.setAttribute('aria-controls', state.popupId);
            trigger.setAttribute('aria-expanded', state.isOpen ? 'true' : 'false');

            if (state.hasPopup) {
                trigger.setAttribute('aria-haspopup', state.hasPopup);
                trigger.setAttribute('data-bit-haspopup', '');
            } else if (trigger.hasAttribute('data-bit-haspopup')) {
                trigger.removeAttribute('aria-haspopup');
                trigger.removeAttribute('data-bit-haspopup');
            }

            return trigger;
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

        private static _tooltips = new Map<string, { root: HTMLElement, dotnetObj: DotNetObject, controller: AbortController }>();
        private static _tooltipsByRoot = new Map<HTMLElement, DotNetObject>();
        private static _tooltipsController: AbortController | null = null;

        // An element that answers Escape itself - a text entry clears or reverts on it, a combobox or anything
        // expanded closes what it opened - so a tooltip around it lets the key through to that element.
        private static readonly _escapeOwners =
            'input:not([type="button"],[type="submit"],[type="reset"],[type="checkbox"],[type="radio"],[type="image"],[type="range"],[type="color"],[type="file"]),' +
            'textarea,select,[contenteditable]:not([contenteditable="false"]),' +
            '[role="combobox"],[role="searchbox"],[role="textbox"],[role="spinbutton"],[aria-expanded="true"]';

        // Lets Escape dismiss a shown tooltip (WCAG 1.4.13 "dismissible") from the two places it can come from:
        // the keyboard inside the tooltip - on its anchor - and anywhere on the page while the pointer rests on
        // the tooltip, since a tooltip shown on hover is shown while the focus is wherever the user left it.
        // Either way the key is the tooltip's alone: it is taken before Blazor's document-level delegation sees
        // it, so a dialog or a callout the tooltip sits in is not dismissed by the same press, and a second
        // Escape reaches them as usual. The one exception is a key pressed on something inside the anchor that
        // answers Escape itself (a text field, a search box, a dropdown): the tooltip is dismissed along with
        // it, and the key goes on to the component it was pressed on. Whether a tooltip takes it is read off the
        // DOM on the spot - shown (bit-ttp-vis) and dismissible (data-bit-ttp-esc) - because the answer cannot
        // wait for a round trip. It also tells a tooltip a click opened about the press outside it that
        // dismisses it.
        public static setupTooltip(rootId: string, tooltipId: string, attribute: string, dotnetObj: DotNetObject) {
            Utils.disposeTooltip(rootId);

            Utils.syncAriaDescription(rootId, tooltipId, attribute);

            const root = document.getElementById(rootId);
            if (!root) return;

            const controller = new AbortController();

            // A component inside the anchor that answered the key natively itself (and said so) keeps it.
            root.addEventListener('keydown', e => {
                if (e.key !== 'Escape' || e.defaultPrevented) return;
                if (!root.querySelector(':scope > .bit-ttp-wrp.bit-ttp-vis[data-bit-ttp-esc]')) return;

                const target = e.target as Element | null;
                const owner = target?.closest(Utils._escapeOwners);
                if (!owner || !root.contains(owner) || owner.closest('.bit-ttp-wrp')) {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                }

                dotnetObj.invokeMethodAsync('OnEscape')
                         .catch(err => console.error("BitBlazorUI.Utils.setupTooltip:", err));
            }, { signal: controller.signal });

            Utils._tooltips.set(rootId, { root, dotnetObj, controller });
            Utils._tooltipsByRoot.set(root, dotnetObj);

            Utils.ensureTooltipListeners();
        }

        public static disposeTooltip(rootId: string) {
            const entry = Utils._tooltips.get(rootId);
            if (!entry) return;

            entry.controller.abort();
            Utils._tooltips.delete(rootId);
            if (Utils._tooltipsByRoot.get(entry.root) === entry.dotnetObj) {
                Utils._tooltipsByRoot.delete(entry.root);
            }

            if (Utils._tooltips.size === 0) {
                Utils._tooltipsController?.abort();
                Utils._tooltipsController = null;
            }
        }

        // The two document-level listeners every tooltip needs are shared by all of them, and each one asks the
        // DOM for the few tooltips that are actually shown instead of every tooltip on the page asking for
        // itself - a toolbar or a grid of a few hundred tooltips pays for one listener per key and press.
        private static ensureTooltipListeners() {
            if (Utils._tooltipsController) return;

            const controller = Utils._tooltipsController = new AbortController();

            const shown = (marker: string) => Array.from(document.querySelectorAll<HTMLElement>(`.bit-ttp-wrp.bit-ttp-vis[${marker}]`))
                .map(wrp => wrp.parentElement)
                .filter((root): root is HTMLElement => !!root && Utils._tooltipsByRoot.has(root));

            // In the capture phase, so the key is taken before whatever holds the focus acts on it. Every
            // tooltip under the pointer - a nested one along with the one around it - is dismissed by the press,
            // and stopImmediatePropagation keeps it from any other listener on the document as well, a callout's
            // own Escape listener included.
            document.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                const target = e.target as Node | null;
                let taken = false;

                for (const root of shown('data-bit-ttp-esc')) {
                    if (target && root.contains(target)) continue; // the root's own listener answers it
                    if (!root.matches(':hover')) continue;

                    Utils._tooltipsByRoot.get(root)!.invokeMethodAsync('OnEscape')
                         .catch(err => console.error("BitBlazorUI.Utils.setupTooltip:", err));
                    taken = true;
                }

                if (!taken) return;

                e.preventDefault();
                e.stopImmediatePropagation();
            }, { signal: controller.signal, capture: true });

            // A tooltip a press of the anchor opened (data-bit-ttp-clk) is dismissed by the next press elsewhere.
            // The focus leaving the anchor says as much in some browsers, but Safari and the touch browsers never
            // focus a pressed button, and an anchor that is not focusable is never focused at all. Nothing is
            // prevented: the press goes on to do whatever it was aimed at.
            document.addEventListener('pointerdown', e => {
                const target = e.target as Node | null;

                for (const root of shown('data-bit-ttp-clk')) {
                    if (target && root.contains(target)) continue;

                    Utils._tooltipsByRoot.get(root)!.invokeMethodAsync('OnOutsidePress')
                         .catch(err => console.error("BitBlazorUI.Utils.setupTooltip:", err));
                }
            }, { signal: controller.signal, capture: true });
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

        // True when the given element holds the focus. focus() does not throw for an element that refuses
        // it (inert, hidden by a collapsed container, disabled), it only leaves the focus where it was, so
        // a caller acting on the move having happened asks here. An element inside a shadow root is the
        // active element of that root, not of the document.
        public static isActiveElement(element: HTMLElement) {
            try {
                if (!element) return false;

                const root = element.getRootNode() as Document | ShadowRoot;
                return (root.activeElement ?? document.activeElement) === element;
            } catch (e) {
                console.error("BitBlazorUI.Utils.isActiveElement:", e);
                return false;
            }
        }

        // True when the reader has asked for less motion ('prefers-reduced-motion: reduce') and the given
        // element has not been opted back into it: an element inside a subtree marked with bit-fam (which is
        // what BitComponentBase.ForceAnimation renders) keeps its motion, the same way the stylesheets
        // restore the motion tokens there.
        public static prefersReducedMotion(element: HTMLElement) {
            try {
                if (typeof window.matchMedia !== "function") return false;
                if (!window.matchMedia("(prefers-reduced-motion: reduce)").matches) return false;

                return !(element && element.closest && element.closest(".bit-fam"));
            } catch (e) {
                console.error("BitBlazorUI.Utils.prefersReducedMotion:", e);
                return false;
            }
        }

        // True when the target of a key sits on something inside the given container that consumes the
        // arrow keys (and Home/End) on its own: an editable field moves its caret with them, and a slider,
        // a list, a radio group or a grid moves its own selection. The container itself does not count,
        // and neither does an element outside of it.
        private static isKeyConsumer(container: HTMLElement, target: HTMLElement) {
            if (target === container || !container.contains(target)) return false;

            if (target.isContentEditable) return true;

            const tag = target.tagName;
            if (tag === 'TEXTAREA' || tag === 'SELECT') return true;
            if (tag === 'INPUT') {
                const type = ((target as HTMLInputElement).type || '').toLowerCase();
                if (['button', 'submit', 'reset', 'checkbox', 'image', 'file', 'color'].indexOf(type) < 0) return true;
            }

            const owner = target.closest('[role="slider"],[role="spinbutton"],[role="textbox"],[role="searchbox"],[role="combobox"],' +
                                         '[role="listbox"],[role="menu"],[role="menubar"],[role="tablist"],[role="radiogroup"],' +
                                         '[role="grid"],[role="treegrid"],[role="tree"],[role="scrollbar"]');

            return owner != null && owner !== container && container.contains(owner);
        }

        // Registers a keydown listener on an element that navigates with the given keys itself (a carousel,
        // for one), which takes each of those keys away from the browser and hands it to the
        // OnNavigationKey method of the .NET object. Both halves are decided here, at once and from the
        // target of the event, so the element never moves without its default being suppressed or the
        // other way around, and a key costs a call to .NET only when it is actually one to act on.
        // A key is left alone when it carries a modifier (a browser shortcut), when a control inside the
        // element consumes it (isKeyConsumer), and when something deeper already took it (a nested
        // element of the same kind, whose listener runs first).
        // With the key, .NET is told where the focus was: the data-bit-key-origin value (and the id) of the
        // outermost element carrying one between the target and the element, which is how the element
        // tells its own controls apart without a focus event per control. Calling it again updates the
        // keys and the .NET object in place, and an empty key list turns it off, so no separate
        // unregister call is needed - the listener is garbage-collected with the element itself.
        public static registerNavigationKeys(element: HTMLElement, keys: string[], dotnetObj: DotNetObject) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitNavigationKeys = keys || [];
                el.__bitNavigationKeysDotnetObj = dotnetObj;

                if (el.__bitNavigationKeysRegistered) return;
                el.__bitNavigationKeysRegistered = true;

                element.addEventListener('keydown', (e: KeyboardEvent) => {
                    const el = element as any;
                    const currentKeys = el.__bitNavigationKeys as string[];

                    if (!currentKeys || currentKeys.indexOf(e.key) < 0) return;
                    if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;
                    if (e.defaultPrevented) return;

                    const target = e.target instanceof HTMLElement ? e.target : null;

                    if (target && Utils.isKeyConsumer(element, target)) return;

                    e.preventDefault();

                    let origin: HTMLElement | null = null;
                    for (let n = target; n && n !== element; n = n.parentElement) {
                        if (n.hasAttribute('data-bit-key-origin')) origin = n;
                    }

                    const dotnet = el.__bitNavigationKeysDotnetObj as DotNetObject | undefined;

                    dotnet?.invokeMethodAsync('OnNavigationKey', e.key,
                                              origin?.getAttribute('data-bit-key-origin') ?? null,
                                              origin?.id || null)
                          .catch(err => console.error("BitBlazorUI.Utils.registerNavigationKeys:", err));
                });
            } catch (e) { console.error("BitBlazorUI.Utils.registerNavigationKeys:", e); }
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
        // `anchorId` names an element around the container that a surface makes programmatically focusable
        // (tabindex="-1") for the sake of the press on its overlay: pressing something that cannot hold the
        // focus moves it to the nearest element that can, and that press is left its default action - it is
        // what blurs the input the user was typing into, and an input only commits what was typed once it
        // loses the focus, which the dismissal the click runs is what most needs to see. The anchor only
        // catches that focus: it is passed straight on into the container, where the dialog role and the
        // accessible name are - a screen reader confined to the dialog by aria-modal would otherwise hear the
        // focus leave it - and where this trap and the surface's Escape handler are.
        public static setupFocusTrap(elementId: string, anchorId?: string | null) {
            Utils.disposeFocusTrap(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const signal = controller.signal;

            element.addEventListener('keydown', e => {
                if (e.key !== 'Tab') return;

                // A trap registered on something nested inside this one - a dialog opened from inside this
                // dialog - owns the key first, and the event carries on bubbling up to here afterwards.
                // Without this the outer trap would wrap the focus a second time, over the decision the
                // inner one has already made, and land it somewhere neither of them meant.
                if (Utils.hasNearerFocusTrap(element, e.target as Element | null)) return;

                Utils.wrapFocus(element, e);
            }, { signal });

            const anchor = anchorId ? document.getElementById(anchorId) : null;
            if (anchor && anchor !== element) {
                anchor.addEventListener('focusin', e => {
                    // The focus landing on something inside the anchor - the container itself included, once
                    // it is passed on below - is not the anchor being focused.
                    if (e.target !== anchor) return;

                    element.focus({ preventScroll: true });
                }, { signal });
            }

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

        private static _surfaceEscapes = new Map<string, AbortController>();

        // Answers an Escape pressed inside a surface (a dialog) through the OnEscape callback - but only when the
        // key is the surface's own. Four things own it first: an IME composition, which Escape cancels; a
        // control that has taken it (defaultPrevented); a component inside the surface whose own popup is open -
        // a combo box, a search box's suggestions, a date picker - which closes that popup on the key and has its
        // keydown bubble on up through the surface; and a surface nested inside this one (a dialog opened from
        // inside it), which has answered the key before it got here.
        // The popups are read as the key is pressed: this listener is on the element, so it runs before Blazor's
        // document-level delegation lets the component close its popup, while the stack of open callouts is still
        // the one the key was pressed against. Whether a control took the key is read once the whole dispatch is
        // over instead, since a Blazor handler's @onkeydown:preventDefault is applied by that same delegation,
        // after this listener has run. The decision is made here, in the browser, so the surface is only called
        // when it is to act - there is no round trip to ask whether it should.
        // Registering again on the same element replaces the previous registration.
        public static setupSurfaceEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeSurfaceEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const signal = controller.signal;

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                // The nearest surface owns the key whatever it goes on to do with it, so an outer one never
                // answers an Escape an inner one has already seen.
                const event = e as KeyboardEvent & { __bitSurfaceEscape?: boolean };
                if (event.__bitSurfaceEscape) return;
                event.__bitSurfaceEscape = true;

                if (e.isComposing || e.keyCode === 229 || e.defaultPrevented) return;

                if (Callouts.componentContains(e.target as Node | null, element)) return;

                setTimeout(() => {
                    if (e.defaultPrevented || signal.aborted) return;

                    dotnetObj.invokeMethodAsync('OnEscape');
                });
            }, { signal });

            Utils._surfaceEscapes.set(elementId, controller);
        }

        public static disposeSurfaceEscape(elementId: string) {
            const controller = Utils._surfaceEscapes.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._surfaceEscapes.delete(elementId);
        }

        private static _tabOuts = new Map<string, AbortController>();

        // Hands the keyboard back to the page around the trigger of a popup that does not trap it. The popup is
        // relocated to the end of the body while it is open, so the browser's own tab order runs from its last
        // element off the end of the page, and from its first one backwards into whatever ends the page - neither
        // anywhere near the trigger the user opened it from, and the popup is left open behind the keyboard. The
        // content is made to read as if it sat right after the trigger instead: Tab on the trigger goes into it,
        // Tab from its last element moves on to what follows the trigger in the page and reports it through the
        // OnTabOut callback so the popup closes, and Shift+Tab from its first element goes back to the trigger,
        // leaving the popup open for the Tab that brings the user back in.
        public static setupTabOut(elementId: string, triggerId: string, dotnetObj: DotNetObject) {
            Utils.disposeTabOut(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            const isPlainTab = (e: KeyboardEvent) => e.key === 'Tab' && !e.defaultPrevented && !e.altKey && !e.ctrlKey && !e.metaKey;

            const getFocusables = () => Array.from(element.querySelectorAll<HTMLElement>(Utils._focusables)).filter(Utils.isFocusable);

            element.addEventListener('keydown', e => {
                if (!isPlainTab(e)) return;

                // A trap registered on something nested inside the popup owns the key.
                if (Utils.hasNearerFocusTrap(element, e.target as Element | null)) return;

                const trigger = document.getElementById(triggerId);
                if (!trigger) return;

                const focusables = getFocusables();
                const active = document.activeElement;

                if (e.shiftKey) {
                    // The popup itself holding the focus is where it is parked when it opens, which is ahead of
                    // everything it holds.
                    if (active !== element && active !== focusables[0]) return;

                    e.preventDefault();
                    Utils.focusTrigger(trigger);
                    return;
                }

                // From the popup itself, a Tab still has its content to go into first.
                const onLastEdge = focusables.length === 0
                    ? active === element
                    : active === focusables[focusables.length - 1];

                if (!onLastEdge) return;

                e.preventDefault();

                const next = Utils.findFocusableAfter(trigger, element);

                if (next) {
                    next.focus();
                } else {
                    Utils.focusTrigger(trigger);
                }

                dotnetObj.invokeMethodAsync('OnTabOut');
            }, { signal: controller.signal });

            // The trigger's half of the same order: a Tab on it goes into the content rather than past it. The
            // trigger is looked up once, since it stays where it is for as long as the popup is open, and the
            // registration goes with the popup's.
            // A trigger can be a container around more than one control - the anchor a callout renders around
            // the consumer's own markup - and the content sits after the container as a whole, so only the Tab
            // that would leave it is taken: the one from its last control. A Tab from any other moves on to the
            // next control inside it, as it would without the popup open. A trigger with no controls inside it
            // is a control itself, and every Tab on it is the one that leaves it.
            document.getElementById(triggerId)?.addEventListener('keydown', e => {
                if (!isPlainTab(e) || e.shiftKey) return;

                const trigger = e.currentTarget as HTMLElement;
                const inner = Array.from(trigger.querySelectorAll<HTMLElement>(Utils._focusables)).filter(Utils.isFocusable);
                if (inner.length > 0 && e.target !== inner[inner.length - 1]) return;

                e.preventDefault();

                (getFocusables()[0] ?? element).focus();
            }, { signal: controller.signal });

            Utils._tabOuts.set(elementId, controller);
        }

        private static _escapes = new Map<string, AbortController>();

        // Dismisses an open callout on Escape through the OnEscape callback - but only when it is the innermost
        // open one. A dropdown or a menu opened from inside the callout closes its own popup on the same key,
        // and the keydown goes on bubbling from it up through this callout: a handler that only looked at the
        // key would close both with one press, taking away the panel the user was still working in. This
        // listener is on the element, so it runs before Blazor's document-level delegation lets the nested
        // component close anything, which is what makes the stack of open callouts a reliable answer here.
        // It is registered once for the life of the component and ignores the key while the callout is closed.
        // `triggerId` names the element that opens the callout: an Escape pressed anywhere in the page OUTSIDE
        // both of them then dismisses the callout too, as long as it is the innermost open one. A callout opened
        // by hovering is shown while the focus is wherever the user left it, and content that appears on hover
        // has to be dismissible without moving the pointer or the focus (WCAG 1.4.13); the trigger itself is
        // left out, since it answers the key on its own.
        public static setupEscape(elementId: string, dotnetObj: DotNetObject, triggerId?: string | null) {
            Utils.disposeEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape' || e.defaultPrevented) return;

                if (Callouts.current.calloutId !== elementId) return;

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            if (triggerId) {
                Utils._escapesFromAnywhere.add(elementId);

                // In the capture phase, for the same reason the listener above is on the element: the stack of
                // open callouts is read before Blazor's document-level delegation lets a popup the key belongs to
                // (a dropdown list relocated to the body, holding the focus in its search box) close itself.
                document.addEventListener('keydown', e => {
                    if (e.key !== 'Escape') return;

                    if (Callouts.current.calloutId !== elementId) return;

                    const target = e.target as Node | null;
                    if (target && (element.contains(target) || document.getElementById(triggerId)?.contains(target))) return;

                    dotnetObj.invokeMethodAsync('OnEscape');
                }, { signal: controller.signal, capture: true });
            }

            Utils._escapes.set(elementId, controller);
        }

        public static disposeEscape(elementId: string) {
            Utils._escapesFromAnywhere.delete(elementId);

            const controller = Utils._escapes.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapes.delete(elementId);
        }

        // The callouts that answer an Escape pressed anywhere in the page (setupEscape with a trigger), not only
        // one pressed inside them or on their trigger.
        private static _escapesFromAnywhere = new Set<string>();

        private static _escapeGuards = new Map<string, AbortController>();

        // The Escapes a guarded surface nested inside another one has already spoken for, so the surfaces it
        // sits in leave them alone.
        private static _claimedEscapes = new WeakSet<Event>();

        // Tells a surface that dismisses itself on Escape through a Blazor keydown handler, through its
        // OnEscapeVerdict callback, whether the Escape it is about to hear belongs to something inside it
        // instead:
        // - an open popup the key closes (a dropdown whose list is open while the focus is still on its field),
        //   since the keydown goes on bubbling from it up through the surface and one press would take away
        //   the panel the user was filling in along with the list they meant to close;
        // - a handler that already prevented the key's default;
        // - an IME composition the key cancels;
        // - a guarded surface nested inside this one, which answers the key itself (or refuses it).
        // The answer has to be taken while the key is still going down: this listener is on the element, so it
        // runs before Blazor's document-level delegation lets the nested component close its popup. It is sent
        // for every Escape rather than read back later, so each keydown carries its own answer: the callback is
        // queued ahead of the keydown Blazor dispatches right after it, and one Escape can never be answered
        // with what was recorded for the next - however slow the connection.
        public static setupEscapeGuard(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeEscapeGuard(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                const foreign = Utils._claimedEscapes.has(e)
                    || e.defaultPrevented
                    || e.isComposing || e.keyCode === 229 // the engines that predate isComposing report 229
                    || Utils.isEscapeOfOpenCallout(e.target);

                // A closed surface is inert, so an Escape reaches it only while it is open - and from then on the
                // key is this surface's to answer or to refuse, never the one's it was opened from.
                if (element.hasAttribute('inert') === false) {
                    Utils._claimedEscapes.add(e);
                }

                dotnetObj.invokeMethodAsync('OnEscapeVerdict', foreign);
            }, { signal: controller.signal });

            Utils._escapeGuards.set(elementId, controller);
        }

        public static disposeEscapeGuard(elementId: string) {
            const controller = Utils._escapeGuards.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapeGuards.delete(elementId);
        }

        // Whether the innermost open callout is the one the key closes: it was pressed inside the callout or on
        // the component that opened it, or the callout answers an Escape from anywhere (one opened by hovering).
        // A callout that is only left open - one the focus has moved away from - does not close on an Escape
        // pressed somewhere else, so it does not take that Escape away from the surface either.
        private static isEscapeOfOpenCallout(target: EventTarget | null) {
            const current = Callouts.current;
            if (!current.calloutId) return false;

            if (Utils._escapesFromAnywhere.has(current.calloutId)) return true;

            if (!(target instanceof Node)) return false;

            const callout = document.getElementById(current.calloutId);
            if (callout?.contains(target)) return true;

            const trigger = current.componentId ? document.getElementById(current.componentId) : null;

            return !!trigger?.contains(target);
        }

        private static _escapeWatches = new Map<string, AbortController>();

        // Dismisses a surface on Escape through the OnEscape callback (a modal, whose own Blazor handler only
        // reports the key) - but only for a press nothing inside it had the better claim to: a dropdown or a
        // menu opened from inside the surface closes its own popup on the same key, an input method editor
        // cancels the candidate it is composing, and a control that answered the key says so by preventing its
        // default. One press then closes the innermost layer only, rather than that layer and the surface the
        // user is still working in.
        // The two halves of the decision are true at different times, so two listeners take it. The stack of
        // open callouts is read in the capture phase on the element, ahead of every listener inside it and of
        // Blazor's document-level delegation that lets the nested component close its popup - after which the
        // stack would no longer say there was one. Whether the default was prevented is read on the window,
        // once the event has bubbled past that delegation: a Blazor handler's @onkeydown:preventDefault is only
        // on the event from there on. Taking the decision at the time of the event leaves nothing for .NET to
        // ask about later, so two quick presses cannot overwrite each other's answer, and .NET is only called
        // for a press that is the surface's.
        // A surface nested inside another one - a modal opened from inside a modal, rendered inside its
        // content - takes the presses made inside it, and the outer one leaves them alone.
        public static watchEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.unwatchEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const claimed = new WeakSet<Event>();

            (element as any).__bitEscapeRoot = true;

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (e.isComposing || Callouts.isOpenedFrom(element)) {
                    claimed.add(e);
                }
            }, { signal: controller.signal, capture: true });

            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                const target = e.target as Node | null;
                if (!target || !element.contains(target)) return;

                if (Utils.nearestEscapeRoot(target) !== element) return;

                if (claimed.has(e) || e.defaultPrevented) return;

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            Utils._escapeWatches.set(elementId, controller);
        }

        public static unwatchEscape(elementId: string) {
            const controller = Utils._escapeWatches.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapeWatches.delete(elementId);

            const element = document.getElementById(elementId) as any;
            if (element) {
                delete element.__bitEscapeRoot;
            }
        }

        private static _layerEscapes: { elementId: string, controller: AbortController }[] = [];

        // Dismisses a layer that covers the page (an overlay) on Escape through the OnEscape callback - the
        // keyboard's way of doing what a click on the layer does. Unlike a dialog, such a layer does not take the
        // focus when it opens, so the key is listened for on the window rather than on the element: the focus is
        // most often still on the control that opened it, behind the layer.
        // A press is the layer's only when nothing had the better claim to it: an input method composing, an open
        // callout anywhere in the page (a dropdown or a menu closes its own popup on the same key), a control that
        // answered the key by preventing its default, or another surface the focus is inside of - a dialog or a
        // panel opened over the layer, or a modal opened from inside its content. With the focus outside of every surface, only the
        // layer watched last - the one opened last, so the topmost - answers, and one press closes one layer. The
        // same goes for the focus inside a layer covering the page, among the layers covering the page: one opened
        // over it has the say, not the layer the focus happens to be in. A
        // layer that refuses the key (a blocking one) is watched all the same and says no in .NET, so that the
        // press does not fall through to a layer underneath it.
        public static watchLayerEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.unwatchLayerEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const claimed = new WeakSet<Event>();

            (element as any).__bitEscapeRoot = true;

            // Read in the capture phase, ahead of the listeners that let an open callout close itself - after which
            // the stack of open callouts would no longer say there was one.
            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (e.isComposing || Callouts.current.calloutId) {
                    claimed.add(e);
                }
            }, { signal: controller.signal, capture: true });

            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (claimed.has(e) || e.defaultPrevented) return;

                // A dialog surface of the library already answered the key (Utils.setupSurfaceEscape).
                if ((e as any).__bitSurfaceEscape) return;

                const target = e.target as Node | null;

                // A dialog the focus is inside of owns the key, unless it is part of what this layer hosts - a
                // surface of the consumer's own the layer is the backdrop of - which the key closes along with it.
                const dialog = target instanceof Element ? target.closest('[role="dialog"],[role="alertdialog"],dialog') : null;
                if (dialog && !element.contains(dialog)) return;

                const layers = Utils._layerEscapes;
                const root = target ? Utils.nearestEscapeRoot(target) : null;
                const rootCoversPage = root instanceof Element
                                    && layers.some(l => l.elementId === root.id)
                                    && Utils.coversPage(root.id);

                if (root && rootCoversPage === false) {
                    // A surface, or a layer scoped to an element, answers the presses made inside it.
                    if (root !== element) return;
                } else {
                    // A layer covering the page is covered in turn by one opened over it, whatever the focus is
                    // inside of: the topmost of those answers the press - or refuses it (a blocking one) - rather
                    // than the layer underneath it.
                    const candidates = root ? layers.filter(l => Utils.coversPage(l.elementId)) : layers;
                    if (candidates[candidates.length - 1]?.elementId !== elementId) return;
                }

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            Utils._layerEscapes.push({ elementId, controller });
        }

        public static unwatchLayerEscape(elementId: string) {
            const index = Utils._layerEscapes.findIndex(l => l.elementId === elementId);
            if (index < 0) return;

            Utils._layerEscapes[index].controller.abort();
            Utils._layerEscapes.splice(index, 1);

            const element = document.getElementById(elementId) as any;
            if (element) {
                delete element.__bitEscapeRoot;
            }
        }

        // A layer fixed to the screen, rather than one placed over the element it was declared inside of.
        private static coversPage(elementId: string) {
            const element = document.getElementById(elementId);
            return !!element && getComputedStyle(element).position === 'fixed';
        }

        private static nearestEscapeRoot(node: Node): Node | null {
            let current: Node | null = node;

            while (current && !(current as any).__bitEscapeRoot) {
                current = current.parentNode;
            }

            return current;
        }

        // Resolves once the exit animation of a surface has played out, so that it is only taken out of the page
        // after it: the animations running on the element and on its direct children (an overlay, a content
        // box), which is where a surface's own movement is. Anything deeper is the content's own business - a
        // spinner inside it runs forever - and so is anything that repeats. `timeout` bounds the wait, so a
        // surface is never kept in the page by an animation that does not end.
        public static async waitForAnimations(elementId: string, timeout: number = 1000) {
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

        // The trigger may be a plain container around the control the user actually lands on - the anchor a callout
        // renders around the consumer's own button - which takes no focus of its own, so the first focusable element
        // inside it is where the focus goes back to.
        private static focusTrigger(trigger: HTMLElement) {
            const target = trigger.matches(Utils._focusables) && Utils.isFocusable(trigger)
                ? trigger
                : Array.from(trigger.querySelectorAll<HTMLElement>(Utils._focusables)).find(Utils.isFocusable);

            (target ?? trigger).focus();
        }

        public static disposeTabOut(elementId: string) {
            const controller = Utils._tabOuts.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._tabOuts.delete(elementId);
        }

        // The first element of the tab order that follows the given one in the document, leaving out the element
        // itself, what it contains, and the popup being tabbed out of. The tab order is the one the anchor is in:
        // an anchor inside a dialog that keeps the keyboard in itself (a modal, a registered focus trap) looks no
        // further than that dialog, and wraps around to its first element past its last one, as the dialog's own
        // trap would - the page behind it is out of reach of the keyboard, however it is arranged in the document.
        private static findFocusableAfter(anchor: HTMLElement, exclude: HTMLElement) {
            const scope = Utils.findTabScope(anchor);

            const candidates = Array.from((scope ?? document).querySelectorAll<HTMLElement>(Utils._focusables)).filter(el =>
                !anchor.contains(el)
                && !exclude.contains(el)
                && Utils.isFocusable(el));

            const next = candidates.find(el => (anchor.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0);

            return next ?? (scope ? candidates[0] ?? null : null);
        }

        // The nearest ancestor of the element that keeps the keyboard inside itself, or null when it is in the
        // page's own tab order.
        private static findTabScope(element: HTMLElement): HTMLElement | null {
            for (let node = element.parentElement; node && node !== document.body; node = node.parentElement) {
                if (node.id && Utils._focusTraps.has(node.id)) return node;

                if (node.getAttribute('aria-modal') === 'true') return node;

                if (node.tagName === 'DIALOG' && Utils.isModalDialog(node)) return node;
            }

            return null;
        }

        private static isModalDialog(dialog: HTMLElement) {
            try {
                return dialog.matches(':modal');
            } catch {
                return false;
            }
        }

        private static _focusOrigins = new Map<string, HTMLElement>();

        // Remembers the element the focus was on at the moment a popup took it over, keyed by the popup, so
        // that closing the popup can hand the keyboard back to where it came from. A popup that moves the
        // focus into itself and then takes its content away leaves the focus on the body, which sends the
        // keyboard back to the top of the page - the one thing the WAI-ARIA dialog pattern asks not to happen.
        // Reports whether an origin was remembered, which it is not for a focus that was on the body.
        public static captureFocusOrigin(elementId: string): boolean {
            try {
                const active = document.activeElement as HTMLElement | null;

                // The body is not somewhere the focus can be handed back to, and neither is an element that
                // is inside the popup itself: the focus was already there, so there is nothing to restore.
                if (!active || active === document.body) return false;

                const container = document.getElementById(elementId);
                if (container?.contains(active)) return false;

                Utils._focusOrigins.set(elementId, active);

                return true;
            } catch (e) { console.error("BitBlazorUI.Utils.captureFocusOrigin:", e); return false; }
        }

        // Hands the focus back to the element captureFocusOrigin remembered, and forgets it either way, so a
        // popup that is opened again captures anew. The focus is only ours to hand back while it is still in
        // the popup - or was dropped to the body by the popup being hidden - so a focus the user has since
        // moved somewhere else of their own accord is left alone.
        // Reports whether the focus was taken care of - handed back, or left where the user put it - which it
        // is not when there was no origin to hand it back to, or the origin has left the page: the caller then
        // has the focus to place itself.
        public static restoreFocusOrigin(elementId: string): boolean {
            try {
                const origin = Utils._focusOrigins.get(elementId);
                if (!origin) return false;

                Utils._focusOrigins.delete(elementId);

                // The element that held the focus may have been taken off the page while the popup was open.
                if (!origin.isConnected) return false;

                const active = document.activeElement;
                const container = document.getElementById(elementId);
                const ours = active == null || active === document.body || (container?.contains(active) ?? false);
                if (!ours) return true;

                origin.focus();

                return true;
            } catch (e) { console.error("BitBlazorUI.Utils.restoreFocusOrigin:", e); return false; }
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
        // has since moved somewhere else belongs to whoever moved it. `scopeId` names a popup that is still in
        // the page while it closes - playing its exit animation, inert already - where the browser only moves
        // the focus out at its next focus fixup: a focus still inside it is as lost as one on the body.
        public static restoreFocus(key: string, onlyWhenLost: boolean, scopeId?: string | null) {
            const element = Utils._focusOrigins.get(key);
            Utils._focusOrigins.delete(key);

            if (!element) return;

            try {
                if (onlyWhenLost) {
                    const active = document.activeElement;
                    const scope = scopeId ? document.getElementById(scopeId) : null;
                    if (active && active !== document.body && active !== document.documentElement && !scope?.contains(active)) return;
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
        // which one its items are laid out along. The offset is measured from the start edge the content
        // flows from, so a horizontal RTL container (whose scrollLeft runs from 0 towards the negative)
        // is handed its negation. The smooth scroll is dropped for a reader who has asked for less motion.
        public static scrollTo(element: HTMLElement, offset: number, horizontal: boolean, smooth: boolean) {
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
        public static scrollToEnd(element: HTMLElement, horizontal: boolean, smooth: boolean) {
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
        public static scrollToChild(element: HTMLElement, container: HTMLElement, index: number, extraOffset: number, horizontal: boolean, smooth: boolean) {
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
        // ones of the components sitting inside the element. Calling it again updates the flags in
        // place, so no separate unregister call is needed - the listeners are garbage-collected with
        // the element itself.
        // While it is active, the native drag of a link (or of anything else draggable) inside the
        // element is cancelled too, since it would swallow the pointer events of the drag the element
        // performs itself. And with a positive clickThreshold, the click that ends a drag which
        // travelled further than that is swallowed before anything else sees it, so letting go of a
        // slide that was dragged over a link (or a button) does not also follow it. With a clickAxis
        // ('x' or 'y') only the travel along that axis counts, and a drag that went further across it
        // than along it is not one either, which is how the element itself tells a drag from a scroll.
        public static registerPreventPointerDown(element: HTMLElement, active: boolean, clickThreshold?: number, clickAxis?: string) {
            if (!element) return;

            try {
                const el = element as any;
                el.__bitPreventPointerDown = active;
                el.__bitPreventPointerDownClickThreshold = clickThreshold || 0;
                el.__bitPreventPointerDownClickAxis = clickAxis || null;

                if (el.__bitPreventPointerDownRegistered) return;
                el.__bitPreventPointerDownRegistered = true;

                // Where the pointer went down is recorded in the capture phase, so a control inside the
                // element that stops the pointerdown from bubbling cannot leave a stale position behind
                // for the click to be measured against.
                element.addEventListener('pointerdown', (e: PointerEvent) => {
                    const el = element as any;
                    el.__bitPointerDownX = e.clientX;
                    el.__bitPointerDownY = e.clientY;
                }, true);

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

                element.addEventListener('dragstart', (e: DragEvent) => {
                    if (!(element as any).__bitPreventPointerDown) return;

                    e.preventDefault();
                });

                element.addEventListener('click', (e: MouseEvent) => {
                    const el = element as any;
                    const threshold = el.__bitPreventPointerDownClickThreshold as number;
                    const downX = el.__bitPointerDownX as number | undefined;
                    const downY = el.__bitPointerDownY as number | undefined;

                    // The position belongs to the one click it started, so it is used up here.
                    el.__bitPointerDownX = el.__bitPointerDownY = undefined;

                    if (!el.__bitPreventPointerDown || !(threshold > 0)) return;
                    if (downX === undefined || downY === undefined) return;

                    // A click raised from the keyboard carries no pointer travel of its own, so only a
                    // click that ends a pointer drag longer than the threshold is taken away.
                    if (e.detail === 0) return;

                    const dx = Math.abs(e.clientX - downX);
                    const dy = Math.abs(e.clientY - downY);
                    const axis = el.__bitPreventPointerDownClickAxis as string | null;

                    const along = axis === 'x' ? dx : axis === 'y' ? dy : Math.max(dx, dy);
                    const across = axis === 'x' ? dy : axis === 'y' ? dx : 0;

                    if (along <= threshold || across > along) return;

                    e.preventDefault();
                    e.stopPropagation();
                }, true);
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

        // Makes an element carrying the button role activate from the keyboard the way a real button
        // does: Enter clicks it as the key goes down, Space as the key comes back up, and neither key
        // scrolls the page. Only a key pressed on the element itself counts - one pressed on a control
        // inside it (a button a template brought along) already clicks that control, and the click
        // bubbles up to the element's own handler, so answering the key here too would act twice.
        // The listeners are garbage-collected with the element, so no unregister call is needed.
        public static registerButtonKeys(element: HTMLElement) {
            if (!element) return;

            try {
                const el = element as any;
                if (el.__bitButtonKeysRegistered) return;
                el.__bitButtonKeysRegistered = true;

                const plain = (e: KeyboardEvent) => e.target === element && !e.ctrlKey && !e.altKey && !e.metaKey;

                element.addEventListener('keydown', (e: KeyboardEvent) => {
                    if (!plain(e)) return;

                    if (e.key === 'Enter') {
                        e.preventDefault();
                        element.click();
                    } else if (e.key === ' ') {
                        e.preventDefault();
                        el.__bitButtonKeysSpace = true;
                    }
                });

                element.addEventListener('keyup', (e: KeyboardEvent) => {
                    if (e.key !== ' ' || !el.__bitButtonKeysSpace) return;
                    el.__bitButtonKeysSpace = false;

                    if (!plain(e)) return;

                    e.preventDefault();
                    element.click();
                });

                element.addEventListener('blur', () => el.__bitButtonKeysSpace = false);
            } catch (e) { console.error("BitBlazorUI.Utils.registerButtonKeys:", e); }
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

            const active = document.activeElement;

            if (focusables.length === 0) {
                // Nothing inside the container can take the focus, which leaves the container itself
                // holding it - the components that trap the focus make it programmatically focusable for
                // exactly this case. Tabbing on from there would walk straight out of the trap and into
                // the page behind it, so the key is swallowed instead of being left to the browser.
                if (active === root) {
                    e.preventDefault();
                }
                return;
            }

            const first = focusables[0];
            const last = focusables[focusables.length - 1];

            // The container itself holding the focus - where a surface parks it as it opens, and where the
            // focus a press on its overlay took off an input is passed on to - is inside the trap but on no
            // edge of it, and the browser's own answer to a Tab from there is not bounded by the trap: its
            // sequential order puts positive tabindexes first wherever on the page they are, and a Shift+Tab
            // walks backwards out of the container altogether. Everything the trap holds is inside the
            // container, so the next stop is its first element and the previous one its last.
            if (active === root) {
                (e.shiftKey ? last : first).focus();
                e.preventDefault();
                return;
            }

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
        // includeDisabled keeps the items a component deliberately left focusable (rendered with aria-disabled
        // instead of the native disabled attribute) in the navigation, so they can be reached and announced as
        // unavailable rather than silently skipped. A natively disabled element cannot take the focus at all,
        // so it stays out either way.
        // fromCurrent keeps the focused item itself in the 'char' search rather than starting after it, which is
        // what lets a typeahead string that already matches it be typed further into without jumping away.
        public static focusItem(containerId: string, selector: string, mode: string, char: string | null, includeDisabled?: boolean, fromCurrent?: boolean) {
            const container = document.getElementById(containerId);
            if (!container) return;

            try {
                const items = (Array.from(container.querySelectorAll(selector)) as HTMLElement[])
                    .filter(el => !(el as HTMLButtonElement).disabled &&
                                  (includeDisabled === true || el.getAttribute('aria-disabled') !== 'true'));
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
                    const start = current < 0 ? 0 : (fromCurrent === true ? current : current + 1);
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