namespace BitBlazorUI {
    export namespace Utils {

        export const _focusOrigins = new Map<string, HTMLElement>();

        // The one focus-restore API: storeFocus, restoreFocus and forgetFocus, over one map keyed by the popup.
        //
        // Remembers the element the focus was on before a popup took it over, so that closing the popup can
        // hand the focus back to whatever opened it. A popup that leaves the focus behind on an element it is
        // about to remove drops the keyboard user back at the top of the page, which is the one place they
        // never navigated to. The body is not an element worth handing anything back to, so it is recorded
        // as "nothing to restore" rather than as an origin. `scopeId` names the popup itself, for one whose
        // focus may already be inside it when it stores (it was opened from within, or stores again while
        // open): the focus was already there, so there is nothing to hand back to - and an origin stored
        // earlier is kept rather than replaced by an element of the popup.
        // Reports whether an origin was stored.
        export function storeFocus(key: string, scopeId?: string | null): boolean {
            try {
                const active = document.activeElement as HTMLElement | null;

                if (!active || active === document.body || active === document.documentElement || typeof active.focus !== 'function') {
                    Utils._focusOrigins.delete(key);
                    return false;
                }

                const scope = scopeId ? document.getElementById(scopeId) : null;
                if (scope?.contains(active)) return false;

                Utils._focusOrigins.set(key, active);

                return true;
            } catch (e) { console.error("BitBlazorUI.Utils.storeFocus:", e); return false; }
        }

        // Hands the focus back to the element storeFocus recorded under the same key, and forgets it either
        // way - a stored origin is only ever restored once. `onlyWhenLost` is the guard for the usual case:
        // the focus is only the popup's to hand back while it is still where the popup left it, which after
        // the popup is taken out of the page means nowhere (the browser drops it on the body). A focus that
        // has since moved somewhere else belongs to whoever moved it. `scopeId` names a popup that is still in
        // the page while it closes - playing its exit animation, inert already - where the browser only moves
        // the focus out at its next focus fixup: a focus still inside it is as lost as one on the body.
        // Reports whether the focus was taken care of - handed back, or left where the user put it - which it
        // is not when there was no origin to hand it back to, or the origin has left the page: the caller then
        // has the focus to place itself.
        export function restoreFocus(key: string, onlyWhenLost: boolean, scopeId?: string | null): boolean {
            const element = Utils._focusOrigins.get(key);
            Utils._focusOrigins.delete(key);

            if (!element) return false;

            try {
                // The element that held the focus may have been taken off the page while the popup was open.
                if (!element.isConnected) return false;

                if (onlyWhenLost) {
                    const active = document.activeElement;
                    const scope = scopeId ? document.getElementById(scopeId) : null;
                    if (active && active !== document.body && active !== document.documentElement && !scope?.contains(active)) return true;
                }

                element.focus();

                return true;
            } catch (e) { console.error("BitBlazorUI.Utils.restoreFocus:", e); return false; }
        }

        // Drops a stored origin without focusing it, for a component that is disposed while its popup is
        // still open: there is no close for the focus to be handed back on, and the map would otherwise keep
        // the element alive for as long as the page lives.
        export function forgetFocus(key: string) {
            Utils._focusOrigins.delete(key);
        }
    }
}

// bit-lazy: teardown Utils.forgetFocus
