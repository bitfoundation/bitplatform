(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '10.5.0-pre-05';

interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

window.addEventListener('scroll', (e: Event) => {
    const currentCallout = BitBlazorUI.Callouts.current;
    if (window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH && currentCallout.responsiveMode) return;

    const target = e.target as HTMLElement;
    if (target?.id && target.id == currentCallout.scrollContainerId) return;

    // On touch devices (notably iOS) focusing an input shows the virtual keyboard, which fires a
    // scroll event as the browser brings the field into view. That should not dismiss an open
    // callout while an editable element tied to that callout is focused.
    const active = document.activeElement;
    if (BitBlazorUI.Utils.isTouchDevice() && BitBlazorUI.Utils.isEditableElementFocused() && active) {
        // The editable lives inside the callout (e.g. a dropdown's search box): the scroll is
        // internal, so keep the callout open and leave it where it is.
        if (document.getElementById(currentCallout.calloutId)?.contains(active)) return;

        // The editable is the callout's anchor (e.g. the SearchBox input): the page itself was
        // scrolled (commonly when the keyboard opens), moving the anchor. Keep the callout open
        // and re-anchor it to the anchor's new position instead of dismissing it.
        if (BitBlazorUI.Callouts.componentContains(active)) {
            BitBlazorUI.Callouts.reposition();
            return;
        }
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
        && (document.getElementById(BitBlazorUI.Callouts.current.calloutId)?.contains(active)
            || BitBlazorUI.Callouts.componentContains(active))) {
        BitBlazorUI.Callouts.reposition();
        return;
    }

    BitBlazorUI.Callouts.replaceCurrent();
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