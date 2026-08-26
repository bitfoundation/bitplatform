namespace BitBlazorUI {
    export class Callouts {
        // Matches the attributes that Blazor's CSS isolation generates (e.g. `b-abc1234567`).
        private static readonly CSS_SCOPE_REGEX = /^b-[a-z0-9]+$/i;
        private static readonly DEFAULT_CALLOUT: BitCallout = { calloutId: '' };
        // How close to a corner of the callout the arrow may be placed, so that it never lands on the
        // rounded corner itself, where half of it would be cut away by the radius.
        private static readonly ARROW_CORNER_INSET = 16;

        public static current = Callouts.DEFAULT_CALLOUT;
        private static _currentParams: BitCalloutParams | null = null;
        private static _calloutOriginalParents: Map<string, {
            parent: Element | null,
            nextSibling: Node | null,
            overlay: HTMLElement | null,
            overlayParent: Element | null,
            overlayNextSibling: Node | null,
            arrow: HTMLElement | null,
            arrowParent: Element | null,
            arrowNextSibling: Node | null,
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
            // An optional cap on the scrollable content of the callout, in pixels. It is applied on top
            // of the space the viewport leaves, so it can only ever make the list shorter. Zero (the
            // default for the components that do not offer it) means the viewport alone decides.
            maxHeight: number = 0,
            // The id of the arrow (beak) element that points at the component, or '' for the callouts
            // that do not show one - which is what every component that does not offer an arrow passes.
            arrowId: string = '',
            // Extra distance in pixels between the component and the callout, on top of the 1px the
            // placement always leaves. Zero (the default) keeps the callout tucked against the component.
            gap: number = 0,
            // Keeps the page moving under the callout from dismissing it: a scroll or a resize re-anchors
            // it to its component instead. Another callout opening still takes over from it.
            noDismiss: boolean = false,
            // The side of the component the callout is preferably placed on ('top', 'bottom', 'start' or
            // 'end'), or '' to leave the placement entirely to the drop direction, which is what every
            // component that does not offer the choice passes.
            preferredSide: string = '',
        ) {
            component ??= document.getElementById(componentId);
            if (component == null) return false;

            callout ??= document.getElementById(calloutId);
            if (callout == null) return false;

            const arrow = arrowId ? document.getElementById(arrowId) : null;

            if (!isCalloutOpen) {
                const windowWidth = window.innerWidth;
                if (windowWidth < Utils.MAX_MOBILE_WIDTH && responsiveMode) {
                    callout.style.opacity = '0';
                    callout.style.transform = '';
                } else {
                    callout.style.display = 'none';
                }
                if (arrow) {
                    arrow.style.display = 'none';
                }
                Callouts.restoreCalloutToOriginalParent(calloutId, callout);
                if (Callouts.current.calloutId === calloutId) {
                    Callouts.reset();
                }
                return false;
            }

            Callouts.moveCalloutToBody(calloutId, callout, overlayId, arrowId);

            Callouts.replaceCurrent({ dotnetObj, calloutId, overlayId, arrowId, responsiveMode, scrollContainerId, noDismiss });

            // Remember the inputs used to position this callout so it can be repositioned later
            // when the visual viewport changes (e.g. the iOS keyboard shows/hides).
            Callouts._currentParams = {
                componentId, calloutId, overlayId, responsiveMode, dropDirection, isRtl,
                scrollContainerId, scrollOffset, headerId, footerId,
                setCalloutWidth, fixedCalloutWidth, maxWindowWidth, maxHeight, arrowId, gap, preferredSide
            };

            const result = Callouts.position(component, callout, responsiveMode, dropDirection, isRtl,
                scrollContainerId, scrollOffset, headerId, footerId,
                setCalloutWidth, fixedCalloutWidth, maxWindowWidth, true, maxHeight, arrowId, gap, preferredSide);

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
        // `top` anchors the "below" placement; `bottom` anchors the "above" placement so the browser
        // keeps the callout glued to the component and grows it upward natively when the content
        // (e.g. an autocomplete list) changes - no reposition required.
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
            isEntering: boolean,
            maxHeight: number = 0,
            arrowId: string = '',
            gap: number = 0,
            preferredSide: string = '',
        ) {
            const windowWidth = window.innerWidth;

            // The consumer's own cap on the scrollable content, applied on top of the space the
            // viewport leaves so that it can only ever shorten the list, never push it off the screen.
            const cap = (height: number) => maxHeight > 0 ? Math.min(height, maxHeight) : height;

            // The distance the callout is pushed away from the component: the 1px the placement has
            // always left, plus whatever the consumer asked for on top of it.
            const offset = 1 + Math.max(0, gap);

            const arrow = arrowId ? document.getElementById(arrowId) : null;

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
            // The consumer's cap shortens the scrollable list (and only it), so the callout the placement
            // below decides for can be shorter than the one measured here: whatever the cap takes off that
            // list comes off this height too, or a list that fits under the component only because of the
            // cap would still be pushed above it.
            const calloutHeight = callout.offsetHeight - (maxHeight > 0 && scrollContainerId
                                                            ? Math.max(0, (scrollContainer as HTMLElement).offsetHeight - maxHeight)
                                                            : 0);
            const { x: calloutLeft } = callout.getBoundingClientRect();

            // Distances from the component to each edge of the visible band (getBoundingClientRect space),
            // less the room the requested gap takes out of each of them.
            const distanceToBottom = visibleBottom - (componentY + componentHeight) - (offset - 1);
            const distanceToTop = componentY - visibleTop - (offset - 1);
            const distanceToRight = visibleRight - (componentX + componentWidth) - (offset - 1);
            const distanceToLeft = componentX - visibleLeft - (offset - 1);

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
                // A panel is sized against the screen rather than placed against the component, so it is
                // never the detached one the placement below can hide - including when the screen only
                // just became narrow enough for the panel, with the callout already hidden as detached.
                callout.style.visibility = '';

                // A callout shown as a panel is sized against the screen rather than placed against the
                // component, so there is nothing left for an arrow to point at.
                if (arrow) {
                    arrow.style.display = 'none';
                }

                setTimeout(() => {
                    scrollContainer.style.maxHeight = cap(Math.max(0, visibleBottom - scrollContainer.getBoundingClientRect().y - footerHeight - 10)) + 'px';
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

            // Which side of the component the callout ended up on, so the entry animation can slide it
            // out of the component instead of always dropping it down from above, and so the arrow knows
            // which edge of the callout to sit on.
            let placement: BitCalloutPlacement = 'below';

            // The side the consumer asked for, when it has the room. It is a preference rather than a
            // demand: the opposite side is the first fallback, the way a flip works, and when neither of
            // the two fits the automatic placement below decides as it always has. Only the callout
            // component offers this, so for everything else `preferredSide` is empty and none of it runs.
            const placedOnPreferredSide = preferredSide
                ? Callouts.placeOnPreferredSide(preferredSide, isRtl, callout, scrollContainer, cap,
                    fixedRect, offset, componentX, componentY, componentWidth, componentHeight,
                    calloutWidth, calloutHeight, distanceToTop, distanceToBottom, distanceToLeft, distanceToRight,
                    visualHeight, visibleTop, visibleBottom, scrollOffset, headerHeight, footerHeight)
                : null;

            if (placedOnPreferredSide) {
                placement = placedOnPreferredSide;
            } else if (dropDirection == BitDropDirection.TopAndBottom) {
                if (calloutHeight <= distanceToBottom || distanceToBottom >= distanceToTop) {
                    callout.style.top = (componentY + componentHeight + offset - fixedRect.top) + 'px';
                    scrollContainer.style.maxHeight = cap(Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
                } else {
                    placement = 'above';
                    callout.style.bottom = (fixedRect.bottom - (componentY - offset)) + 'px';
                    scrollContainer.style.maxHeight = cap(Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
                }
            } else {
                if (distanceToBottom >= calloutHeight) {
                    callout.style.top = (componentY + componentHeight + offset - fixedRect.top) + 'px';
                    scrollContainer.style.maxHeight = cap(Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
                } else if (distanceToTop >= calloutHeight) {
                    placement = 'above';
                    callout.style.bottom = (fixedRect.bottom - (componentY - offset)) + 'px';
                    scrollContainer.style.maxHeight = cap(Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
                } else if ((isRtl ? distanceToLeft : distanceToRight) >= calloutWidth) {
                    placement = isRtl ? 'left' : 'right';
                    callout.style.left = ((isRtl ? (componentX - calloutWidth - offset) : (componentX + componentWidth + offset)) - fixedRect.left) + 'px';
                } else {
                    // Neither horizontal side has enough space; fall back to the opposite side but
                    // re-clamp so the callout never lands at a negative/off-viewport left offset.
                    placement = isRtl ? 'right' : 'left';
                    let sideLeft = isRtl ? (componentX + componentWidth + offset) : (componentX - calloutWidth - offset);
                    if (sideLeft + calloutWidth > visibleRight) sideLeft = visibleRight - calloutWidth - 3;
                    if (sideLeft < visibleLeft) sideLeft = visibleLeft;
                    callout.style.left = (sideLeft - fixedRect.left) + 'px';
                }

                // A callout placed beside the component is aligned with the top of it, the way a submenu
                // is, and only slides along the screen when it would otherwise hang off it. Pinning it to
                // the bottom of the viewport instead - which is what it used to do - left it floating far
                // away from the component it belongs to whenever the component sat near the top.
                if (placement === 'left' || placement === 'right') {
                    const available = Math.max(0, visualHeight - 4);
                    const height = Math.min(calloutHeight, available);
                    let top = componentY;
                    if (top + height > visibleBottom - 2) top = visibleBottom - 2 - height;
                    if (top < visibleTop + 2) top = visibleTop + 2;
                    callout.style.top = (top - fixedRect.top) + 'px';
                    scrollContainer.style.maxHeight = cap(Math.max(0, available - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
                }
            }

            // A callout whose component has been scrolled out of the visible area has nothing left to
            // point at, and the clamping above would leave it stuck against an edge of the screen next to
            // whatever happens to be there. It is hidden rather than dismissed, so it comes back with its
            // component: a callout is normally dismissed by the page moving under it, and the ones that
            // asked not to be are the ones that reach this.
            // Strictly outside on one of the axes: a zero-height component (which is what a callout with
            // no anchor of its own is positioned against) sitting exactly on an edge of the band is still
            // on screen, not past it.
            const detached = (componentY + componentHeight) < visibleTop || componentY > visibleBottom
                          || (componentX + componentWidth) < visibleLeft || componentX > visibleRight;
            callout.style.visibility = detached ? 'hidden' : '';

            if (arrow) {
                arrow.style.visibility = detached ? 'hidden' : '';

                Callouts.positionArrow(arrow, callout, placement, fixedRect,
                    componentX, componentY, componentWidth, componentHeight,
                    visibleTop, visibleLeft, visibleBottom, visibleRight);
            }

            if (isEntering) {
                Callouts.playEntryAnimation(callout, placement);
            }

            return (calloutWidth + calloutLeft) > document.body.offsetWidth;
        }

        // Places the callout on the side of the component the consumer asked for, or on the opposite one
        // when that side has no room for it, and reports which of the two it used. Returns null when
        // neither fits, which leaves the placement to the automatic one that weighs every allowed side.
        // Nothing is written to the callout until a side is settled on, so a null return leaves it exactly
        // as the automatic placement finds it.
        private static placeOnPreferredSide(
            preferredSide: string,
            isRtl: boolean,
            callout: HTMLElement,
            scrollContainer: { style: any },
            cap: (height: number) => number,
            fixedRect: { top: number, left: number, bottom: number },
            offset: number,
            componentX: number,
            componentY: number,
            componentWidth: number,
            componentHeight: number,
            calloutWidth: number,
            calloutHeight: number,
            distanceToTop: number,
            distanceToBottom: number,
            distanceToLeft: number,
            distanceToRight: number,
            visualHeight: number,
            visibleTop: number,
            visibleBottom: number,
            scrollOffset: number,
            headerHeight: number,
            footerHeight: number,
        ): BitCalloutPlacement | null {
            // The logical sides are resolved against the direction the callout is laid out in; the
            // physical ones the placement works in are what comes out.
            const side: BitCalloutPlacement | '' =
                  preferredSide === 'start' ? (isRtl ? 'right' : 'left')
                : preferredSide === 'end' ? (isRtl ? 'left' : 'right')
                : preferredSide === 'top' ? 'above'
                : preferredSide === 'bottom' ? 'below'
                : '';
            if (!side) return null;

            const opposite: Record<BitCalloutPlacement, BitCalloutPlacement> = {
                above: 'below', below: 'above', left: 'right', right: 'left'
            };

            const fits = (candidate: BitCalloutPlacement) => {
                if (candidate === 'below') return calloutHeight <= distanceToBottom;
                if (candidate === 'above') return calloutHeight <= distanceToTop;
                if (candidate === 'left') return calloutWidth <= distanceToLeft;
                return calloutWidth <= distanceToRight;
            };

            const placement = fits(side) ? side
                            : fits(opposite[side]) ? opposite[side]
                            : null;
            if (placement == null) return null;

            if (placement === 'below') {
                callout.style.top = (componentY + componentHeight + offset - fixedRect.top) + 'px';
                scrollContainer.style.maxHeight = cap(Math.max(0, distanceToBottom - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
            } else if (placement === 'above') {
                callout.style.bottom = (fixedRect.bottom - (componentY - offset)) + 'px';
                scrollContainer.style.maxHeight = cap(Math.max(0, distanceToTop - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
            } else {
                // The horizontal placement above aligned the callout with the component; a callout beside
                // it is placed against the edge it sits next to instead, and aligned with the top of the
                // component the way a submenu is, sliding along the screen only when it would hang off it.
                callout.style.left = ((placement === 'left'
                                        ? (componentX - calloutWidth - offset)
                                        : (componentX + componentWidth + offset)) - fixedRect.left) + 'px';

                const available = Math.max(0, visualHeight - 4);
                const height = Math.min(calloutHeight, available);
                let top = componentY;
                if (top + height > visibleBottom - 2) top = visibleBottom - 2 - height;
                if (top < visibleTop + 2) top = visibleTop + 2;
                callout.style.top = (top - fixedRect.top) + 'px';
                scrollContainer.style.maxHeight = cap(Math.max(0, available - scrollOffset - headerHeight - footerHeight - 10)) + 'px';
            }

            return placement;
        }

        // Places the arrow (beak) on the edge of the callout that faces the component, centred on the
        // component and kept away from the rounded corners of the callout. The arrow is a fixed-position
        // sibling of the callout rather than a child of it, so that a callout which scrolls its own
        // content never clips the half of the arrow that sticks out of it. The stylesheet draws it as a
        // square rotated onto its corner and centred on the point set here, which puts one half inside
        // the callout - where the callout, painted after it, covers it - and leaves the other half
        // showing as the beak.
        private static positionArrow(
            arrow: HTMLElement,
            callout: HTMLElement,
            placement: BitCalloutPlacement,
            fixedRect: { top: number, left: number, bottom: number },
            componentX: number,
            componentY: number,
            componentWidth: number,
            componentHeight: number,
            visibleTop: number,
            visibleLeft: number,
            visibleBottom: number,
            visibleRight: number,
        ) {
            arrow.style.display = 'block';
            arrow.setAttribute('data-bit-cal-pos', placement);

            // The callout has just been laid out, so its resolved box is what the arrow is placed against
            // rather than the inputs the placement was computed from (which do not account for clamping).
            const rect = callout.getBoundingClientRect();
            const inset = Callouts.ARROW_CORNER_INSET;

            const clamp = (value: number, min: number, max: number) => max < min ? (min + max) / 2 : Math.min(Math.max(value, min), max);

            if (placement === 'below' || placement === 'above') {
                const centerX = clamp(componentX + componentWidth / 2,
                                      Math.max(rect.left + inset, visibleLeft),
                                      Math.min(rect.right - inset, visibleRight));
                const edgeY = placement === 'below' ? rect.top : rect.bottom;
                arrow.style.left = (centerX - fixedRect.left) + 'px';
                arrow.style.top = (edgeY - fixedRect.top) + 'px';
            } else {
                const centerY = clamp(componentY + componentHeight / 2,
                                      Math.max(rect.top + inset, visibleTop),
                                      Math.min(rect.bottom - inset, visibleBottom));
                const edgeX = placement === 'right' ? rect.left : rect.right;
                arrow.style.left = (edgeX - fixedRect.left) + 'px';
                arrow.style.top = (centerY - fixedRect.top) + 'px';
            }
        }

        // Which side the callout lands on is only known after the measuring above, and by then the
        // browser has already resolved the callout to its open state. So the entry is (re)started
        // from here: the from-state class is applied with transitions suppressed, forced into effect
        // with a reflow, and then dropped, which transitions the callout to its open state - sliding
        // down out of the component when it sits below it, and up when it sits above it.
        // Components opt in by styling `.bit-cal-ent`; for the ones that don't this is a no-op.
        private static playEntryAnimation(callout: HTMLElement, placement: BitCalloutPlacement) {
            callout.setAttribute('data-bit-cal-pos', placement);
            callout.classList.add('bit-cal-ent');
            void callout.offsetHeight;
            callout.classList.remove('bit-cal-ent');
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

            // Not an entry: replaying the open animation on every viewport change would be a flicker.
            Callouts.position(component, callout, params.responsiveMode, params.dropDirection, params.isRtl,
                params.scrollContainerId, params.scrollOffset, params.headerId, params.footerId,
                params.setCalloutWidth, params.fixedCalloutWidth, params.maxWindowWidth, false, params.maxHeight,
                params.arrowId, params.gap, params.preferredSide);
        }

        // Re-applies the space the scrollable content of an open callout cannot use. The parts that sit
        // above that content can come and go while the callout stays open (a dropdown's select all row
        // disappears as soon as a search matches nothing), and the callout is otherwise only laid out
        // when it is toggled, which would leave the list measured against a header that is no longer there.
        public static updateScrollOffset(calloutId: string, scrollOffset: number) {
            const params = Callouts._currentParams;
            if (params == null) return;
            if (params.calloutId !== calloutId) return;
            if (params.scrollOffset === scrollOffset) return;

            params.scrollOffset = scrollOffset;

            Callouts.reposition();
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

        private static moveCalloutToBody(calloutId: string, callout: HTMLElement, overlayId: string, arrowId: string = '') {
            if (Callouts._calloutOriginalParents.has(calloutId)) return;
            if (callout.parentElement === document.body) return;

            const overlay = overlayId ? document.getElementById(overlayId) : null;
            const arrow = arrowId ? document.getElementById(arrowId) : null;
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

            // ForceAnimation works by re-pointing inherited custom properties on the subtree carrying
            // `bit-fam`, so relocating the callout to the body drops it back to the reduced-motion
            // durations even though its original ancestors opted in. The wrapper is `display: contents`
            // and therefore still inherits into the callout, so carrying the class over restores it.
            // The callout may also carry the class itself, for the components that render it there
            // because their root - the only element the class would otherwise land on - is not an
            // ancestor of the callout.
            if (parent?.closest('.bit-fam')) {
                wrapper.classList.add('bit-fam');
            }

            Callouts._calloutOriginalParents.set(calloutId, {
                parent: parent,
                nextSibling: nextSibling,
                overlay: overlay,
                overlayParent: overlay?.parentElement ?? null,
                overlayNextSibling: overlay?.nextSibling ?? null,
                arrow: arrow,
                arrowParent: arrow?.parentElement ?? null,
                arrowNextSibling: arrow?.nextSibling ?? null,
                wrapper: wrapper
            });

            if (overlay) {
                wrapper.appendChild(overlay);
            }
            // The arrow is appended before the callout so that the callout paints over the half of it
            // that lands inside the callout, leaving only the beak showing.
            if (arrow) {
                wrapper.appendChild(arrow);
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

            if (original.arrow && original.arrowParent) {
                if (original.arrowNextSibling && original.arrowNextSibling.parentNode === original.arrowParent) {
                    original.arrowParent.insertBefore(original.arrow, original.arrowNextSibling);
                } else {
                    original.arrowParent.appendChild(original.arrow);
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

                const arrow = current.arrowId && document.getElementById(current.arrowId);
                arrow && (arrow.style.display = 'none');

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
        arrowId?: string;
        noDismiss?: boolean;
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
        maxHeight: number;
        arrowId: string;
        gap: number;
        preferredSide: string;
    }

    // Which side of the component the callout was placed on, as a physical side, so that the stylesheets
    // that read it back off `data-bit-cal-pos` can point an arrow or an entry animation at it directly.
    type BitCalloutPlacement = 'below' | 'above' | 'left' | 'right';

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
