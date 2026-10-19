namespace BitBlazorUI {
    export namespace Utils {

        // The convention every surface that closes on Escape goes by (setupSurfaceEscape, setupEscapeGuard,
        // watchEscape, watchLayerEscape, setupEscape, setupTooltip): whoever acts on the key claims it by
        // preventing its default, so one press does one thing - a field inside a dialog clears, and the dialog
        // waits for the next press. A component that acts on Escape only in its .NET handler cannot say so from
        // there: a Blazor @onkeydown:preventDefault holds for every key at once, and the handler itself runs too
        // late for anything to read its answer. It says so in its markup instead, on the element it listens on,
        // and this is read off the DOM as the key goes down (at the foot of this script, in the capture phase, so the claim is on
        // the event before any surface around the component hears it):
        // - data-bit-esc="claim": the element acts on every plain Escape pressed in it;
        // - data-bit-esc="text": it acts while the field it is on holds text - the live text, which a field that
        //   only commits on blur has not reported to .NET yet.
        // The attribute is rendered from the very state the .NET handler decides on, so the key is claimed
        // exactly when the component acts on it, and an Escape the component leaves alone still reaches the
        // surface around it. An element that leaves the key alone hands it to the next one out, whose handler
        // hears a key pressed anywhere inside it - up to the nearest surface the key was pressed in (isEscapeSurface):
        // that surface is the innermost thing the key closes, so nothing it is rendered inside of (a message, a
        // dropdown's field) claims the key away from it. The same goes for the two other things that close on the
        // key without claiming it: a shown tooltip whose anchor the key was pressed in (setupTooltip), and a
        // component whose popup is open (Callouts.componentContains) - so a date picker in a navigation drawer
        // closes its calendar, and a tooltip on a button of a dialog goes away, while the drawer or the dialog
        // around them waits for the next press. An IME composition is nobody's here (the key cancels the
        // candidate), and neither is an Escape with a modifier, which the components leave alone (IsPlainEscape)
        // so the surface around them closes on it.
        export function claimEscape(e: KeyboardEvent) {
            if (e.key !== 'Escape' || e.defaultPrevented) return;
            if (Utils.isComposingKey(e)) return;
            if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;

            const target = e.target instanceof Element ? e.target : null;

            for (let element = target; element; element = element.parentElement) {
                const claim = element.getAttribute('data-bit-esc');
                if (claim === 'claim' || (claim === 'text' && (element as HTMLInputElement).value)) {
                    // A component between the key and the claim whose popup is open closes that popup on it.
                    if (Callouts.componentContains(target, element)) return;

                    Utils._escapeClaimers.set(e, element);
                    e.preventDefault();
                    return;
                }

                if (Utils.isEscapeSurface(element)) return;
                if (Utils._escapeBoundaries.some(isBoundary => isBoundary(element))) return;
            }
        }

        export const _escapeClaimers = new WeakMap<Event, Element>();

        // The element an Escape was claimed for (see claimEscape), if any. A claimer whose .NET handler hears every
        // key pressed inside it - a message dismissing on Escape - reads it to tell its own press from one that a
        // component or a surface inside it took first, which Blazor's dispatch goes on bubbling to it all the same.
        export function escapeClaimer(e: Event) {
            return Utils._escapeClaimers.get(e) ?? null;
        }

        // The elements that close on an Escape pressed inside them, registered by the surfaces of the library as they
        // start listening (setupSurfaceEscape, setupEscapeGuard, setupEscape; watchEscape and watchLayerEscape mark
        // theirs as escape roots), plus a dialog of the consumer's own.
        export const _escapeSurfaces = new WeakSet<Element>();

        export function isEscapeSurface(element: Element) {
            return Utils._escapeSurfaces.has(element)
                || !!(element as any).__bitEscapeRoot
                || element.matches('[role="dialog"],[role="alertdialog"],dialog');
        }

        export function forgetEscapeSurface(elementId: string) {
            const element = document.getElementById(elementId);
            if (element) {
                Utils._escapeSurfaces.delete(element);
            }
        }

        // Whether the node sits inside a surface nested in the boundary - a dialog, a panel or a modal rendered in its
        // content - rather than in the boundary itself. A component whose own .NET keydown handlers hear every key
        // pressed in its content, which no surface stops on its way out, leaves the ones such a surface is hearing
        // alone (BitDataGrid's cells and rows).
        export function isInNestedSurface(node: Node | null, boundary: Element) {
            for (let element = node instanceof Element ? node : node?.parentElement ?? null;
                 element && element !== boundary;
                 element = element.parentElement) {
                if (Utils.isEscapeSurface(element)) return true;
            }

            return false;
        }

        // The gestures in progress that an Escape puts back, each asked as the key goes down whether this press is
        // its own. They take the key on a listener of their own, added on the window as the gesture starts - after
        // the one that dismisses the tooltips, which has to leave the press to them.
        export const _escapeGestures = new Set<() => boolean>();

        export function addEscapeGesture(takes: () => boolean) {
            Utils._escapeGestures.add(takes);
        }

        export function removeEscapeGesture(takes: () => boolean) {
            Utils._escapeGestures.delete(takes);
        }

        // The other things a claim stops at, the way it stops at a surface (see claimEscape): an element whose script
        // closes something on the key without claiming it - the anchor of a shown tooltip (Utils.Tooltips.ts). Each
        // script adds its own as it loads, so the claim reads only the ones of the scripts an app was given.
        export const _escapeBoundaries: ((element: Element) => boolean)[] = [];

        export function addEscapeBoundary(isBoundary: (element: Element) => boolean) {
            Utils._escapeBoundaries.push(isBoundary);
        }

        // The listeners an Escape goes to right after the claim, in the order they were added: they run on the one
        // listener the claim runs on, so their place among the listeners of the window is the claim's own, whenever the
        // script adding them was loaded - which is what keeps the tooltips' Escape ahead of every surface of the library.
        export const _afterEscapeClaim: ((e: KeyboardEvent) => void)[] = [];

        export function afterEscapeClaim(listener: (e: KeyboardEvent) => void) {
            Utils._afterEscapeClaim.push(listener);
        }
    }
}

// bit-trim: markup data-bit-esc
// Kept for every component that claims an Escape in its markup (data-bit-esc, see claimEscape), and for every script
// that reads a claim. An Escape a component acts on is claimed for it before anything else hears the key: on the window
// in the capture phase, ahead of every listener of a surface the component sits in - a dialog, a modal, a panel, an
// overlay, a tooltip - so none of them closes on the press the component has taken.
window.addEventListener('keydown', (e: KeyboardEvent) => {
    BitBlazorUI.Utils.claimEscape(e);

    for (const listener of BitBlazorUI.Utils._afterEscapeClaim) listener(e);
}, true);
