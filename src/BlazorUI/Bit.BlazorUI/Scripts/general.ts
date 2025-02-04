(BitBlazorUI as any).version = (window as any)['bit-blazorui version'] = '9.4.0-pre-03';

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

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

window.addEventListener('resize', (e: any) => {
    const resizeTriggeredByOpenningKeyboard = document?.activeElement?.getAttribute('type') === 'text';
    if (window.innerWidth < BitBlazorUI.Utils.MAX_MOBILE_WIDTH && resizeTriggeredByOpenningKeyboard) return;

    BitBlazorUI.Callouts.replaceCurrent();
}, true);

namespace BitBlazorUI {
    export class BitController {
        id: string = Utils.uuidv4();
        controller = new AbortController();
        dotnetObj: DotNetObject | undefined;
    }
}