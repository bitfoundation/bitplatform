interface DotNetObject {
    invokeMethod<T>(methodIdentifier: string, ...args: any[]): T;
    invokeMethodAsync<T>(methodIdentifier: string, ...args: any[]): Promise<T>;
    dispose(): void;
}

//window.addEventListener('load', e => {
//    Bit.init();
//})

window.addEventListener('scroll', (e: Event) => {
    const currentCallout = BitCallouts.current;
    if (window.innerWidth < Bit.MAX_MOBILE_WIDTH && currentCallout.responsiveMode) return;

    const target = e.target as HTMLElement;
    if (target?.id && target.id == currentCallout.scrollContainerId) return;

    BitCallouts.replaceCurrent();
}, true);

window.addEventListener('resize', (e: any) => {
    const resizeTriggeredByOpenningKeyboard = document?.activeElement?.getAttribute('type') === 'text';
    if (window.innerWidth < Bit.MAX_MOBILE_WIDTH && resizeTriggeredByOpenningKeyboard) return;

    BitCallouts.replaceCurrent();
}, true);