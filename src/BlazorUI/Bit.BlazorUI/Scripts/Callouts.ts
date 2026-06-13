namespace BitBlazorUI {
    export class Callouts {
        private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

        public static current = Callouts.DEFAULT_CALLOUT;
        private static _currentParams: BitCalloutParams | null = null;
        private static _calloutOriginalParents: Map<string, {
            parent: Element | null,
            nextSibling: Node | null,
            overlay: HTMLElement | null,
            overlayParent: Element | null,
            overlayNextSibling: Node | null
        }> = new Map();

        public static toggle(
            dotnetObj: DotNetObject,
            componentId: string,
            component: HTMLElement | null,
            calloutId: string,
            callout: HTMLElement | null,
            overlayId: string,
            isCalloutOpen: boolean,
            responsiveMode: BitResponsiveMode,
            dropDirection: BitDropDirection,
            isRtl: boolean,
            scrollContainerId: string,
            scrollOffset: number,
            headerId: string,
            footerId: string,
            setCalloutWidth: boolean,
            fixedCalloutWidth: boolean,
            maxWindowWidth: number,
        ) {
            component ??= document.getElementById(componentId);
            if (component == null) return false;

            callout ??= document.getElementById(calloutId);
            if (callout == null) return false;

            if (!isCalloutOpen) {
                const windowWidth = window.innerWidth;
                if (windowWidth < Utils.MAX_MOBILE_WIDTH && responsiveMode) {
                    callout.style.opacity = '0';
                    callout.style.transform = '';
                } else {
                    callout.style.display = 'none';
                }
                Callouts.restoreCalloutToOriginalParent(calloutId, callout);
                if (Callouts.current.calloutId === calloutId) {
                    Callouts.reset();
                }
                return false;
            }

            Callouts.moveCalloutToBody(calloutId, callout, overlayId);

            Callouts.replaceCurrent({ dotnetObj, calloutId, overlayId, responsiveMode, scrollContainerId });

            // Remember the inputs used to position this callout so it can be repositioned later
            // when the visual viewport changes (e.g. the iOS keyboard shows/hides).
            Callouts._currentParams = {
                componentId, calloutId, overlayId, responsiveMode, dropDirection, isRtl,
                scrollContainerId, scrollOffset, headerId, footerId,
                setCalloutWidth, fixedCalloutWidth, maxWindowWidth
            };

            return Callouts.position(component, callout, responsiveMode, dropDirection, isRtl,
                scrollContainerId, scrollOffset, headerId, footerId,
                setCalloutWidth, fixedCalloutWidth, maxWindowWidth);
        }

        // Positions an already-open (and reparented) callout relative to its component.
        // Uses the visual viewport for both axes so the layout stays correct while the iOS
        // keyboard is shown or the page is pinch-zoomed. getBoundingClientRect() values are
        // layout-viewport relative, so the visible* values translate the visible region into
        // the same coordinate space; fixed-position offsets (bottom) use the layout viewport.
        private static position(
            component: HTMLElement,
            callout: HTMLElement,
            responsiveMode: BitResponsiveMode,
            dropDirection: BitDropDirection,
            isRtl: boolean,
            scrollContainerId: string,
            scrollOffset: number,
            headerId: string,
            footerId: string,
            setCalloutWidth: boolean,
            fixedCalloutWidth: boolean,
            maxWindowWidth: number,
        ) {
            const windowWidth = window.innerWidth;

            const viewport = Utils.getViewport();
            const visualHeight = viewport.height;
            const layoutHeight = viewport.layoutHeight;
            const visibleTop = viewport.offsetTop;
            const visibleBottom = viewport.offsetTop + viewport.height;
            const visibleLeft = viewport.offsetLeft;
            const visibleRight = viewport.offsetLeft + viewport.width;

            const scrollContainer = (scrollContainerId
                ? document.getElementById(scrollContainerId)
                : { style: {} as any, getBoundingClientRect: () => ({ y: 0 }) })!;

            const header = (headerId
                ? document.getElementById(headerId)
                : { getBoundingClientRect: () => ({ height: 0 }) })!;

            const footer = (footerId
                ? document.getElementById(footerId)
                : { getBoundingClientRect: () => ({ height: 0 }) })!;

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

            const distanceToBottom = visibleBottom - (componentY + componentHeight);
            const distanceToTop = componentY - visibleTop;
            const distanceToRight = visibleRight - (componentX + componentWidth);
            const distanceToLeft = componentX - visibleLeft;

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
            if (fixedCalloutWidth) {
                let width = Math.min(componentWidth, calloutWidth);
                callout.style.width = width + 'px';
                calloutWidth = width;
            }

            if (windowWidth < Utils.MAX_MOBILE_WIDTH && responsiveMode) {
                callout.style.opacity = '1';
                callout.style.transform = 'translate(0,0)';
                callout.style.maxHeight = visualHeight + 'px';

                setTimeout(() => {
                    scrollContainer.style.maxHeight = Math.max(0, visibleBottom - scrollContainer.getBoundingClientRect().y - footerHeight - 10) + 'px';
                });

                return true;
            }

            let left = componentX + (isRtl ? (componentWidth - calloutWidth) : 0);
            const right = left + calloutWidth;
            const correctedLeft = visibleRight - calloutWidth - 3;
            if (maxWindowWidth) {
                left = (windowWidth >= maxWindowWidth && (right > visibleRight)) ? correctedLeft : left;
            } else {
                left = (right > visibleRight) ? correctedLeft : left;
            }
            left = (left < visibleLeft) ? visibleLeft : left;
            callout.style.left = left + 'px';

            if (dropDirection == BitDropDirection.TopAndBottom) {
                if (calloutHeight <= distanceToBottom || distanceToBottom >= distanceToTop) {
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    callout.style.bottom = (layoutHeight - componentY + 1) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            } else {
                if (distanceToBottom >= calloutHeight) {
                    callout.style.top = componentY + componentHeight + 1 + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if (distanceToTop >= calloutHeight) {
                    callout.style.bottom = (layoutHeight - componentY + 1) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if ((isRtl ? distanceToLeft : distanceToRight) >= calloutWidth) {
                    callout.style.bottom = (layoutHeight - visibleBottom + 2) + 'px';
                    callout.style.left = (isRtl ? (componentX - calloutWidth - 1) : (componentX + componentWidth + 1)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    callout.style.bottom = (layoutHeight - visibleBottom + 2) + 'px';
                    callout.style.left = (isRtl ? (componentX + componentWidth + 1) : (componentX - calloutWidth - 1)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            }

            return (calloutWidth + calloutLeft) > document.body.offsetWidth;
        }

        // Re-runs positioning for the currently open callout. Used when the visual viewport
        // changes (iOS keyboard show/hide, pinch-zoom) so the callout doesn't stay anchored
        // to the previous viewport geometry.
        public static reposition() {
            const params = Callouts._currentParams;
            if (params == null) return;
            if (Callouts.current.calloutId !== params.calloutId) return;

            const component = document.getElementById(params.componentId);
            const callout = document.getElementById(params.calloutId);
            if (component == null || callout == null) return;

            Callouts.position(component, callout, params.responsiveMode, params.dropDirection, params.isRtl,
                params.scrollContainerId, params.scrollOffset, params.headerId, params.footerId,
                params.setCalloutWidth, params.fixedCalloutWidth, params.maxWindowWidth);
        }

        public static reset() {
            Callouts.current = Callouts.DEFAULT_CALLOUT;
            Callouts._currentParams = null;
        }

        private static moveCalloutToBody(calloutId: string, callout: HTMLElement, overlayId: string) {
            if (callout.parentElement === document.body) return;

            const overlay = overlayId ? document.getElementById(overlayId) : null;
            const parent = callout.parentElement;
            const nextSibling = parent ? callout.nextSibling : null;

            Callouts._calloutOriginalParents.set(calloutId, {
                parent: parent,
                nextSibling: nextSibling,
                overlay: overlay,
                overlayParent: overlay?.parentElement ?? null,
                overlayNextSibling: overlay?.nextSibling ?? null
            });

            document.body.appendChild(callout);
            if (overlay) {
                document.body.appendChild(overlay);
            }
        }

        private static restoreCalloutToOriginalParent(calloutId: string, callout: HTMLElement) {
            const original = Callouts._calloutOriginalParents.get(calloutId);
            if (!original) return;

            Callouts._calloutOriginalParents.delete(calloutId);

            if (original.parent) {
                if (original.nextSibling && original.nextSibling.parentNode === original.parent) {
                    original.parent.insertBefore(callout, original.nextSibling);
                } else {
                    original.parent.appendChild(callout);
                }
            }

            if (original.overlay && original.overlayParent) {
                if (original.overlayNextSibling && original.overlayNextSibling.parentNode === original.overlayParent) {
                    original.overlayParent.insertBefore(original.overlay, original.overlayNextSibling);
                } else {
                    original.overlayParent.appendChild(original.overlay);
                }
            }
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
                if (previousCallout) {
                    previousCallout.style.display = 'none';
                    Callouts.restoreCalloutToOriginalParent(current.calloutId, previousCallout);
                }

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

    interface BitCalloutParams {
        componentId: string;
        calloutId: string;
        overlayId: string;
        responsiveMode: BitResponsiveMode;
        dropDirection: BitDropDirection;
        isRtl: boolean;
        scrollContainerId: string;
        scrollOffset: number;
        headerId: string;
        footerId: string;
        setCalloutWidth: boolean;
        fixedCalloutWidth: boolean;
        maxWindowWidth: number;
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
