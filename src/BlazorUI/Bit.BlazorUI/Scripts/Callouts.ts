namespace BitBlazorUI {
    export class Callouts {
        // Matches the attributes that Blazor's CSS isolation generates (e.g. `b-abc1234567`).
        private static readonly CSS_SCOPE_REGEX = /^b-[a-z0-9]+$/i;
        private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };

        public static current = Callouts.DEFAULT_CALLOUT;
        private static _currentParams: BitCalloutParams | null = null;
        private static _calloutResizeObserver: ResizeObserver | null = null;
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

            // Re-anchor the callout whenever its own size changes (e.g. an autocomplete list
            // grows/shrinks as the query changes). Because callouts are anchored via `top`, the
            // bottom edge would otherwise drift away from / overlap the component when the
            // content height changes; repositioning recomputes `top` from the new height.
            Callouts.observeCalloutResize(callout);

            return result;
        }

        // Positions an already-open (and reparented) callout relative to its component.
        // Positions the callout so it stays correct while the iOS keyboard is shown (or the page
        // is pinch-zoomed). On iOS, getBoundingClientRect() reports coordinates relative to the
        // VISIBLE (visual) viewport, but a position:fixed element is laid out against the LAYOUT
        // viewport, which the keyboard pushes up by visualViewport.offsetTop. So any fixed
        // coordinate derived from a getBoundingClientRect() value must be translated by
        // offsetTop/offsetLeft to land where intended. We anchor exclusively via `top`/`left`
        // (never `bottom`/`right`, whose layout-viewport-size basis is unreliable in this state).
        // On every other browser offsetTop/offsetLeft are 0, so this reduces to plain positioning.
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

            // Visible (visual) viewport size, in screen coordinates (the same space that
            // getBoundingClientRect() reports in). offset* is how far the visible viewport is
            // pushed within the layout viewport (non-zero mainly when the iOS keyboard is up).
            const viewport = Utils.getViewport();
            const visualWidth = viewport.width;
            const visualHeight = viewport.height;
            const layoutHeight = viewport.layoutHeight;
            const offsetTop = viewport.offsetTop;
            const offsetLeft = viewport.offsetLeft;

            // When the visible viewport is offset from the layout viewport (notably while the iOS
            // keyboard is shown), position:fixed no longer lines up with getBoundingClientRect()'s
            // coordinates. In that case we anchor via `top` translated by the offset, and rely on
            // the ResizeObserver to re-anchor when the content height changes. Otherwise (Android,
            // desktop, iOS without keyboard) we keep the native `bottom` anchoring so the browser
            // grows the callout upward on content changes with no JS and no reposition jump.
            const isViewportOffset = offsetTop !== 0 || offsetLeft !== 0;

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

            // All distances are in visible-viewport (screen) space: the visible area spans
            // [0, visualHeight] vertically and [0, visualWidth] horizontally.
            const distanceToBottom = visualHeight - (componentY + componentHeight);
            const distanceToTop = componentY;
            const distanceToRight = visualWidth - (componentX + componentWidth);
            const distanceToLeft = componentX;

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
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollContainer.getBoundingClientRect().y - footerHeight - 10) + 'px';
                });

                return true;
            }

            let left = componentX + (isRtl ? (componentWidth - calloutWidth) : 0);
            const right = left + calloutWidth;
            const correctedLeft = visualWidth - calloutWidth - 3;
            if (maxWindowWidth) {
                left = (windowWidth >= maxWindowWidth && (right > visualWidth)) ? correctedLeft : left;
            } else {
                left = (right > visualWidth) ? correctedLeft : left;
            }
            left = (left < 0) ? 0 : left;
            callout.style.left = (left + offsetLeft) + 'px';

            if (dropDirection == BitDropDirection.TopAndBottom) {
                if (calloutHeight <= distanceToBottom || distanceToBottom >= distanceToTop) {
                    callout.style.top = (componentY + componentHeight + 1 + offsetTop) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else {
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                    Callouts.anchorAbove(callout, componentY, layoutHeight, offsetTop, isViewportOffset);
                }
            } else {
                if (distanceToBottom >= calloutHeight) {
                    callout.style.top = (componentY + componentHeight + 1 + offsetTop) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                } else if (distanceToTop >= calloutHeight) {
                    scrollContainer.style.maxHeight = Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                    Callouts.anchorAbove(callout, componentY, layoutHeight, offsetTop, isViewportOffset);
                } else if ((isRtl ? distanceToLeft : distanceToRight) >= calloutWidth) {
                    callout.style.left = ((isRtl ? (componentX - calloutWidth - 1) : (componentX + componentWidth + 1)) + offsetLeft) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                    Callouts.anchorToVisibleBottom(callout, visualHeight, layoutHeight, offsetTop, isViewportOffset);
                } else {
                    callout.style.left = ((isRtl ? (componentX + componentWidth + 1) : (componentX - calloutWidth - 1)) + offsetLeft) + 'px';
                    scrollContainer.style.maxHeight = Math.max(0, visualHeight - scrollOffset - headerHeight - footerHeight - 10) + 'px';
                    Callouts.anchorToVisibleBottom(callout, visualHeight, layoutHeight, offsetTop, isViewportOffset);
                }
            }

            return (calloutWidth + calloutLeft) > document.body.offsetWidth;
        }

        // Places the callout just above the component. When the visible viewport is offset from
        // the layout viewport (iOS keyboard), position:fixed must be anchored via `top` (translated
        // by the offset) using the callout's current height. Otherwise `bottom` is used, so the
        // browser keeps the bottom pinned to the component and grows the callout upward as its
        // content changes - with no JS reposition needed.
        private static anchorAbove(callout: HTMLElement, componentY: number, layoutHeight: number, offsetTop: number, isViewportOffset: boolean) {
            if (isViewportOffset) {
                callout.style.top = (componentY - callout.offsetHeight - 1 + offsetTop) + 'px';
            } else {
                callout.style.bottom = (layoutHeight - componentY + 1) + 'px';
            }
        }

        // Anchors the callout to the bottom of the visible area (used for the beside-the-component
        // placements). See anchorAbove for the top-vs-bottom rationale.
        private static anchorToVisibleBottom(callout: HTMLElement, visualHeight: number, layoutHeight: number, offsetTop: number, isViewportOffset: boolean) {
            if (isViewportOffset) {
                callout.style.top = (Math.max(0, visualHeight - callout.offsetHeight - 2) + offsetTop) + 'px';
            } else {
                callout.style.bottom = (layoutHeight - visualHeight + 2) + 'px';
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
            Callouts.unobserveCalloutResize();
        }

        // Watches the currently open callout for size changes (content updates) and re-runs
        // positioning so its anchored edge stays glued to the component. A single observer is
        // kept for whichever callout is currently open.
        private static observeCalloutResize(callout: HTMLElement) {
            Callouts.unobserveCalloutResize();
            if (typeof ResizeObserver === 'undefined') return;

            // Skip the initial synchronous callback (the callout was just positioned); only react
            // to subsequent size changes to avoid an unnecessary reposition right after opening.
            let initial = true;
            Callouts._calloutResizeObserver = new ResizeObserver(() => {
                if (initial) { initial = false; return; }
                // Only the offset (top-anchored) case needs JS to re-anchor on content changes.
                // Without an offset the native `bottom` anchoring already keeps the callout glued
                // to the component, so repositioning here would just add a needless reflow/jump.
                const vp = Utils.getViewport();
                if (vp.offsetTop === 0 && vp.offsetLeft === 0) return;
                Callouts.reposition();
            });
            Callouts._calloutResizeObserver.observe(callout);
        }

        private static unobserveCalloutResize() {
            Callouts._calloutResizeObserver?.disconnect();
            Callouts._calloutResizeObserver = null;
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
