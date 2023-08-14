interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

//window.addEventListener('load', e => {
//    Bit.init();
//})

window.addEventListener('scroll', (e: any) => {
    const minimumWidthForDropdownNormalOpen = 640;
    if ((BitCallouts.currentDropdownCalloutId && window.innerWidth < minimumWidthForDropdownNormalOpen && BitCallouts.currentDropdownCalloutResponsiveModeIsEnabled) ||
        (e.target.id && BitCallouts.currentDropdownCalloutId === e.target.id)) return;

    BitCallouts.replaceCurrentCallout("", "", null);
}, true);

window.addEventListener('resize', (e: any) => {
    const isMobile = window.screen.width < 640;
    const resizeTriggeredByKeyboardOpen = document?.activeElement?.getAttribute('type') === 'text';
    if (isMobile && resizeTriggeredByKeyboardOpen) return;

    BitCallouts.replaceCurrentCallout("", "", null);
}, true);