namespace BitBlazorUI {
    export class Callouts {
        // Matches the attributes that Blazor's CSS isolation generates (e.g. `b-abc1234567`).
        private static readonly CSS_SCOPE_REGEX = /^b-[a-z0-9]+$/i;
        private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

        public static current = Callouts.DEFAULT_CALLOUT;
        private static _currentParams: BitCalloutParams | null = null;
        private static _calloutOriginalParents: Map<string, {
            parent: Element | null,
            nextSibling: Node | null,
            overlay: HTMLElement | null,
            overlayParent: Element | null,
            overlayNextSibling: Node | null,
            wrapper: HTMLElement | null
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

            const result = Callouts.position(component, callout, responsiveMode, dropDirection, isRtl,
                scrollContainerId, scrollOffset, headerId, footerId,
                setCalloutWidth, fixedCalloutWidth, maxWindowWidth);

            return result;
        }

        // Positions an already-open (and reparented) callout relative to its component.
        //
        // The hard part is the on-screen keyboard. getBoundingClientRect() and position:fixed do
        // NOT share the same coordinate origin once the visible viewport is offset, and the
        // relationship differs per engine: iOS reports getBoundingClientRect() in visual-viewport
        // space while fixed is laid out in layout-viewport space (they differ by offsetTop), but
        // Android Chrome keeps both in the same space (no difference). Rather than special-case
        // engines, we MEASURE the relationship at runtime with a hidden fixed probe stretched to
        // the layout viewport: its getBoundingClientRect() gives the layout viewport's edges in
        // getBoundingClientRect space (`probe.top/left/bottom`). We then compute every target in
        // getBoundingClientRect space and convert to style values:
        //   - style.top  = targetTopInRectSpace - probe.top
        //   - style.bottom = probe.bottom - targetBottomInRectSpace
        // The visible (visual viewport) band in that same space is [visibleTop, visibleBottom].
        // `top` anchors the "below" placement; `bottom` anchors the "above"/beside placements so
        // the browser keeps the callout glued to the component and grows it upward natively when
        // the content (e.g. an autocomplete list) changes - no reposition required.
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

            // Visible (visual) viewport size and how far it is offset within the layout viewport
            // (non-zero mainly when the on-screen keyboard is shown).
            const viewport = Utils.getViewport();
            const visualWidth = viewport.width;
            const visualHeight = viewport.height;
            const offsetTop = viewport.offsetTop;
            const offsetLeft = viewport.offsetLeft;

            // Measure the layout viewport's edges in getBoundingClientRect space (see method doc).
            const fixedRect = Callouts.measureFixedViewport();
            // Visible band, expressed in getBoundingClientRect space.
            const visibleTop = fixedRect.top + offsetTop;
            const visibleLeft = fixedRect.left + offsetLeft;
            const visibleBottom = visibleTop + visualHeight;
            const visibleRight = visibleLeft + visualWidth;

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

            // Distances from the component to each edge of the visible band (getBoundingClientRect space).
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

            // Horizontal placement is computed in getBoundingClientRect space then converted to a
            // style.left value via the measured offset.
            let left = componentX + (isRtl ? (componentWidth - calloutWidth) : 0);
            const right = left + calloutWidth;
            const correctedLeft = visibleRight - calloutWidth - 3;
            if (maxWindowWidth) {
                left = (windowWidth >= maxWindowWidth && (right > visibleRight)) ? correctedLeft : left;
            } else {
                left = (right > visibleRight) ? correctedLeft : left;
            }
            left = (left < visibleLeft) ? visibleLeft : left;
            callout.style.left = (left - fixedRect.left) + 'px';

