namespace BitBlazorUI {
    export namespace Utils {

        export const _focusTraps = new Map<string, AbortController>();

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
        export function setupFocusTrap(elementId: string, anchorId?: string | null) {
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
        export function hasNearerFocusTrap(root: HTMLElement, target: Element | null) {
            let node = target;

            while (node && node !== root) {
                if (node.id && Utils._focusTraps.has(node.id)) return true;

                node = node.parentElement;
            }

            return false;
        }

        export function disposeFocusTrap(elementId: string) {
            const controller = Utils._focusTraps.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._focusTraps.delete(elementId);
        }

        export const _tabOuts = new Map<string, AbortController>();

        // Hands the keyboard back to the page around the trigger of a popup that does not trap it. The popup is
        // relocated to the end of the body while it is open, so the browser's own tab order runs from its last
        // element off the end of the page, and from its first one backwards into whatever ends the page - neither
        // anywhere near the trigger the user opened it from, and the popup is left open behind the keyboard. The
        // content is made to read as if it sat right after the trigger instead: Tab on the trigger goes into it,
        // Tab from its last element moves on to what follows the trigger in the page and reports it through the
        // OnTabOut callback so the popup closes, and Shift+Tab from its first element goes back to the trigger,
        // leaving the popup open for the Tab that brings the user back in. Reports whether it was registered,
        // which it is not for a popup that is not in the page.
        export function setupTabOut(elementId: string, triggerId: string, dotnetObj: DotNetObject) {
            Utils.disposeTabOut(elementId);

            const element = document.getElementById(elementId);
            if (!element) return false;

            const controller = new AbortController();

            const isPlainTab = (e: KeyboardEvent) => e.key === 'Tab' && !e.defaultPrevented && !e.altKey && !e.ctrlKey && !e.metaKey;

            element.addEventListener('keydown', e => {
                if (!isPlainTab(e)) return;

                // A trap registered on something nested inside the popup owns the key.
                if (Utils.hasNearerFocusTrap(element, e.target as Element | null)) return;

                const trigger = document.getElementById(triggerId);
                if (!trigger) return;

                const focusables = Utils.getFocusables(element);
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

                // The component may be disposed while the call is on its way - a navigation the Tab started - and
                // the popup goes with it then, which is all the call was for.
                dotnetObj.invokeMethodAsync('OnTabOut').catch(() => { });
            }, { signal: controller.signal });

            // The trigger's half of the same order: a Tab on it goes into the content rather than past it. The
            // trigger is looked up once, since it stays where it is for as long as the popup is open, and the
            // registration goes with the popup's.
            // A trigger can be a container around more than one control - the anchor a callout renders around
            // the consumer's own markup - and the content sits after the container as a whole, so only the Tab
            // that would leave it is taken: the one from its last control. A Tab from any other moves on to the
            // next control inside it, as it would without the popup open. A trigger with no controls inside it
            // is a control itself, and every Tab on it is the one that leaves it.
            // The Tab is only taken while there is somewhere in the popup for it to go. A popup its component has
            // already closed - the trigger saying so through aria-expanded before the hiding and this listener's
            // release reach the page - or one that is hidden, or that holds nothing the focus can land on and is
            // not focusable itself, would otherwise swallow the key: the focus would be sent to an element that
            // cannot take it, and stay where it is.
            document.getElementById(triggerId)?.addEventListener('keydown', e => {
                if (!isPlainTab(e) || e.shiftKey) return;

                const trigger = e.currentTarget as HTMLElement;
                const inner = Utils.getFocusables(trigger);
                if (inner.length > 0 && e.target !== inner[inner.length - 1]) return;

                // Whether the popup is shown at all: a popup is kept out of the tab sequence itself (tabindex="-1"),
                // which is not what is asked here.
                if (trigger.getAttribute('aria-expanded') === 'false' || !Utils.canHoldFocus(element)) return;

                const target = Utils.firstFocusable(element) ?? (element.hasAttribute('tabindex') ? element : null);
                if (!target) return;

                e.preventDefault();

                target.focus();
            }, { signal: controller.signal });

            Utils._tabOuts.set(elementId, controller);

            return true;
        }

        export function disposeTabOut(elementId: string) {
            const controller = Utils._tabOuts.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._tabOuts.delete(elementId);
        }

        // The trigger may be a plain container around the control the user actually lands on - the anchor a callout
        // renders around the consumer's own button - which takes no focus of its own, so the first focusable element
        // inside it is where the focus goes back to.
        export function focusTrigger(trigger: HTMLElement) {
            const target = Utils.isFocusableElement(trigger)
                ? trigger
                : Utils.firstFocusable(trigger);

            (target ?? trigger).focus();
        }

        // The first element of the tab order that follows the given one in the document, leaving out the element
        // itself, what it contains, and the popup being tabbed out of. The tab order is the one the anchor is in:
        // an anchor inside a dialog that keeps the keyboard in itself (a modal, a registered focus trap) looks no
        // further than that dialog, and wraps around to its first element past its last one, as the dialog's own
        // trap would - the page behind it is out of reach of the keyboard, however it is arranged in the document.
        export function findFocusableAfter(anchor: HTMLElement, exclude: HTMLElement) {
            const scope = Utils.findTabScope(anchor);

            const skip = (el: HTMLElement) => anchor.contains(el) || exclude.contains(el);

            const next = Utils.findFocusable(scope ?? document, anchor, true, skip);

            return next ?? (scope ? Utils.firstFocusable(scope, skip) : null);
        }

        // The nearest ancestor of the element that keeps the keyboard inside itself, or null when it is in the
        // page's own tab order.
        export function findTabScope(element: HTMLElement): HTMLElement | null {
            for (let node = element.parentElement; node && node !== document.body; node = node.parentElement) {
                if (node.id && Utils._focusTraps.has(node.id)) return node;

                if (node.getAttribute('aria-modal') === 'true') return node;

                if (node.tagName === 'DIALOG' && Utils.isModalDialog(node)) return node;
            }

            return null;
        }

        export function isModalDialog(dialog: HTMLElement) {
            try {
                return dialog.matches(':modal');
            } catch {
                return false;
            }
        }

        export const _focusIns = new Map<string, AbortController>();

        // Reports, through the OnFocusIn callback, the first time the focus goes into the element other than by the
        // pointer: the keyboard tabbing into it, or a script focusing something in it. A popup a pointer opened by
        // hovering is the pointer's until then, and a click on it - on a field, or on its own background when it
        // is focusable itself - is still the pointer's, so the focus a press moves in is left out. What moved the
        // focus is the last of the two that went down, a pointer or a key, since a press reaches the focus through
        // more than one event (a mousedown focuses a field, a click on a label focuses the field it names). It is
        // registered once and released by the callback that reports it.
        export function setupFocusIn(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeFocusIn(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();

            let byPointer = false;

            document.addEventListener('pointerdown', () => byPointer = true, { signal: controller.signal, capture: true });
            document.addEventListener('keydown', () => byPointer = false, { signal: controller.signal, capture: true });

            element.addEventListener('focusin', () => {
                if (byPointer) return;

                Utils.disposeFocusIn(elementId);

                dotnetObj.invokeMethodAsync('OnFocusIn');
            }, { signal: controller.signal });

            Utils._focusIns.set(elementId, controller);
        }

        export function disposeFocusIn(elementId: string) {
            const controller = Utils._focusIns.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._focusIns.delete(elementId);
        }
    }
}
