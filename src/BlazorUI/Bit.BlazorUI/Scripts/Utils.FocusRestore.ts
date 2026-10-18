namespace BitBlazorUI {
    export namespace Utils {

        export const _focusOrigins = new Map<string, HTMLElement>();

        // Remembers the element the focus was on at the moment a popup took it over, keyed by the popup, so
        // that closing the popup can hand the keyboard back to where it came from. A popup that moves the
        // focus into itself and then takes its content away leaves the focus on the body, which sends the
        // keyboard back to the top of the page - the one thing the WAI-ARIA dialog pattern asks not to happen.
        // Reports whether an origin was remembered, which it is not for a focus that was on the body.
        export function captureFocusOrigin(elementId: string): boolean {
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
        export function restoreFocusOrigin(elementId: string): boolean {
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

        export function disposeFocusOrigin(elementId: string) {
            Utils._focusOrigins.delete(elementId);
        }

        // Remembers the element the focus was on before a popup took it over, so that closing the popup can
        // hand the focus back to whatever opened it. A popup that leaves the focus behind on an element it is
        // about to remove drops the keyboard user back at the top of the page, which is the one place they
        // never navigated to. The body is not an element worth handing anything back to, so it is recorded
        // as "nothing to restore" rather than as an origin.
        export function storeFocus(key: string) {
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
        export function restoreFocus(key: string, onlyWhenLost: boolean, scopeId?: string | null) {
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
        export function forgetFocus(key: string) {
            Utils._focusOrigins.delete(key);
        }
    }
}

// bit-lazy: teardown Utils.disposeFocusOrigin
