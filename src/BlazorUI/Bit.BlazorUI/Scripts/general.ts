interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
}

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

window.addEventListener('load', e => {
    Bit.init();
})

window.addEventListener('scroll', (e: any) => {
    const minimumWidthForDropDownNormalOpen = 640;
    if ((Bit.currentDropDownCalloutId && window.innerWidth < minimumWidthForDropDownNormalOpen && Bit.currentDropDownCalloutResponsiveModeIsEnabled) ||
        (e.target.id && Bit.currentDropDownCalloutId === e.target.id)) return;

    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);

window.addEventListener('resize', (e: any) => {
    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);