interface DotNetObject {

    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;

    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
}

class CalloutComponent {
    calloutId: string;
    overlayId: string;
    objRef: DotNetObject | null;

    constructor() {
        this.calloutId = "";
        this.overlayId = "";
        this.objRef = null;
    }

    update(calloutId: string, overlayId: string, obj: DotNetObject | null) {
        this.calloutId = calloutId;
        this.overlayId = overlayId;
        this.objRef = obj;
    }
}

class Bit {
    static currentCallout: CalloutComponent = new CalloutComponent();
    static currentDropDownCalloutId: string = "";
    static currentDropDownCalloutResponsiveModeIsEnabled: boolean = false;

    static setProperty(element: Record<string, any>, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: Record<string, any>, property: string): string | null {
        return element[property];
    }

    static getBoundingClientRect(element: HTMLElement): DOMRect {
        return element.getBoundingClientRect();
    }

    static getClientHeight(element: HTMLElement): number {
        return element.clientHeight;
    }

    static closeCurrentCalloutIfExists(calloutId: string, overlayId: string, obj: DotNetObject | null) {
        if (Bit.currentCallout.calloutId.length === 0 || Bit.currentCallout.overlayId.length === 0) {
            Bit.currentCallout.update(calloutId, overlayId, obj);
            return;
        }

        if (calloutId !== Bit.currentCallout.calloutId && overlayId !== Bit.currentCallout.overlayId) {
            const callout = document.getElementById(Bit.currentCallout.calloutId);
            if (callout == null)
                return;

            const overlay = document.getElementById(Bit.currentCallout.overlayId);
            if (overlay == null)
                return;

            callout.style.display = "none";
            overlay.style.display = "none";
            Bit.currentCallout.objRef?.invokeMethodAsync("CloseCallout");
            Bit.currentCallout.update(calloutId, overlayId, obj);
        }
    }

    static selectText(element: HTMLInputElement) {
        element.select();
    }

    static setStyle(element: HTMLElement, key: string, value: string) {
        (element.style as any)[key] = value;
    }

    static preventDefault(element: HTMLElement, event: string) {
        element.addEventListener(event, e => e.preventDefault(), { passive: false });
    }
}

window.addEventListener('scroll', (e: any) => {
    const minimumWidthForDropDownNormalOpen = 640;
    if ((Bit.currentDropDownCalloutId && window.innerWidth < minimumWidthForDropDownNormalOpen && Bit.currentDropDownCalloutResponsiveModeIsEnabled) ||
        (e.target.id && Bit.currentDropDownCalloutId === e.target.id)) return;

    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);

window.addEventListener('resize', (e: any) => {
    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);