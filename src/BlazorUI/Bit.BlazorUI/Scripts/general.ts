(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '10.6.0-pre-03';

interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

// Scroll events arrive faster than the page is laid out, and every reposition measures each open
// callout, so the paths that follow a scroll instead of dismissing on it coalesce their work into
// one run per animation frame.
let repositionFrame: number | null = null;
const repositionOnNextFrame = () => {
    if (repositionFrame != null) return;

    repositionFrame = requestAnimationFrame(() => {
        repositionFrame = null;
        BitBlazorUI.Callouts.reposition();
    });
};

window.addEventListener('scroll', (e: Event) => {
    const currentCallout = BitBlazorUI.Callouts.current;
    if (window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH && currentCallout.responsiveMode) return;

    const target = e.target as HTMLElement;
    if (target?.id && target.id == currentCallout.scrollContainerId) return;

    // A scroll that started inside one of the open callouts is the user reading it, not the page moving
    // out from under it, so it must not dismiss it. The named scroll container above is the part a
    // component measures and caps; anything else the content scrolls - a callout capped with a max height,
    // a scrollable the consumer put in it - is covered here.
    if (BitBlazorUI.Callouts.calloutContains(target)) return;

    // On touch devices (notably iOS) focusing an input shows the virtual keyboard, which fires a
    // scroll event as the browser brings the field into view. That should not dismiss an open
    // callout while an editable element tied to that callout is focused.
    const active = document.activeElement;
    if (BitBlazorUI.Utils.isTouchDevice() && BitBlazorUI.Utils.isEditableElementFocused() && active) {
        // The editable lives inside the callout (e.g. a dropdown's search box): the scroll is
        // internal, so keep the callout open and leave it where it is.
        if (BitBlazorUI.Callouts.calloutContains(active)) return;

        // The editable is the callout's anchor (e.g. the SearchBox input): the page itself was
        // scrolled (commonly when the keyboard opens), moving the anchor. Keep the callout open
        // and re-anchor it to the anchor's new position instead of dismissing it.
        if (BitBlazorUI.Callouts.componentContains(active)) {
            repositionOnNextFrame();
            return;
        }
    }

    // A callout that asked not to be dismissed by the page moving under it is re-anchored to its
    // component instead, so that it follows what it points at rather than being left behind by it.
    if (currentCallout.noDismiss) {
        repositionOnNextFrame();
        return;
    }

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

window.addEventListener('resize', () => {
    // A resize caused by the virtual keyboard (touch devices, notably iOS) should not dismiss an
    // open callout whose focused editable belongs to the callout or is its anchor component;
    // reposition it instead. Any other resize dismisses the callout as before.
    const active = document.activeElement;
    if (BitBlazorUI.Utils.isTouchDevice()
        && window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH
        && BitBlazorUI.Utils.isEditableElementFocused()
        && active
        && (BitBlazorUI.Callouts.calloutContains(active)
            || BitBlazorUI.Callouts.componentContains(active))) {
        BitBlazorUI.Callouts.reposition();
        return;
    }

    // See the scroll handler above: a callout that opted out of being dismissed by the page moving
    // under it follows its component instead.
    if (BitBlazorUI.Callouts.current.noDismiss) {
        BitBlazorUI.Callouts.reposition();
        return;
    }

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

// The callouts that render no overlay of their own leave the page its own clicks, so nothing lies between
// them and the page to hear that the user has gone elsewhere - the page itself tells them. The capture
// phase is what a stopPropagation inside the page cannot take away, and the primary button is what a
// dismissal is: a right-click is the contextmenu handler's below, and the middle one is the browser's.
document.addEventListener('pointerdown', (e: PointerEvent) => {
    if (e.button !== 0) return;

    BitBlazorUI.Callouts.dismissOnOutsideInteraction(e.target as Node);
}, true);

// A right-click dismisses the same callouts, except where the page took the click for itself - a handler
// that opens a context menu of its own calls preventDefault on it - since that is the page moving its own
// menu to the new point rather than the user leaving it. Whether the click was taken is only known once
// the event has been dispatched, which is what the deferral is for: the handlers that take it, Blazor's
// among them, all run before the task queued here does.
document.addEventListener('contextmenu', (e: MouseEvent) => {
    setTimeout(() => {
        if (e.defaultPrevented) return;

        BitBlazorUI.Callouts.dismissOnOutsideInteraction(e.target as Node);
    });
}, true);

// Keep an open callout aligned with the visible area when the visual viewport changes
// (iOS keyboard show/hide, pinch-zoom). window 'resize' doesn't fire for these on iOS, so
// listen to visualViewport directly. Reposition is a no-op when no callout is open.
if (window.visualViewport) {
    let settleTimer: ReturnType<typeof setTimeout> | null = null;
    const onVisualViewportChange = BitBlazorUI.Utils.throttle(() => {
        // Track the viewport live while it changes (throttled)...
        BitBlazorUI.Callouts.reposition();

        // ...and guarantee one final reposition after it settles. The keyboard animates the page
        // scroll/visual-viewport over a few hundred ms; a leading-edge throttle can drop the last
        // frame, leaving the callout anchored to a mid-scroll position of its component. Re-running
        // once the burst of events stops lands it on the final, settled geometry.
        if (settleTimer != null) clearTimeout(settleTimer);
        settleTimer = setTimeout(() => {
            settleTimer = null;
            BitBlazorUI.Callouts.reposition();
        }, 100);
    }, 16);
    window.visualViewport.addEventListener('resize', onVisualViewportChange);
    window.visualViewport.addEventListener('scroll', onVisualViewportChange);
}

namespace BitBlazorUI {
    export class BitController {
        id: string = Utils.uuidv4();
        controller = new AbortController();
        dotnetObj: DotNetObject | undefined;
    }
}