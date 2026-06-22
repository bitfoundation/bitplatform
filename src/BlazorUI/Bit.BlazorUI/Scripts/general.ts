(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '10.5.0-pre-03';

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

    // On touch devices (notably iOS) focusing an input shows the virtual keyboard, which
    // fires a scroll event as the browser brings the field into view. That should not
    // dismiss an open callout (e.g. a dropdown with a search box), so keep it open while
    // an editable element is focused, but only when that editable belongs to the active callout.
    if (BitBlazorUI.Utils.isTouchDevice()
        && BitBlazorUI.Utils.isEditableElementFocused()
        && document.activeElement
        && document.getElementById(BitBlazorUI.Callouts.current.calloutId)?.contains(document.activeElement)) return;

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

window.addEventListener('resize', () => {
    // A resize caused by the virtual keyboard (touch devices, notably iOS) should not dismiss
    // an open callout that owns the focused editable element; reposition it to the new visible
    // area instead. Any other resize dismisses the callout as before.
    if (BitBlazorUI.Utils.isTouchDevice()
        && window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH
        && BitBlazorUI.Utils.isEditableElementFocused()
        && document.activeElement
        && document.getElementById(BitBlazorUI.Callouts.current.calloutId)?.contains(document.activeElement)) {
        BitBlazorUI.Callouts.reposition();
        return;
    }

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

// Keep an open callout aligned with the visible area when the visual viewport changes
// (iOS keyboard show/hide, pinch-zoom). window 'resize' doesn't fire for these on iOS, so
// listen to visualViewport directly. Reposition is a no-op when no callout is open.
if (window.visualViewport) {
    const onVisualViewportChange = BitBlazorUI.Utils.throttle(() => {
        BitBlazorUI.Callouts.reposition();
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