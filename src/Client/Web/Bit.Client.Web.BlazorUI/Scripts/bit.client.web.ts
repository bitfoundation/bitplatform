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
    static cureentCallout: CalloutComponent = new CalloutComponent();

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
        if (Bit.cureentCallout.calloutId.length === 0 || Bit.cureentCallout.overlayId.length === 0) {
            Bit.cureentCallout.update(calloutId, overlayId, obj);
            return;
        }

        if (calloutId !== Bit.cureentCallout.calloutId && overlayId !== Bit.cureentCallout.overlayId) {
            var callout = document.getElementById(Bit.cureentCallout.calloutId) ?? new HTMLElement();
            var overlay = document.getElementById(Bit.cureentCallout.overlayId) ?? new HTMLElement();
            callout.style.display = "none";
            overlay.style.display = "none";
            Bit.cureentCallout.objRef.invokeMethodAsync("CloseCallout");
            Bit.cureentCallout.update(calloutId, overlayId, obj);
        }
    }
}
