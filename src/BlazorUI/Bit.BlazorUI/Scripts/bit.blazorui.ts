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
    static currentDropDownCalloutId = "";
    static currentCallout = new CalloutComponent();
    static currentDropDownCalloutResponsiveModeIsEnabled = false;

    static setProperty(element: Record<string, any>, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: Record<string, any>, property: string): string | null {
        return element[property];
    }

    static getClientHeight(element: HTMLElement): number {
        return element.clientHeight;
    }

    static getBoundingClientRect(element: HTMLElement): DOMRect {
        return element.getBoundingClientRect();
    }

    static scrollElementIntoView(targetElementId: string) {
        const element = document.getElementById(targetElementId);

        if (element != null) {
            element.scrollIntoView({
                behavior: "smooth",
                block: "start",
                inline: "nearest"
            });
        }
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

    static getComputedTransform(element: HTMLElement) {
        const computedStyle = window.getComputedStyle(element);
        const matrix = computedStyle.getPropertyValue('transform');
        const matched = matrix.match(/matrix\((.+)\)/);

        if (matched && matched.length > 1) {
            const splitted = matched[1].split(',');
            return {
                ScaleX: +splitted[0],
                SkewY: +splitted[1],
                SkewX: +splitted[2],
                ScaleY: +splitted[3],
                TranslateX: +splitted[4],
                TranslateY: +splitted[5]
            }
        }

        return null;
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