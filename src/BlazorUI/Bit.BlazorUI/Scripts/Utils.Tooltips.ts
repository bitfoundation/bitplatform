namespace BitBlazorUI {
    export namespace Utils {

        // Mirrors the relationship a tooltip declares onto the element the reader actually lands on. The
        // tooltip renders the consumer's anchor inside a plain container of its own, and an aria-describedby
        // or an aria-labelledby on a container that is neither focusable nor interactive is an attribute no
        // screen reader ever reads: the control inside it is what the user reaches. The first focusable
        // descendant that is not part of the tooltip surface itself is that control.
        // An empty attribute takes the mirrored one away again - and only ever the one this code wrote, so
        // an anchor that names a description of its own keeps it.
        export function syncAriaDescription(rootId: string, tooltipId: string, attribute: string) {
            try {
                const root = document.getElementById(rootId);
                if (!root) return;

                // An interactive tooltip may hold something focusable of its own, which sits inside the same
                // root and would otherwise be taken for the anchor whenever the anchor holds none itself.
                const target = Utils.findControl(root, el => el.closest('.bit-ttp-wrp') !== null);

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

        export const _tooltips = new Map<string, { root: HTMLElement, dotnetObj: DotNetObject, controller: AbortController }>();
        export const _tooltipsByRoot = new Map<HTMLElement, DotNetObject>();
        export let _tooltipsController: AbortController | null = null;

        // Lets Escape dismiss a shown tooltip (WCAG 1.4.13 "dismissible") from the two places it can come from:
        // the keyboard inside the tooltip - on its anchor - and anywhere on the page while the pointer rests on
        // the tooltip, since a tooltip shown on hover is shown while the focus is wherever the user left it.
        // Either way the key is the tooltip's alone: it is taken before Blazor's document-level delegation sees
        // it, so a dialog or a callout the tooltip sits in is not dismissed by the same press, and a second
        // Escape reaches them as usual. The one exception is a key pressed on a component inside the anchor that
        // acts on it itself: the tooltip is dismissed along with it, and the key goes on to that component. Which
        // components act on it is never guessed from what the target looks like - the components say so, the
        // way every surface hears it: a claim (data-bit-esc, see claimEscape), or a popup of theirs that is open
        // and that the key closes (Callouts.componentContains). Whether a tooltip takes it is read off the DOM on
        // the spot - shown (bit-ttp-vis) and dismissible (data-bit-ttp-esc) - because the answer cannot wait for
        // a round trip. It also tells a tooltip a click opened about the press outside it that dismisses it.
        export function setupTooltip(rootId: string, tooltipId: string, attribute: string, dotnetObj: DotNetObject) {
            Utils.disposeTooltip(rootId);

            Utils.syncAriaDescription(rootId, tooltipId, attribute);

            const root = document.getElementById(rootId);
            if (!root) return;

            const controller = new AbortController();

            root.addEventListener('keydown', e => {
                // Only the plain key: an Escape with a modifier goes on to the surface around the tooltip, as it
                // does past every component that acts on Escape (claimEscape).
                if (e.key !== 'Escape' || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
                // A press cancelling an IME composition in a field of the anchor is the composition's: it goes on to the
                // field untaken, and no further than the page's own elements (see stopComposingEscape).
                if (Utils.isComposingKey(e)) return;
                if (!root.querySelector(':scope > .bit-ttp-wrp.bit-ttp-vis[data-bit-ttp-esc]')) return;

                if (e.defaultPrevented) {
                    // A press a component inside the anchor claimed is that component's, and the tooltip goes with
                    // it. One prevented by anything else - a listener of the consumer's own - is left alone.
                    const claimer = Utils.escapeClaimer(e);
                    if (!claimer || !root.contains(claimer)) return;
                } else if (!Callouts.componentContains(e.target as Node | null, root)) {
                    // Nothing inside the anchor acts on the key, so it is the tooltip's alone.
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

        export function disposeTooltip(rootId: string) {
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

        // The tooltips registered on the page that are shown right now and carry the given marker, read off the DOM
        // so that a toolbar or a grid of a few hundred tooltips is asked about only the few that are shown.
        export function shownTooltips(marker: string) {
            return Array.from(document.querySelectorAll<HTMLElement>(`.bit-ttp-wrp.bit-ttp-vis[${marker}]`))
                .map(wrp => wrp.parentElement)
                .filter((root): root is HTMLElement => !!root && Utils._tooltipsByRoot.has(root));
        }

        // Dismisses every dismissible tooltip under the pointer - a nested one along with the one around it - on an
        // Escape pressed anywhere in the page. It is not added by the tooltips: this script adds it once, as it loads, to
        // the claim's own listener on the window in the capture phase, right after the claim (afterEscapeClaim). Listeners
        // on one node run in
        // the order they were added, so one a tooltip added as it registered would land wherever the page happened
        // to be by then - behind the listener of a menu that was already open when the tooltip is in its content,
        // or of an overlay opened before the first tooltip was - and those would act on the press the tooltip takes.
        // Added first it is ahead of every surface of the library whenever it opened, and it costs nothing while no
        // tooltip is shown.
        // A press it takes goes no further than the window: it is prevented, and stopPropagation keeps it from the
        // document and everything under it - a callout's own Escape listener and Blazor's delegation included. The
        // other listeners on the window still hear it, and read it as taken (defaultPrevented), which leaves an app's
        // own listener there the key it has always been given.
        export function dismissTooltipsOnEscape(e: KeyboardEvent) {
            // A press a component claimed (see claimEscape) is that component's: the tooltip stays, and the key goes
            // on to the component rather than being stopped here.
            if (e.key !== 'Escape' || e.defaultPrevented) return;
            if (e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
            // A press cancelling an IME composition is the composition's, wherever the pointer rests.
            if (Utils.isComposingKey(e)) return;

            if (Utils._tooltipsByRoot.size === 0) return;

            // A gesture in progress that the key puts back (a swipe) is what the user is doing with the pointer the
            // tooltip is under, so the press is the gesture's, and the tooltip stays.
            for (const takes of Utils._escapeGestures) {
                if (takes()) return;
            }

            const target = e.target as Node | null;
            let taken = false;

            for (const root of Utils.shownTooltips('data-bit-ttp-esc')) {
                if (target && root.contains(target)) continue; // the root's own listener answers it
                if (!root.matches(':hover')) continue;

                Utils._tooltipsByRoot.get(root)!.invokeMethodAsync('OnEscape')
                     .catch(err => console.error("BitBlazorUI.Utils.dismissTooltipsOnEscape:", err));
                taken = true;
            }

            if (!taken) return;

            e.preventDefault();
            e.stopPropagation();
        }

        // The page-level listener every tooltip a press opens needs is shared by all of them, and asks the DOM for
        // the few tooltips that are actually shown instead of every tooltip on the page asking for itself - a
        // toolbar or a grid of a few hundred tooltips pays for one listener per press. (The Escape that dismisses a
        // tooltip has a listener of its own, added once for the page: see dismissTooltipsOnEscape.)
        export function ensureTooltipListeners() {
            if (Utils._tooltipsController) return;

            const controller = Utils._tooltipsController = new AbortController();

            // A tooltip a press of the anchor opened (data-bit-ttp-clk) is dismissed by the next press elsewhere.
            // The focus leaving the anchor says as much in some browsers, but Safari and the touch browsers never
            // focus a pressed button, and an anchor that is not focusable is never focused at all. Nothing is
            // prevented: the press goes on to do whatever it was aimed at.
            document.addEventListener('pointerdown', e => {
                const target = e.target as Node | null;

                for (const root of Utils.shownTooltips('data-bit-ttp-clk')) {
                    if (target && root.contains(target)) continue;

                    Utils._tooltipsByRoot.get(root)!.invokeMethodAsync('OnOutsidePress')
                         .catch(err => console.error("BitBlazorUI.Utils.setupTooltip:", err));
                }
            }, { signal: controller.signal, capture: true });
        }

        // Whether the element is the anchor of a tooltip that is shown and closes on Escape (see setupTooltip).
        export function showsDismissibleTooltip(element: Element) {
            return Utils._tooltipsByRoot.has(element as HTMLElement)
                && !!element.querySelector(':scope > .bit-ttp-wrp.bit-ttp-vis[data-bit-ttp-esc]');
        }
    }
}

// bit-trim: on-demand
// A claim stops at the anchor of a shown tooltip as it does at a surface, and the tooltips under the pointer take an
// Escape right after the claim (see dismissTooltipsOnEscape), ahead of every other listener of the library.
BitBlazorUI.Utils.addEscapeBoundary(element => BitBlazorUI.Utils.showsDismissibleTooltip(element));
BitBlazorUI.Utils.afterEscapeClaim(e => BitBlazorUI.Utils.dismissTooltipsOnEscape(e));
