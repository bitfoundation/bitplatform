interface BitCallout {
    calloutId: string;
    overlayId?: string;
    dotnetObj?: DotNetObject;
    isResponsive?: boolean;
    scrollContainerId?: string;
}

enum BitDropDirection {
    Auto,
    TopAndBottom
}

class BitCallouts {
    public static readonly MIN_MOBILE_WIDTH = 320;
    public static readonly MAX_MOBILE_WIDTH = 640;
    private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

    public static current: BitCallout = BitCallouts.DEFAULT_CALLOUT;

    public static reset() {
        BitCallouts.current = BitCallouts.DEFAULT_CALLOUT;
    }

    public static replaceCurrent(callout?: BitCallout) {
        callout = callout || BitCallouts.DEFAULT_CALLOUT;
        const current = BitCallouts.current;

        if (current.calloutId.length === 0) {
            BitCallouts.current = callout;
            return;
        }

        //close the previous one
        if (callout.calloutId !== current.calloutId) {
            const previousCallout = document.getElementById(current.calloutId);
            previousCallout && (previousCallout.style.display = 'none');

            const overlay = current.overlayId && document.getElementById(current.overlayId);
            overlay && (overlay.style.display = 'none');

            current.dotnetObj?.invokeMethodAsync('CloseCallout');

            BitCallouts.current = callout;
        }
    }

    static toggle(
        dotnetObj: DotNetObject,
        componentId: string,
        calloutId: string,
        isCalloutOpen: boolean,
        isResponsive: boolean,
        dropDirection: BitDropDirection,
        isRtl: boolean,
        scrollContainerId: string,
        scrollOffset: number
    ) {
        const component = document.getElementById(componentId);
        if (component == null) return;

        const callout = document.getElementById(calloutId);
        if (callout == null) return;

        const scrollContainer = (scrollContainerId ? document.getElementById(scrollContainerId) : document.createElement('dummy'))!;

        if (!isCalloutOpen) {
            callout.style.display = 'none';
            BitCallouts.reset();
        } else {
            BitCallouts.replaceCurrent({ dotnetObj, calloutId, isResponsive, scrollContainerId });
            callout.style.display = 'block';

            const componentWidth = component.offsetWidth;
            const componentHeight = component.offsetHeight;

            const calloutHeight = callout.offsetHeight;
            const calloutWidth = callout.offsetWidth;

            const { x: componentX, y: componentY } = component.getBoundingClientRect();

            const distanceToScreenBottom = window.innerHeight - (componentHeight + componentY);
            const distanceToScreenRight = window.innerWidth - (componentWidth + componentX);

            let width = componentWidth;

            if (isResponsive && componentWidth < BitCallouts.MIN_MOBILE_WIDTH && window.innerWidth < BitCallouts.MAX_MOBILE_WIDTH) {
                width = window.innerWidth > BitCallouts.MIN_MOBILE_WIDTH ? BitCallouts.MIN_MOBILE_WIDTH : window.innerWidth;
            }

            callout.style.width = width + 'px';

            //clear last style
            callout.style.top = '';
            callout.style.left = '';
            callout.style.right = '';
            callout.style.bottom = '';
            callout.style.height = '';
            callout.style.maxHeight = '';
            scrollContainer.style.height = '';
            scrollContainer.style.maxHeight = '';

            if (window.innerWidth < BitCallouts.MAX_MOBILE_WIDTH && isResponsive) {
                callout.style.top = '0';
                callout.style[isRtl ? 'left' : 'right'] = '0';
                callout.style.maxHeight = window.innerHeight + 'px';
                setTimeout(() => {
                    scrollContainer.style.maxHeight = (window.innerHeight - scrollContainer.getBoundingClientRect().y - 10) + 'px';
                    scrollContainer.style.height = scrollContainer.style.maxHeight;
                });

            } else if (dropDirection == BitDropDirection.TopAndBottom) {
                callout.style.left = componentX + 'px';

                if (calloutHeight <= distanceToScreenBottom || distanceToScreenBottom >= componentY) {
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (distanceToScreenBottom - scrollOffset - 10) + 'px';
                } else {
                    callout.style.bottom = distanceToScreenBottom + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (componentY - scrollOffset - 10) + 'px';
                }
            } else {
                if (distanceToScreenBottom >= calloutHeight) {
                    callout.style.left = componentX + 'px';
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (distanceToScreenBottom - scrollOffset - 10) + 'px';
                } else if (componentY >= calloutHeight) {
                    callout.style.left = componentX + 'px';
                    callout.style.bottom = distanceToScreenBottom + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (componentY - scrollOffset - 10) + 'px';
                } else if (distanceToScreenRight >= calloutWidth) {
                    callout.style.bottom = '2px';
                    callout.style.left = componentX + componentWidth + 1 + 'px';
                    scrollContainer.style.maxHeight = (window.innerHeight - scrollOffset - 10) + 'px';
                } else {
                    callout.style.bottom = '2px';
                    callout.style.left = componentX - calloutWidth - 1 + 'px';
                    scrollContainer.style.maxHeight = (window.innerHeight - scrollOffset - 10) + 'px';
                }
            }
        }
    }

    static clear(calloutId: string) {
        if (BitCallouts.current.calloutId !== calloutId) return;

        BitCallouts.replaceCurrent();
    }
}