            if (dropDirection == BitDropDirection.TopAndBottom) {
                if (calloutHeight <= distanceToBottom || distanceToBottom >= distanceToTop) {
                    callout.style.top = (componentY + componentHeight + 1 - fixedRect.top) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    callout.style.bottom = (fixedRect.bottom - (componentY - 1)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            } else {
                if (distanceToBottom >= calloutHeight) {
                    callout.style.top = (componentY + componentHeight + 1 - fixedRect.top) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if (distanceToTop >= calloutHeight) {
                    callout.style.bottom = (fixedRect.bottom - (componentY - 1)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if ((isRtl ? distanceToLeft : distanceToRight) >= calloutWidth) {
                    callout.style.left = ((isRtl ? (componentX - calloutWidth - 1) : (componentX + componentWidth + 1)) - fixedRect.left) + 'px';
                    callout.style.bottom = (fixedRect.bottom - (visibleBottom - 2)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    // Neither horizontal side has enough space; fall back to the opposite side but
                    // re-clamp so the callout never lands at a negative/off-viewport left offset.
                    let sideLeft = isRtl ? (componentX + componentWidth + 1) : (componentX - calloutWidth - 1);
                    if (sideLeft + calloutWidth > visibleRight) sideLeft = visibleRight - calloutWidth - 3;
                    if (sideLeft < visibleLeft) sideLeft = visibleLeft;
                    callout.style.left = (sideLeft - fixedRect.left) + 'px';
                    callout.style.bottom = (fixedRect.bottom - (visibleBottom - 2)) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                }
            }

            return (calloutWidth + calloutLeft) > document.body.offsetWidth;
        }

        // Measures the layout viewport's edges in getBoundingClientRect() space by stretching a
        // hidden position:fixed probe across it. Lets positioning convert between getBoundingClientRect
        // coordinates and style.top/left/bottom values regardless of how the engine relates fixed
        // positioning to getBoundingClientRect when the visible viewport is offset (iOS keyboard).
        private static measureFixedViewport(): { top: number, left: number, bottom: number } {
            try {
                const probe = document.createElement('div');
                probe.style.cssText = 'position:fixed;top:0;left:0;right:0;bottom:0;margin:0;border:0;padding:0;visibility:hidden;pointer-events:none;';
                document.body.appendChild(probe);
                const rect = probe.getBoundingClientRect();
                document.body.removeChild(probe);
                return { top: rect.top, left: rect.left, bottom: rect.bottom };
            } catch (e) {
                console.error('BitBlazorUI.Callouts.measureFixedViewport:', e);
                return { top: 0, left: 0, bottom: window.innerHeight };
            }
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

        // True when the node lives inside the open callout's anchor component (e.g. the SearchBox
        // input that owns the suggestion callout). Used so that a scroll/resize while that input is
        // focused - typically caused by the on-screen keyboard moving the page - re-anchors the
        // callout to the component's new position instead of dismissing it.
        public static componentContains(node: Node | null): boolean {
            if (node == null) return false;
            const componentId = Callouts._currentParams?.componentId;
            if (!componentId) return false;
            return document.getElementById(componentId)?.contains(node) ?? false;
        }

        private static moveCalloutToBody(calloutId: string, callout: HTMLElement, overlayId: string) {
            if (Callouts._calloutOriginalParents.has(calloutId)) return;
            if (callout.parentElement === document.body) return;

            const overlay = overlayId ? document.getElementById(overlayId) : null;
            const parent = callout.parentElement;
            const nextSibling = parent ? callout.nextSibling : null;

            // Relocating the callout to the body escapes the clipping/stacking-context issues of
            // its ancestors, but it also detaches it from the DOM subtree that the Blazor CSS
            // isolation scopes (and `::deep` rules) of the consuming components rely on.
            // To preserve those locally defined styles we wrap the relocated callout (and overlay)
            // in a `display: contents` element that carries the same scope attributes the callout
            // inherited from its original ancestors, so it keeps matching the scoped selectors.
            const scopes = Callouts.collectCssScopes(parent);
            const wrapper = document.createElement('div');
            wrapper.style.display = 'contents';
            wrapper.setAttribute('data-bit-callout-wrapper', calloutId);
            for (const scope of scopes) {
                wrapper.setAttribute(scope, '');
            }

            Callouts._calloutOriginalParents.set(calloutId, {
                parent: parent,
                nextSibling: nextSibling,
                overlay: overlay,
                overlayParent: overlay?.parentElement ?? null,
                overlayNextSibling: overlay?.nextSibling ?? null,
                wrapper: wrapper
            });

            if (overlay) {
                wrapper.appendChild(overlay);
            }
            wrapper.appendChild(callout);
            document.body.appendChild(wrapper);
        }

        private static collectCssScopes(element: Element | null): string[] {
            const scopes: string[] = [];
            let current: Element | null = element;
            while (current && current !== document.body && current !== document.documentElement) {
                const attributes = current.attributes;
                for (let i = 0; i < attributes.length; i++) {
                    const attribute = attributes[i];
                    if (attribute.value === '' &&
                        Callouts.CSS_SCOPE_REGEX.test(attribute.name) &&
                        scopes.indexOf(attribute.name) === -1) {
                        scopes.push(attribute.name);
                    }
                }
                current = current.parentElement;
            }
            return scopes;
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

            if (original.wrapper && original.wrapper.parentElement) {
                original.wrapper.parentElement.removeChild(original.wrapper);
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
