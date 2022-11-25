interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
}

//window.addEventListener('load', e => {
//    Bit.init();
//})

window.addEventListener('scroll', (e: any) => {
    const minimumWidthForDropDownNormalOpen = 640;
    if ((Bit.currentDropDownCalloutId && window.innerWidth < minimumWidthForDropDownNormalOpen && Bit.currentDropDownCalloutResponsiveModeIsEnabled) ||
        (e.target.id && Bit.currentDropDownCalloutId === e.target.id)) return;

    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);

window.addEventListener('resize', (e: any) => {
    const isMobile = window.screen.width < 640;
    const resizeTriggeredByKeyboardOpen = document?.activeElement?.getAttribute('type') === 'text';
    if (isMobile && resizeTriggeredByKeyboardOpen) return;

    Bit.closeCurrentCalloutIfExists("", "", null);
}, true);