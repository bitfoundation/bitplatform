namespace BitBlazorUI {
    export class Callouts {
        private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

        public static current = Callouts.DEFAULT_CALLOUT;

        public static toggle(
            dotnetObj: DotNetObject,
            componentId: string,
            component: HTMLElement | null,
            calloutId: string,
            callout: HTMLElement | null,
            isCalloutOpen: boolean,
            responsiveMode: BitResponsiveMode,
            dropDirection: BitDropDirection,
            isRtl: boolean,
            scrollContainerId: string,
            scrollOffset: number,
            headerId: string,
            footerId: string,
            setCalloutWidth: boolean,
            maxWidth: number,
        ) {
            component ??= document.getElementById(componentId);
            if (component == null) return false;

            callout ??= document.getElementById(calloutId);
            if (callout == null) return false;

            const windowWidth = window.innerWidth;
            const windowHeight = window.innerHeight;

            if (!isCalloutOpen) {
                if (windowWidth < Utils.MAX_MOBILE_WIDTH && responsiveMode) {
                    callout.style.transform = '';
                } else {
                    callout.style.display = 'none';
                }
                Callouts.reset();
                return false;
            }

            const scrollContainer = (scrollContainerId
                ? document.getElementById(scrollContainerId)
                : { style: {} as any, getBoundingClientRect: () => ({ y: 0 }) })!;

            const header = (headerId
                ? document.getElementById(headerId)
                : { getBoundingClientRect: () => ({ height: 0 }) })!;

            const footer = (footerId
                ? document.getElementById(footerId)
                : { getBoundingClientRect: () => ({ height: 0 }) })!;

            Callouts.replaceCurrent({ dotnetObj, calloutId, responsiveMode, scrollContainerId });
            callout.style.display = 'block';

            //clear last style
            callout.style.top = '';
            callout.style.left = '';
            callout.style.right = '';
            callout.style.bottom = '';
            callout.style.width = '';
            callout.style.height = '';
            callout.style.maxHeight = '';
            callout.style.aspectRatio = '';
            scrollContainer.style.height = '';
            scrollContainer.style.maxHeight = '';

            const componentWidth = component.offsetWidth;
            const componentHeight = component.offsetHeight;
            const { x: componentX, y: componentY } = component.getBoundingClientRect();

            let calloutWidth = callout.offsetWidth;
            const calloutHeight = callout.offsetHeight;
            const { x: calloutLeft } = callout.getBoundingClientRect();

            const distanceToBottom = windowHeight - (componentY + componentHeight);
            const distanceToRight = windowWidth - (componentX + componentWidth);

            const { height: headerHeight } = header.getBoundingClientRect();
            const { height: footerHeight } = footer.getBoundingClientRect();

            if (setCalloutWidth) {
                let width = Math.max(componentWidth, calloutWidth);
                if (responsiveMode == BitResponsiveMode.Panel &&
                    width < Utils.MIN_MOBILE_WIDTH &&
                    windowWidth < Utils.MAX_MOBILE_WIDTH) {
                    width = windowWidth > Utils.MIN_MOBILE_WIDTH
                        ? Utils.MIN_MOBILE_WIDTH
                        : windowWidth;
                }
                callout.style.width = width + 'px';
                calloutWidth = width;
            }

            if (windowWidth < Utils.MAX_MOBILE_WIDTH && responsiveMode) {
                callout.style.opacity = '1';
                callout.style.transform = 'translate(0,0)';
                callout.style.maxHeight = windowHeight + 'px';

                setTimeout(() => {
                    scrollContainer.style.maxHeight = (windowHeight - scrollContainer.getBoundingClientRect().y - footerHeight - 10) + 'px';
                });

                return true;
            }

            let left = componentX + (isRtl ? (componentWidth - calloutWidth) : 0);
            const right = left + calloutWidth;
            const correctedLeft = windowWidth - calloutWidth - 3;
            if (maxWidth) {
                left = (windowWidth >= maxWidth && (right > windowWidth)) ? correctedLeft : left;
            } else {
                left = (right > windowWidth) ? correctedLeft : left;
            }
            left = (left < 0) ? 0 : left;
            callout.style.left = left + 'px';

            if (dropDirection == BitDropDirection.TopAndBottom) {
                if (calloutHeight <= distanceToBottom || distanceToBottom >= componentY) {
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    callout.style.bottom = distanceToBottom + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (componentY - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            } else {
                if (distanceToBottom >= calloutHeight) {
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if (componentY >= calloutHeight) {
                    callout.style.bottom = distanceToBottom + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = (componentY - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if ((isRtl ? componentX : distanceToRight) >= calloutWidth) {
                    callout.style.bottom = '2px';
                    callout.style.left = (isRtl ? (componentX - calloutWidth - 1) : (componentX + componentWidth + 1)) + 'px';
                    scrollContainer.style.maxHeight = (windowHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    callout.style.bottom = '2px';
                    callout.style.left = (isRtl ? (componentX + componentWidth + 1) : (componentX - calloutWidth - 1)) + 'px';
                    scrollContainer.style.maxHeight = (windowHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            }

            return (calloutWidth + calloutLeft) > document.body.offsetWidth;
        }

        public static reset() {
            Callouts.current = Callouts.DEFAULT_CALLOUT;
        }

        public static replaceCurrent(callout?: BitCallout) {
            callout = callout || Callouts.DEFAULT_CALLOUT;
            const current = Callouts.current;

            if (current.calloutId.length === 0) {
                Callouts.current = callout;
                return;
            }

            //close the previous one
            if (callout.calloutId !== current.calloutId) {
                const previousCallout = document.getElementById(current.calloutId);
                previousCallout && (previousCallout.style.display = 'none');

                const overlay = current.overlayId && document.getElementById(current.overlayId);
                overlay && (overlay.style.display = 'none');

                current.dotnetObj?.invokeMethodAsync('CloseCallout');

                Callouts.current = callout;
            }
        }

        public static clear(calloutId: string) {
            if (Callouts.current.calloutId !== calloutId) return;

            Callouts.replaceCurrent();
        }
    }

    interface BitCallout {
        calloutId: string;
        overlayId?: string;
        dotnetObj?: DotNetObject;
        scrollContainerId?: string;
        responsiveMode?: BitResponsiveMode;
    }

    enum BitDropDirection {
        All,
        TopAndBottom
    }

    enum BitResponsiveMode {
        None,
        Panel,
        Top,
        Bottom
    }
}
