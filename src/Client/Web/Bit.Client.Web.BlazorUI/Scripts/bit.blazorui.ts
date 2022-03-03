class CalloutComponent {
    calloutId: string;
    overlayId: string;
    objRef: any;

    constructor() {
        this.calloutId = "";
        this.overlayId = "";
    }

    update(calloutId: string, overlayId: string, obj: any) {
        this.calloutId = calloutId;
        this.overlayId = overlayId;
        this.objRef = obj;
    }
}

class Bit {
    static currentCallout: CalloutComponent = new CalloutComponent();
    static currentDropDownCalloutId: string = "";

    static setProperty(element: { [key: string]: any }, property: string, value: any): void {
        element[property] = value;
    }

    static getProperty(element: { [key: string]: any }, property: string): string | null {
        return element[property];
    }

    static getBoundingClientRect(element: any): object {
        return element.getBoundingClientRect();
    }

    static getClientHeight(element: { [key: string]: any }): string {
        return element.clientHeight;
    }

    static closeCurrentCalloutIfExists(calloutId: string, overlayId: string, obj: any) {
        if (Bit.currentCallout.calloutId.length === 0 || Bit.currentCallout.overlayId.length === 0) {
            Bit.currentCallout.update(calloutId, overlayId, obj);
            return;
        }

        if (calloutId !== Bit.currentCallout.calloutId && overlayId !== Bit.currentCallout.overlayId) {
            var callout = document.getElementById(Bit.currentCallout.calloutId) ?? new HTMLElement();
            var overlay = document.getElementById(Bit.currentCallout.overlayId) ?? new HTMLElement();
            callout.style.display = "none";
            overlay.style.display = "none";
            Bit.currentCallout.objRef.invokeMethodAsync("CloseCallout");
            Bit.currentCallout.update(calloutId, overlayId, obj);
        }
    }

    static selectText(element: any) {
        element.select();
    }
}

window.addEventListener('scroll', (e: any) => {
    if (e.target.id && Bit.currentDropDownCalloutId === e.target.id) return;
    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);