namespace BitBlazorUI {
    export namespace Utils {

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
        export function syncAriaPopup(anchorId: string, popupId: string, isOpen: boolean, hasPopup: string): boolean {
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

        export function disposeAriaPopup(anchorId: string) {
            const state = Utils._ariaPopups.get(anchorId);
            if (!state) return;

            state.observer?.disconnect();
            Utils._ariaPopups.delete(anchorId);
        }

        export const _ariaPopups = new Map<string, {
            anchor: HTMLElement,
            popupId: string,
            isOpen: boolean,
            hasPopup: string,
            target: HTMLElement | null,
            observer: MutationObserver | null
        }>();

        export function applyAriaPopup(state: { anchor: HTMLElement, popupId: string, isOpen: boolean, hasPopup: string, target: HTMLElement | null }) {
            const anchor = state.anchor;
            const trigger = Utils.findControl(anchor) ?? anchor;

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
    }
}

// bit-lazy: teardown Utils.disposeAriaPopup
