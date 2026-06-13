(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '10.4.5';

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
        && document.getElementById(BitBlazorUI.Callouts.current?.calloutId)?.contains(document.activeElement)) return;

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

window.addEventListener('resize', (e: any) => {
    const resizeTriggeredByOpenningKeyboard = document?.activeElement?.getAttribute('type') === 'text';
    if (window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH && resizeTriggeredByOpenningKeyboard) return;

    // A resize caused by the virtual keyboard (touch devices, notably iOS) should not
    // dismiss an open callout that owns the focused editable element.
    if (BitBlazorUI.Utils.isTouchDevice() && BitBlazorUI.Utils.isEditableElementFocused()) return;

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

namespace BitBlazorUI {
    export class BitController {
        id: string = Utils.uuidv4();
        controller = new AbortController();
        dotnetObj: DotNetObject | undefined;
    }
}