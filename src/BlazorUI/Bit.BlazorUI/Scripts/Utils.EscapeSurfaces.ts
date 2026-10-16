namespace BitBlazorUI {
    export namespace Utils {

        export const _escapeClaimWatches = new Map<string, AbortController>();

        // Tells a component whose .NET keydown handler on its root acts on Escape, through its OnEscapeVerdict
        // callback, whether the Escape that handler is about to hear is its own: the one its root claimed (see
        // claimEscape), rather than one a component inside it acted on and claimed first (a search box emptying
        // itself, a field of a template clearing), which Blazor's dispatch bubbles up to the root all the same. A
        // press with a modifier or during an IME composition is claimed by nobody, so it is not the root's either.
        // The listener is on the root, so it runs before Blazor's document-level delegation, and the answer is
        // queued ahead of the keydown Blazor dispatches right after it. It only answers while the root claims the
        // key at all (data-bit-esc). A BitMessage goes by it to dismiss itself, and the Extras' dialogs and panels
        // through BitEscapeClaimWatch.
        export function watchEscapeClaim(id: string, root: HTMLElement, dotnetObj: DotNetObject) {
            Utils.unwatchEscapeClaim(id);

            if (!root || !(root instanceof Element)) return;

            const controller = new AbortController();

            Utils.sendEscapeVerdicts([root], dotnetObj, controller.signal, 'watchEscapeClaim',
                e => root.hasAttribute('data-bit-esc') ? Utils.escapeClaimer(e) !== root : null);

            Utils._escapeClaimWatches.set(id, controller);
        }

        export function unwatchEscapeClaim(id: string) {
            const controller = Utils._escapeClaimWatches.get(id);
            if (!controller) return;

            Utils._escapeClaimWatches.delete(id);
            controller.abort();
        }

        export const _surfaceEscapes = new Map<string, AbortController>();

        // Answers an Escape pressed inside a surface (a dialog) through the OnEscape callback - but only when the
        // key is the surface's own. Four things own it first: an IME composition, which Escape cancels; a
        // control that has taken it (defaultPrevented, see claimEscape); a component inside the surface whose own
        // popup is open - a combo box, a search box's suggestions, a date picker - which closes that popup on the
        // key and has its keydown bubble on up through the surface; and a surface nested inside this one (a dialog
        // opened from inside it), which has answered the key before it got here, or a layer opened over it (see
        // _escapeSurfaceOwners).
        // The popups are read as the key is pressed: this listener is on the element, so it runs before Blazor's
        // document-level delegation lets the component close its popup, while the stack of open callouts is still
        // the one the key was pressed against. Whether a control took the key is read once the whole dispatch is
        // over instead, since a Blazor handler's @onkeydown:preventDefault is applied by that same delegation,
        // after this listener has run. The decision is made here, in the browser, so the surface is only called
        // when it is to act - there is no round trip to ask whether it should.
        // Registering again on the same element replaces the previous registration.
        export function setupSurfaceEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeSurfaceEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            Utils._escapeSurfaces.add(element);

            const controller = new AbortController();
            const signal = controller.signal;

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                // The nearest surface owns the key whatever it goes on to do with it, so an outer one never
                // answers an Escape an inner one has already seen - a dialog, a panel, a modal or an open callout
                // nested inside this one - nor do the surfaces around it (see _escapeSurfaceOwners).
                if (Utils.claimSurfaceEscape(e, element)) return;

                if (Utils.isComposingKey(e) || e.defaultPrevented) return;

                if (Callouts.componentContains(e.target as Node | null, element)) return;

                setTimeout(() => {
                    if (e.defaultPrevented || signal.aborted) return;

                    dotnetObj.invokeMethodAsync('OnEscape');
                });
            }, { signal });

            Utils._surfaceEscapes.set(elementId, controller);
        }

        export function disposeSurfaceEscape(elementId: string) {
            const controller = Utils._surfaceEscapes.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._surfaceEscapes.delete(elementId);
            Utils.forgetEscapeSurface(elementId);
        }

        export const _escapes = new Map<string, AbortController>();

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
        export function setupEscape(elementId: string, dotnetObj: DotNetObject, triggerId?: string | null) {
            Utils.disposeEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            Utils._escapeSurfaces.add(element);

            const controller = new AbortController();

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape' || e.defaultPrevented) return;

                // An IME cancelling what it was composing is not dismissing the callout the field is in.
                if (Utils.isComposingKey(e)) return;

                if (Callouts.current.calloutId !== elementId) return;

                // A dialog, a panel or a modal in the content answered the key, and is not one of the open callouts.
                if (Utils.isNestedSurfaceEscape(e, element)) return;

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            if (triggerId) {
                Utils._escapesFromAnywhere.add(elementId);

                // In the capture phase, for the same reason the listener above is on the element: the stack of
                // open callouts is read before Blazor's document-level delegation lets a popup the key belongs to
                // (a dropdown list relocated to the body, holding the focus in its search box) close itself.
                document.addEventListener('keydown', e => {
                    // A press a component claimed (see claimEscape) is that component's, wherever its focus is, and
                    // one cancelling an IME composition is the composition's.
                    if (e.key !== 'Escape' || e.defaultPrevented || Utils.isComposingKey(e)) return;

                    if (Callouts.current.calloutId !== elementId) return;

                    const target = e.target as Node | null;
                    if (target && (element.contains(target) || document.getElementById(triggerId)?.contains(target))) return;

                    dotnetObj.invokeMethodAsync('OnEscape');
                }, { signal: controller.signal, capture: true });
            }

            Utils._escapes.set(elementId, controller);
        }

        export function disposeEscape(elementId: string) {
            Utils._escapesFromAnywhere.delete(elementId);

            const controller = Utils._escapes.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapes.delete(elementId);
            Utils.forgetEscapeSurface(elementId);
        }

        // The callouts that answer an Escape pressed anywhere in the page (setupEscape with a trigger), not only
        // one pressed inside them or on their trigger.
        export const _escapesFromAnywhere = new Set<string>();

        export const _escapeGuards = new Map<string, AbortController>();

        // The surface each Escape belongs to: the innermost one it was pressed inside of, of a dialog
        // (setupSurfaceEscape), an open panel (setupEscapeGuard), a modal (watchEscape), an open callout's root or
        // callout (watchNestedEscape) and a layer (watchLayerEscape). Each records itself as the key bubbles through its
        // root, so the first to see it is the one it is pressed nearest to - unless a layer covering the page has been
        // opened over that one, which then owns the press instead (layerOver). None of them stops a key from bubbling on
        // to the handlers around it - the page's own keyboard shortcuts keep working while one is open - so this is what
        // tells every other surface, and the .NET handler it has hearing the same key, that the press was not its own.
        export const _escapeSurfaceOwners = new WeakMap<Event, Element>();

        // Records the surface as the owner of the Escape - or the layer opened over it, if there is one - unless another
        // surface already is, and says whether one is: whether the press belongs to somebody else rather than to this
        // one.
        export function claimSurfaceEscape(e: Event, surface: Element) {
            if (Utils.isNestedSurfaceEscape(e, surface)) return true;

            const layer = Utils.layerOver(surface, e.target);

            Utils._escapeSurfaceOwners.set(e, layer ?? surface);
            return !!layer;
        }

        // Whether the Escape belongs to a surface other than this one (see _escapeSurfaceOwners): one nested inside it,
        // or a layer opened over it. A surface listening on more than one element of its own - a callout's root and its
        // callout - records whichever one the key is in.
        export function isNestedSurfaceEscape(e: Event, surface: Element) {
            const owner = Utils._escapeSurfaceOwners.get(e);
            return !!owner && owner !== surface;
        }

        // The layer covering the page (watchLayerEscape) that lies over the surface the key was pressed in, if any: one
        // opened from a button of the surface, which does not take the focus and so leaves it where it was, under the
        // layer. Which one is on top is a question of the stacking order rather than of the DOM - such a layer may be
        // rendered inside the surface's content or anywhere else in the page, and a z-index of the app's may put the
        // surface back over it - so it is read off the screen, at the focused element (or the surface): the first layer
        // or part of the surface found there, from the top down, is the one the user sees there. A layer the surface is
        // shown in is never over it. While a callout is open, the layers leave the key to it (watchLayerEscape), so none
        // takes it away from the surface it is open in either.
        export function layerOver(surface: Element, target: EventTarget | null): Element | null {
            if (Utils._layerEscapes.length === 0 || Callouts.current.calloutId) return null;

            const layers = Utils.openLayers().filter(l => l !== surface && l.contains(surface) === false
                                                                         && Utils.coversPage(l.id));
            if (layers.length === 0) return null;

            const from = target instanceof Element && target !== surface && surface.contains(target) ? target : null;
            const point = (from && Utils.visibleCenter(from)) ?? Utils.visibleCenter(surface);
            if (!point) return null;

            for (const hit of document.elementsFromPoint(point.x, point.y)) {
                for (let element: Element | null = hit; element; element = element.parentElement) {
                    if (layers.includes(element as HTMLElement)) return element;
                }

                if (surface.contains(hit)) return null;
            }

            return null;
        }

        // The center of the part of the element that is inside the viewport, if any of it is.
        export function visibleCenter(element: Element) {
            const rect = element.getBoundingClientRect();

            const left = Math.max(rect.left, 0);
            const top = Math.max(rect.top, 0);
            const right = Math.min(rect.right, window.innerWidth);
            const bottom = Math.min(rect.bottom, window.innerHeight);

            if (right <= left || bottom <= top) return null;

            return { x: (left + right) / 2, y: (top + bottom) / 2 };
        }

        // Tells a surface's .NET keydown handler on the elements, through its OnEscapeVerdict callback, what `verdict`
        // decides for the Escape it is about to hear - whether it is somebody else's - or nothing, for a null. The
        // listeners are on the elements, so they run before Blazor's document-level delegation, and the answer is
        // queued ahead of the keydown Blazor dispatches right after it. The handler takes each answer once
        // (BitEscapeVerdict), so not every press has to be answered: a true always is, and a false only when the answer
        // before it was true - and so may never have been taken, its keydown stopped on the way to the handler - or
        // there has been none since this watch began. Every other press is the handler's own, and it hears it without
        // a round trip. The elements share that record, as they share the one handler's answer.
        export function sendEscapeVerdicts(elements: Element[], dotnetObj: DotNetObject, signal: AbortSignal, source: string,
                                          verdict: (e: KeyboardEvent, element: Element) => boolean | null) {
            let lastSent: boolean | null = null;

            for (const element of elements) {
                element.addEventListener('keydown', e => {
                    const event = e as KeyboardEvent;
                    if (event.key !== 'Escape') return;

                    const foreign = verdict(event, element);
                    if (foreign === null || (foreign === false && lastSent === false)) return;

                    lastSent = foreign;

                    dotnetObj.invokeMethodAsync('OnEscapeVerdict', foreign)
                             .catch(err => console.error(`BitBlazorUI.Utils.${source}:`, err));
                }, { signal });
            }
        }

        export const _nestedEscapeWatches = new Map<string, AbortController>();

        // Records a surface whose .NET keydown handlers act on Escape as the owner of the Escapes pressed inside its
        // elements (see _escapeSurfaceOwners), and tells those handlers, through its OnEscapeVerdict callback, which ones
        // are somebody else's: a surface nested inside it owns them, a component inside it claimed them (see
        // claimEscape) - another callout's anchor among them, which claims its Escape from the render that opens it,
        // before its own watch is in place - or a popup opened from inside it closes on them (a dropdown whose list is
        // open while the focus is still on its field, see isEscapeOfOpenCallout, which `id` names the watching callout
        // to). A BitCallout watches its root (the anchor) and the callout while it is open (Callouts.toggle).
        export function watchNestedEscape(id: string, elementIds: string[], dotnetObj: DotNetObject) {
            Utils.unwatchNestedEscape(id);

            const elements = elementIds.map(elementId => document.getElementById(elementId))
                                       .filter((element): element is HTMLElement => !!element);

            const controller = new AbortController();

            Utils.sendEscapeVerdicts(elements, dotnetObj, controller.signal, 'watchNestedEscape', (e, element) => {
                const nested = Utils.claimSurfaceEscape(e, element);
                const claimer = Utils.escapeClaimer(e);

                return nested
                    || (!!claimer && claimer !== element && element.contains(claimer))
                    || Utils.isEscapeOfOpenCallout(e.target, id);
            });

            Utils._nestedEscapeWatches.set(id, controller);
        }

        export function unwatchNestedEscape(id: string) {
            const controller = Utils._nestedEscapeWatches.get(id);
            if (!controller) return;

            Utils._nestedEscapeWatches.delete(id);
            controller.abort();
        }

        // Tells a surface that dismisses itself on Escape through a Blazor keydown handler, through its
        // OnEscapeVerdict callback, whether the Escape it is about to hear belongs to something inside it
        // instead:
        // - an open popup the key closes (a dropdown whose list is open while the focus is still on its field),
        //   since the keydown goes on bubbling from it up through the surface and one press would take away
        //   the panel the user was filling in along with the list they meant to close;
        // - a handler that already prevented the key's default;
        // - an IME composition the key cancels;
        // - a surface nested inside this one - a panel, a dialog, a modal, a layer, an open callout's anchor - or a
        //   layer covering the page opened over it, which answers the key itself (or refuses it), see
        //   _escapeSurfaceOwners.
        // The answer has to be taken while the key is still going down: this listener is on the element, so it
        // runs before Blazor's document-level delegation lets the nested component close its popup (see
        // sendEscapeVerdicts).
        export function setupEscapeGuard(elementId: string, dotnetObj: DotNetObject) {
            Utils.disposeEscapeGuard(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            Utils._escapeSurfaces.add(element);

            const controller = new AbortController();

            Utils.sendEscapeVerdicts([element], dotnetObj, controller.signal, 'setupEscapeGuard', e => {
                // A closed surface is inert, so an Escape reaches it only while it is open - and from then on the
                // key is this surface's to answer or to refuse, never the one's it was opened from - unless a layer
                // has been opened over it since (see layerOver).
                const nested = element.hasAttribute('inert')
                    ? Utils.isNestedSurfaceEscape(e, element)
                    : Utils.claimSurfaceEscape(e, element);

                return nested
                    || e.defaultPrevented
                    || Utils.isComposingKey(e)
                    || Utils.isEscapeOfOpenCallout(e.target);
            });

            Utils._escapeGuards.set(elementId, controller);
        }

        export function disposeEscapeGuard(elementId: string) {
            const controller = Utils._escapeGuards.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapeGuards.delete(elementId);
            Utils.forgetEscapeSurface(elementId);
        }

        // Whether the innermost open callout is the one the key closes: it was pressed inside the callout or on
        // the component that opened it, or the callout answers an Escape from anywhere (one opened by hovering).
        // A callout that is only left open - one the focus has moved away from - does not close on an Escape
        // pressed somewhere else, so it does not take that Escape away from the surface either. The callout that is
        // asking (`ownCalloutId`, see watchNestedEscape) is never one the key belongs to instead of it.
        export function isEscapeOfOpenCallout(target: EventTarget | null, ownCalloutId?: string | null) {
            const current = Callouts.current;
            if (!current.calloutId || current.calloutId === ownCalloutId) return false;

            if (Utils._escapesFromAnywhere.has(current.calloutId)) return true;

            if (!(target instanceof Node)) return false;

            const callout = document.getElementById(current.calloutId);
            if (callout?.contains(target)) return true;

            const trigger = current.componentId ? document.getElementById(current.componentId) : null;

            return !!trigger?.contains(target);
        }

        export const _escapeWatches = new Map<string, AbortController>();

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
        // content, a dialog, a panel, a layer or an open callout's anchor in it - takes the presses made inside it, and
        // the outer one leaves them alone, as it does the ones a layer opened over it takes (see _escapeSurfaceOwners).
        // The modal's own handler, which reports the key, is told the same through OnEscapeVerdict (sendEscapeVerdicts).
        export function watchEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.unwatchEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const claimed = new WeakSet<Event>();
            const pressedInside = new WeakSet<Event>();

            (element as any).__bitEscapeRoot = true;

            // Where the key was pressed is read before anything has acted on it: in the capture phase on the window,
            // ahead of every listener of the page and of Blazor's delegation. A component's .NET handler that closes a
            // popup of its own on the key - a date picker in the surface whose calendar was relocated to the body - puts
            // that popup back inside the surface as it closes it, and under WebAssembly it does so while the event is
            // still being dispatched, so the same question asked once the key has bubbled to the window would find a
            // press made in the body inside the surface, and close the surface along with the popup.
            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                const target = e.target as Node | null;
                if (target && element.contains(target) && Utils.nearestEscapeRoot(target) === element) {
                    pressedInside.add(e);
                }
            }, { signal: controller.signal, capture: true });

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (Utils.isComposingKey(e) || Callouts.isOpenedFrom(element)) {
                    claimed.add(e);
                }
            }, { signal: controller.signal, capture: true });

            Utils.sendEscapeVerdicts([element], dotnetObj, controller.signal, 'watchEscape',
                                     e => Utils.claimSurfaceEscape(e, element));

            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (pressedInside.has(e) === false) return;

                if (claimed.has(e) || e.defaultPrevented || Utils.isNestedSurfaceEscape(e, element)) return;

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            Utils._escapeWatches.set(elementId, controller);
        }

        export function unwatchEscape(elementId: string) {
            const controller = Utils._escapeWatches.get(elementId);
            if (!controller) return;

            controller.abort();
            Utils._escapeWatches.delete(elementId);

            const element = document.getElementById(elementId) as any;
            if (element) {
                delete element.__bitEscapeRoot;
            }
        }

        export const _layerEscapes: { elementId: string, controller: AbortController }[] = [];

        // Dismisses a layer that covers the page (an overlay) on Escape through the OnEscape callback - the
        // keyboard's way of doing what a click on the layer does. Unlike a dialog, such a layer does not take the
        // focus when it opens, so the key is listened for on the window rather than on the element: the focus is
        // most often still on the control that opened it, behind the layer.
        // A press is the layer's only when nothing had the better claim to it: an input method composing, an open
        // callout anywhere in the page (a dropdown or a menu closes its own popup on the same key), a control that
        // answered the key by preventing its default, or another surface the key went through on its way here - a
        // dialog, a panel, a modal or another layer, whether it is rendered inside this layer's content or around
        // it (see _escapeSurfaceOwners). The layer records itself as the key bubbles through it like every other
        // surface, so a press made inside it is its own, and a surface around it leaves the press alone. A layer
        // covering the page that was opened over the surface the key was pressed in - from a button of a panel,
        // which keeps the focus - takes the press away from that surface (layerOver), and so does one opened over a
        // layer, so the one the user sees on top answers, whichever the focus happens to be in. With the focus
        // outside of every surface, only the layer watched last - the one opened last, so the topmost - answers, and
        // one press closes one layer. A layer that refuses the key (a blocking one) is watched all the same and says
        // no in .NET, so that the press does not fall through to a layer or a surface underneath it.
        export function watchLayerEscape(elementId: string, dotnetObj: DotNetObject) {
            Utils.unwatchLayerEscape(elementId);

            const element = document.getElementById(elementId);
            if (!element) return;

            const controller = new AbortController();
            const claimed = new WeakSet<Event>();

            (element as any).__bitEscapeRoot = true;

            // Read in the capture phase, ahead of the listeners that let an open callout close itself - after which
            // the stack of open callouts would no longer say there was one.
            // Whether the press would be this layer's when no surface owns it is read here as well, before anything
            // has acted on it (see watchEscape): a .NET handler that runs during the dispatch under WebAssembly may move
            // or close what the answer is read off - the dialog the focus is in, the layers still open - before the key
            // has bubbled to the window.
            const topmost = new WeakSet<Event>();

            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (Utils.isComposingKey(e) || Callouts.current.calloutId) {
                    claimed.add(e);
                }

                // A dialog of the consumer's own the focus is inside of owns the key. Inside a layer, one is no surface
                // that answers the key on its own: it is what the layer is the backdrop of, and the key closes it along
                // with the layer (a press made inside the layer is the layer's, below) unless the consumer claims it.
                const target = e.target as Node | null;
                if (target instanceof Element && target.closest('[role="dialog"],[role="alertdialog"],dialog')) return;

                const layers = Utils.openLayers();
                if (layers[layers.length - 1] === element) {
                    topmost.add(e);
                }
            }, { signal: controller.signal, capture: true });

            element.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                Utils.claimSurfaceEscape(e, element);
            }, { signal: controller.signal });

            window.addEventListener('keydown', e => {
                if (e.key !== 'Escape') return;

                if (claimed.has(e) || e.defaultPrevented) return;

                // The surface the key was pressed in answers it - or refuses it - and only this layer when it is that
                // surface, or the layer opened over it.
                const owner = Utils._escapeSurfaceOwners.get(e);
                if (owner) {
                    if (owner === element) {
                        dotnetObj.invokeMethodAsync('OnEscape');
                    }

                    return;
                }

                if (topmost.has(e) === false) return;

                dotnetObj.invokeMethodAsync('OnEscape');
            }, { signal: controller.signal });

            Utils._layerEscapes.push({ elementId, controller });
        }

        // The watched layers that are still open, in the order they were opened. A layer that has just closed is still
        // watched until the round trip that unwatches it is back, but it is inert from the render that closes it on -
        // so a second press right after the first goes to the layer underneath it rather than to the one it closed.
        export function openLayers() {
            return Utils._layerEscapes.map(l => document.getElementById(l.elementId))
                                      .filter((l): l is HTMLElement => !!l && l.hasAttribute('inert') === false);
        }

        export function unwatchLayerEscape(elementId: string) {
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
        export function coversPage(elementId: string) {
            const element = document.getElementById(elementId);
            return !!element && getComputedStyle(element).position === 'fixed';
        }

        export function nearestEscapeRoot(node: Node): Node | null {
            let current: Node | null = node;

            while (current && !(current as any).__bitEscapeRoot) {
                current = current.parentNode;
            }

            return current;
        }
    }
}
