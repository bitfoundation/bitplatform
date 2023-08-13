class BitCalloutComponent {
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

class BitCallouts {
    static currentDropdownCalloutId = "";
    static currentCallout: BitCalloutComponent = new BitCalloutComponent();
    static currentDropdownCalloutResponsiveModeIsEnabled = false;

    static replaceCurrentCallout(calloutId: string, overlayId: string, obj: DotNetObject | null) {
        if (BitCallouts.currentCallout.calloutId.length === 0 || BitCallouts.currentCallout.overlayId.length === 0) {
            BitCallouts.currentCallout.update(calloutId, overlayId, obj);
            return;
        }

        if (calloutId !== BitCallouts.currentCallout.calloutId && overlayId !== BitCallouts.currentCallout.overlayId) {
            const callout = document.getElementById(BitCallouts.currentCallout.calloutId);
            if (callout == null) return;

            const overlay = document.getElementById(BitCallouts.currentCallout.overlayId);
            if (overlay == null) return;

            callout.style.display = "none";
            overlay.style.display = "none";
            BitCallouts.currentCallout.objRef?.invokeMethodAsync("CloseCallout");
            BitCallouts.currentCallout.update(calloutId, overlayId, obj);
        }
    }

    static toggleCallout(componentId: string, calloutId: string, overlayId: string, isCalloutOpen: boolean, dotNetObj: DotNetObject) {
        const component = document.getElementById(componentId);
        if (component == null) return;

        const callout = document.getElementById(calloutId);
        if (callout == null) return;

        //const overlay = document.getElementById(overlayId);
        //if (overlay == null) return;

        if (!isCalloutOpen) {
            callout.style.display = "none";
            //overlay.style.display = "none";
            BitCallouts.currentCallout.update("", "", null);
        } else {
            BitCallouts.replaceCurrentCallout(calloutId, overlayId, dotNetObj);
            callout.style.display = "block";
            //overlay.style.display = "block";

            const calloutHeight = callout.offsetHeight;
            const calloutWidth = callout.offsetWidth;

            const componentWidth = component.offsetWidth;
            const componentHeight = component.offsetHeight;

            const { x: componentX, y: componentY } = component.getBoundingClientRect();

            const lengthToScreenBottom = window.innerHeight - (componentHeight + componentY);
            const lengthToScreenRight = window.innerWidth - (componentWidth + componentX);

            if (lengthToScreenBottom >= calloutHeight) {        // show callout to the bottom
                callout.style.top = componentY + componentHeight + 1 + "px";
                callout.style.left = componentX + "px";
                callout.style.right = "unset";
                callout.style.bottom = "unset";
            } else if (componentY >= calloutHeight) {           // show callout to the top
                callout.style.bottom = lengthToScreenBottom + componentHeight + 1 + "px";
                callout.style.left = componentX + "px";
                callout.style.right = "unset";
                callout.style.top = "unset";
            } else if (lengthToScreenRight >= calloutWidth) {   // show callout to the right
                callout.style.left = componentX + componentWidth + 1 + "px";
                callout.style.bottom = "2px";
                callout.style.right = "unset";
                callout.style.top = "unset";
            } else {                                            // show callout to the left
                callout.style.left = componentX - calloutWidth - 1 + "px";
                callout.style.bottom = "2px";
                callout.style.top = "unset";
                callout.style.right = "unset";
            }
        }
    }

    static clearCallout(calloutId: string) {
        if (BitCallouts.currentCallout.calloutId !== calloutId) return;

        BitCallouts.replaceCurrentCallout("", "", null);
    }
}