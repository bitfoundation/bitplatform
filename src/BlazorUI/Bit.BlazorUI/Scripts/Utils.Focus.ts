namespace BitBlazorUI {
    export namespace Utils {

        // Everything that can hold the focus inside a container. A roving tabindex takes every item of a grid
        // but one out of the tab sequence, which is why tabindex="-1" is excluded here. The controls a header
        // or footer template brings along are part of the container too, so the whole set of natively
        // focusable elements is listed rather than only the ones the components render themselves - a
        // disclosure's summary and a media player's controls included, which a trap that missed them would
        // let the Tab out of the container from. Only the first summary of a details element is its disclosure
        // control; any other summary is plain content the browser never puts in the tab sequence.
        // This is the library's one definition of "what can hold the focus": every script asks it, through
        // the helpers below, rather than keeping a copy of its own that a fix here would not reach - the
        // scripts of Bit.BlazorUI.Extras included, which call these members through the ambient declaration
        // of Scripts/BitBlazorUI.d.ts (BitFocusablesContractTests keeps the two in step).
        export const focusableSelector =
            'a[href]:not([tabindex="-1"]), button:not([disabled]):not([tabindex="-1"]), ' +
            'input:not([disabled]):not([tabindex="-1"]), select:not([disabled]):not([tabindex="-1"]), ' +
            'textarea:not([disabled]):not([tabindex="-1"]), details > summary:first-of-type:not([tabindex="-1"]), ' +
            'iframe:not([tabindex="-1"]), audio[controls]:not([tabindex="-1"]), video[controls]:not([tabindex="-1"]), ' +
            '[contenteditable]:not([contenteditable="false"]):not([tabindex="-1"]), ' +
            '[tabindex]:not([tabindex="-1"])';

        // Whether an element matching the set above is a place the focus can actually land. It is the one
        // answer both the initial focus and the focus trap ask for, so the element the focus is moved to
        // when a popup opens is the same one Shift+Tab wraps back to from the end of it.
        // An inert subtree refuses the focus while still matching the selector and keeping its box - a
        // surface playing its exit animation, a folded splitter panel - and so does a control disabled by
        // the fieldset around it, which carries no disabled attribute of its own for the selector to see.
        // Inside an editing host only the host itself is a tab stop; the links and controls of the content
        // being edited are text to the browser, and counting one as the last stop of a trap would let the
        // Tab from the host walk out of it.
        // A hidden element has no box at all - which is how a display:none subtree (e.g. a collapsed
        // section) is skipped without measuring every ancestor. A subtree whose content the engine skips -
        // hidden="until-found" (a HiddenUntilFound accordion panel), content-visibility:hidden - can still
        // hand its descendants a box the focus never reaches; checkVisibility sees both, and the box alone is
        // asked for only in the engines without it. visibility:hidden leaves a box the focus still cannot
        // land in, so it is asked about separately, and only for the elements that got past the measurement.
        // A negative tabindex takes an element out of the tab sequence however it is written ("-2", " -1"), which
        // the selector's exact match on "-1" cannot see, so the attribute is parsed the way the browser parses it.
        export function isFocusable(el: HTMLElement) {
            return Utils.isInTabSequence(el) && Utils.canHoldFocus(el);
        }

        // The attribute rather than the tabIndex property, which also reads -1 for an element that carries no
        // tabindex at all (an editing host in some engines), so only a value written on the element counts. An
        // unparsable value is ignored, as it is by the browser.
        export function isInTabSequence(el: HTMLElement) {
            const tabindex = el.getAttribute('tabindex');

            return tabindex === null || (parseInt(tabindex, 10) < 0) === false;
        }

        // Whether the focus can be moved onto the element at all, by the keyboard or by a script.
        export function canHoldFocus(el: HTMLElement) {
            if (el.closest('[inert]') !== null || el.matches(':disabled') || el.parentElement?.isContentEditable === true) return false;

            const rendered = typeof el.checkVisibility === 'function'
                ? el.checkVisibility()
                : el.offsetWidth > 0 || el.offsetHeight > 0 || el.getClientRects().length > 0;

            return rendered && getComputedStyle(el).visibility !== 'hidden';
        }

        // Whether any element - not only one taken from the selector above - is a place the focus can land: the
        // element a caller holds may be a plain container, which isFocusable alone would accept for its box.
        export function isFocusableElement(el: HTMLElement) {
            return el.matches(Utils.focusableSelector) && Utils.isFocusable(el);
        }

        // The elements inside the container the focus can land on, in document order.
        export function getFocusables(container: ParentNode): HTMLElement[] {
            return Array.from(container.querySelectorAll<HTMLElement>(Utils.focusableSelector)).filter(Utils.isFocusable);
        }

        // The first element inside the container the focus can land on, leaving out the ones the skip callback
        // names, and measuring no more of them than it takes.
        export function firstFocusable(container: ParentNode, skip?: (el: HTMLElement) => boolean): HTMLElement | null {
            const all = container.querySelectorAll<HTMLElement>(Utils.focusableSelector);

            for (let i = 0; i < all.length; i++) {
                if (skip?.(all[i]) !== true && Utils.isFocusable(all[i])) return all[i];
            }

            return null;
        }

        // The nearest element inside the scope the focus can land on that follows (or, backwards, precedes) the
        // given node in the document, leaving out the ones the skip callback names - which is where a node's own
        // descendants are left out, as they follow it too. Only the elements on the far side of the node are
        // measured, and no more of them than it takes, which matters when the scope is the whole document.
        export function findFocusable(scope: ParentNode, from: Node, forward: boolean, skip?: (el: HTMLElement) => boolean): HTMLElement | null {
            const all = scope.querySelectorAll<HTMLElement>(Utils.focusableSelector);
            const side = forward ? Node.DOCUMENT_POSITION_FOLLOWING : Node.DOCUMENT_POSITION_PRECEDING;
            const step = forward ? 1 : -1;

            for (let i = forward ? 0 : all.length - 1; i >= 0 && i < all.length; i += step) {
                const el = all[i];

                if ((from.compareDocumentPosition(el) & side) === 0) continue;

                if (skip?.(el) !== true && Utils.isFocusable(el)) return el;
            }

            return null;
        }

        // Keeps Tab and Shift+Tab cycling inside a container, which is what a popup that reports itself a modal
        // dialog has to do: the tab order runs on into the page behind it otherwise, leaving the focus somewhere
        // an overlay swallows every click that could bring it back. The keydown is left alone where the focus is
        // not on the edge of the container, so tabbing within it moves as it normally would.
        export function wrapFocus(root: HTMLElement, e: KeyboardEvent) {
            if (!root) return;

            // A hidden element is not a place the focus can land, and a callout carries parts that are only
            // rendered for some of its states.
            const focusables = Utils.getFocusables(root);

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
                Utils.focusFromKey(e.shiftKey ? last : first);
                e.preventDefault();
                return;
            }

            if (e.shiftKey && active === first) {
                Utils.focusFromKey(last);
                e.preventDefault();
            } else if (!e.shiftKey && active === last) {
                Utils.focusFromKey(first);
                e.preventDefault();
            }
        }

        // Focuses an element from the handler of the key that asks for it. Firefox does not move the focus into a frame
        // from in there - a frame on the edge of a trap, an HTML preview in a dialog, kept the focus where it was - so a
        // frame that did not take it is focused once the key has been handled, unless the focus has moved on by then.
        export function focusFromKey(el: HTMLElement) {
            const from = document.activeElement;

            el.focus();

            if (el instanceof HTMLIFrameElement === false || document.activeElement === el) return;

            setTimeout(() => {
                if (document.activeElement === from) {
                    el.focus();
                }
            });
        }

        // Moves the focus to the first focusable element inside the given container, falling back to the
        // container itself (which the caller makes programmatically focusable with tabindex="-1") when it
        // holds nothing focusable, so the focus never stays behind on the element that opened the popup.
        // The fallback is where the caller would rather start than the first focusable element, but only by
        // default - a Dialog button an app names for every Dialog at once, which the content of this one has
        // not seen - so the selector and an element the content marks for the focus both win over it.
        export function focusFirstElement(elementId: string, selector?: string | null, fallback?: HTMLElement | null) {
            const container = document.getElementById(elementId);
            if (!container) return;

            // A caller-supplied selector says where the focus belongs when the first focusable element is
            // not it. It is tried on its own so a selector that is invalid, or that matches nothing
            // visible, falls through to the default rather than leaving the focus behind on the page. An
            // element it names is focused even when a negative tabindex keeps it out of the tab sequence -
            // a heading made programmatically focusable is a place the consumer asked for by name.
            if (selector) {
                try {
                    const preferred = Array.from(container.querySelectorAll<HTMLElement>(selector)).find(Utils.canHoldFocus);
                    if (preferred) {
                        preferred.focus();
                        return;
                    }
                } catch (e) { console.error("BitBlazorUI.Utils.focusFirstElement:", e); }
            }

            try {
                // The same set the focus trap cycles through, so the element the focus lands on when the
                // popup opens is the same one Shift+Tab wraps back to from the end of it.
                // The consumer naming the element the focus should land on, for the popups whose first
                // focusable element is not the one worth starting at - a dismiss button ahead of the field
                // the popup was opened to fill in. The first focusable element is the fallback.
                // The standard autofocus attribute says the same thing and is what a native dialog reads,
                // so it is honoured alongside the data- one: the browser only ever acts on it for markup
                // that was in the document when it was parsed, which a popup's content never is.
                const requested = Utils.firstFocusable(container, el =>
                    el.hasAttribute('data-autofocus') === false && el.hasAttribute('autofocus') === false);

                // A fallback that has left the container or cannot take the focus right now is passed over like
                // a selector that matches nothing, so the focus still lands inside the container.
                const preferred = fallback && container.contains(fallback) && Utils.canHoldFocus(fallback) ? fallback : null;

                (requested ?? preferred ?? Utils.firstFocusable(container) ?? container).focus();
            } catch (e) { console.error("BitBlazorUI.Utils.focusFirstElement:", e); }
        }

        // Moves the focus to the trigger inside the given container - its first focusable element - unless the
        // focus is somewhere inside the container already. A click focuses the button it lands on in most engines
        // but not in WebKit, which leaves the focus where it was, and a popup whose trigger never took the focus
        // is a popup without its Escape key wherever that key is listened for on the trigger. Where the engine
        // has already focused what was clicked, nothing moves; the container itself is never focused, as a
        // container with nothing focusable in it is not a trigger the keyboard could have reached either.
        export function focusClickedTrigger(containerId: string) {
            try {
                const container = document.getElementById(containerId);
                if (!container || container.contains(document.activeElement)) return;

                const trigger = Utils.firstFocusable(container);

                trigger?.focus({ preventScroll: true });
            } catch (e) { console.error("BitBlazorUI.Utils.focusClickedTrigger:", e); }
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
        export function focusItem(containerId: string, selector: string, mode: string, char: string | null, includeDisabled?: boolean, fromCurrent?: boolean) {
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

        // The focusable elements that are content rather than a control: a disclosure's summary and an embedded
        // frame or media player take the focus, but are never the control a popup or a tooltip belongs to, and
        // one written ahead of that control would otherwise take the attributes meant for it.
        export const _notAControl = 'summary, iframe, audio, video';

        // The first control inside the container a relationship can be mirrored onto, leaving out the ones the
        // skip callback names. Nothing is measured: the relationship is written whether or not the control is
        // on screen right now, as nothing writes it again the moment it is.
        export function findControl(container: HTMLElement, skip?: (el: HTMLElement) => boolean): HTMLElement | null {
            const all = container.querySelectorAll<HTMLElement>(Utils.focusableSelector);

            for (let i = 0; i < all.length; i++) {
                if (all[i].matches(Utils._notAControl) === false && skip?.(all[i]) !== true) return all[i];
            }

            return null;
        }

        // The pointers of the gesture under way, and the element the focus landed on while one was (see hasKeyboardFocus).
        const gesturePointers = new Set<number>();
        let pointerGesture = false;
        let pointerGestureEnd = 0;
        let pointerFocused: EventTarget | null = null;
        let watchingFocusOrigin = false;

        // How long after its last pointer is lifted a gesture can still move the focus when no click comes to end it
        // sooner: a tap's focus lands on the mouse events that follow the lift, a beat after it.
        const pointerGestureTail = 1000;

        // Starts telling a focus a pointer press put on an element apart from one the keyboard put there (see
        // hasKeyboardFocus). Every script that asks calls it as it is set up rather than as the page loads; only the
        // first call adds the listeners, which stay for the life of the page.
        // A focus counts as the pointer's when it lands while a pointer gesture is under way: from the press until the
        // click it ends with has run its own default actions (a label's click focuses its control) - or, where no click
        // comes, a moment after the last pointer was lifted, and at once where the browser took the gesture over (a
        // cancel). A focus moved later - by a script, or by assistive technology, which presses no key - is no
        // pointer's. The control the gesture focused stays the pointer's until the focus moves on, or until a key: one
        // that only modifies others (or a shortcut made with one) is no use of the keyboard, any more than it is to
        // :focus-visible; any other key pressed on a control a pointer focused is the keyboard taking it over.
        export function watchFocusOrigin() {
            if (watchingFocusOrigin) return;

            watchingFocusOrigin = true;

            const endGesture = () => {
                clearTimeout(pointerGestureEnd);
                pointerGestureEnd = 0;
                pointerGesture = false;
                gesturePointers.clear();
            };

            // Ended after the tasks already queued have run, so a focus the gesture still leads to is recorded first.
            const endGestureAfter = (delay: number) => {
                clearTimeout(pointerGestureEnd);
                pointerGestureEnd = setTimeout(endGesture, delay);
            };

            document.addEventListener('pointerdown', e => {
                // The primary pointer going down starts a gesture of its own, whatever an earlier one left behind.
                if (e.isPrimary) gesturePointers.clear();

                clearTimeout(pointerGestureEnd);
                pointerGestureEnd = 0;
                gesturePointers.add(e.pointerId);
                pointerGesture = true;
            }, { capture: true, passive: true });

            const lift = (e: PointerEvent, delay: number) => {
                gesturePointers.delete(e.pointerId);
                if (gesturePointers.size === 0 && pointerGesture) endGestureAfter(delay);
            };

            document.addEventListener('pointerup', e => lift(e, pointerGestureTail), { capture: true, passive: true });
            document.addEventListener('pointercancel', e => lift(e, 0), { capture: true, passive: true });

            // The click a gesture ends with is the last of what it can focus, once its default actions have run.
            document.addEventListener('click', () => {
                if (pointerGesture && gesturePointers.size === 0) endGestureAfter(0);
            }, { capture: true, passive: true });

            document.addEventListener('keydown', e => {
                if (e.ctrlKey || e.metaKey || e.altKey || /^(Control|Shift|Alt|AltGraph|Meta|OS|CapsLock)$/.test(e.key)) return;

                endGesture();
                pointerFocused = null;
            }, { capture: true, passive: true });

            document.addEventListener('focusin', e => { pointerFocused = pointerGesture ? e.target : null; }, { capture: true, passive: true });
        }

        // Whether the focus is inside the container and the keyboard is what put it there - a control a scroll must
        // not roll out of the view, a bar to keep on the screen. :focus-visible says so of most elements, but the
        // engines match it on a text field however it was focused (the field shows its caret to a mouse user too), so
        // a field a pointer press focused is told apart through watchFocusOrigin, which the caller has started. A
        // frame cannot say either way, its focus being in a document of its own, and answers `frame`: what the caller
        // knows of the focus that moved into it. An engine that does not know :focus-visible throws on it rather than
        // answering false, and every focus then counts: a control hidden under an edge is the worse outcome.
        export function hasKeyboardFocus(container: Element, frame = false): boolean {
            const active = document.activeElement;

            if (!active || !container.contains(active)) return false;

            if (/^(IFRAME|FRAME|OBJECT|EMBED)$/.test(active.tagName)) return frame;

            if (active === pointerFocused) return false;

            try {
                return active.matches(':focus-visible');
            } catch {
                return true;
            }
        }
    }
}
