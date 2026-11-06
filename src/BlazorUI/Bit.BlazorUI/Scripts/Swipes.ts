namespace BitBlazorUI {
    export class Swipes {
        private static _swipes: BitSwipe[] = [];

        public static setup(
            id: string,
            trigger: number,
            position: BitSwipePosition,
            isRtl: boolean | null,
            orientationLock: BitSwipeOrientation,
            dotnetObj: DotNetObject,
            isResponsive: boolean,
            scrollContainerId: string) {

            if (isResponsive) {
                const windowWidth = window.innerWidth;
                if (windowWidth >= Utils.MAX_MOBILE_WIDTH) return;
            }

            const element = document.getElementById(id);
            if (!element) return;

            let touchOnScrollContainer = false;
            const scrollContainer = scrollContainerId ? document.getElementById(scrollContainerId) : null;

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            let originalTransform: string;
            // The transform the drag itself last left on the element, told apart from one written over it since.
            let dragTransform: string;
            let orientation = BitSwipeOrientation.None;
            // Set once the swipe is disposed: a gesture that dismissed the surface is still awaiting OnClose by then,
            // and the reference it would report its end through goes along with the swipe.
            let disposed = false;
            // Which way Start and End face. A direction the component was given is taken as it is; one it was
            // not given (or left to its content to decide) is the one the element is laid out in - inherited from the page or from whatever box
            // it sits in - read when the gesture starts, so a page whose direction changes is followed.
            let rtl = isRtl ?? false;
            // How far the surface has to be dragged is a fraction of how big it is, so the box is measured
            // when the gesture starts rather than when it is registered: a surface that is resized while it
            // is registered - a panel given a new size, a callout whose content grew - would otherwise be
            // weighed against a box it no longer has.
            let bcr = element.getBoundingClientRect();
            // Whether the gesture under way is a mouse's or a pen's rather than a finger's.
            let isPointerGesture = false;
            // The events the gesture under way is followed through: 'touch' for the touch events, or the pointerType
            // of the pointer events. A gesture is only ever followed through one of the two, since a pen raises both
            // on most touch screens and the two read different coordinates (see getX).
            let gestureSource: string | null = null;
            // The identifier of the touch a gesture followed through the touch events is read off (the first one
            // down, which getX and getY read), so a touchcancel that takes away another finger is told apart from one
            // that takes away the gesture's own.
            let touchId: number | null = null;

            // The physical edge the surface is pinned to: the logical pair is read against the direction it is
            // laid out in, the physical pair stays where it is named in both.
            const onLeftEdge = () => position === 'left' || (!rtl && position === 'start') || (rtl && position === 'end');
            const onRightEdge = () => position === 'right' || (!rtl && position === 'end') || (rtl && position === 'start');

            // A finger is followed on the screen, where the viewport a mobile browser's own bars move under it does
            // not drag the gesture along. A mouse or a pen is followed in the viewport, which nothing moves under
            // one - and whose coordinates are always there to read, where WebKit gives the mouse events it
            // synthesizes no screen position (0, 0) at all.
            const isTouch = (e: TouchEvent | PointerEvent): e is TouchEvent => 'touches' in e;
            const getX = (e: TouchEvent | PointerEvent) => isTouch(e) ? e.touches[0].screenX : e.clientX;
            const getY = (e: TouchEvent | PointerEvent) => isTouch(e) ? e.touches[0].screenY : e.clientY;

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (belongsElsewhere(e.target)) return;

                rtl = isRtl ?? Utils.isRtl(element);
                isPointerGesture = isTouch(e) === false;
                gestureSource = isTouch(e) ? 'touch' : e.pointerType;
                touchId = isTouch(e) ? e.touches[0].identifier : null;

                startX = getX(e);
                startY = getY(e);

                bcr = element.getBoundingClientRect();

                element.style.transitionDuration = '0s';
                originalTransform = element.style.transform;
                dragTransform = originalTransform;

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX === -1 || startY === -1) return;

                // A mouse dragged across text is selecting it, which is what a mouse drag inside a surface
                // nearly always means: the drag is given back to the selection before it can throw the
                // surface away with the text the user was about to copy.
                if (isPointerGesture && isSelectingText()) return abort();

                diffX = getX(e) - startX;
                diffY = getY(e) - startY;

                const absX = Math.abs(diffX);
                const absY = Math.abs(diffY);
                const thresX = absX > 5;
                const thresY = absY > 5;


                if (orientation === BitSwipeOrientation.None) {
                    if (thresX && !thresY) {
                        orientation = BitSwipeOrientation.Horizontal;
                    } else if (!thresX && thresY) {
                        orientation = BitSwipeOrientation.Vertical;
                    }
                }

                if (orientationLock === BitSwipeOrientation.Horizontal) {
                    if (orientation === BitSwipeOrientation.Horizontal) {
                        cancel();
                        diffY = 0;
                    } else {
                        diffX = 0;
                    }
                } else if (orientationLock === BitSwipeOrientation.Vertical) {
                    if (orientation === BitSwipeOrientation.Vertical) {
                        cancel();
                        diffX = 0;
                    } else {
                        diffY = 0;
                    }
                } else if ((thresX || thresY)) {
                    cancel();
                }

                if (onLeftEdge()) {
                    if (diffX < 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (onRightEdge()) {
                    if (diffX > 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === 'top') {
                    if (diffY < 0 && !canScrollAway()) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === 'bottom') {
                    if (diffY > 0 && !canScrollAway()) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                dragTransform = element.style.transform;

                await dotnetObj.invokeMethodAsync('OnMove', diffX, diffY);

                // A surface that scrolls its own content is dragged away only from the end of that content:
                // pulling a bottom sheet down while it is scrolled means scrolling it back up, and it is the
                // gesture the finger is already making. The surfaces whose content scrolls in an element of
                // their own never scroll themselves, so this leaves them where they were.
                function canScrollAway() {
                    const scrollable = element!.scrollHeight - element!.clientHeight;
                    if (scrollable <= 1) return false;

                    return position === 'bottom'
                        ? element!.scrollTop > 1
                        : element!.scrollTop < scrollable - 1;
                }

                function cancel() {
                    if (!e.cancelable) return;

                    if (touchOnScrollContainer) {
                        const [isScrollAtLeft, isScrollAtRight] = calcScrolls();

                        if (diffX < 0 && (rtl ? isScrollAtRight : isScrollAtLeft)) return;
                        if (diffX > 0 && (rtl ? isScrollAtLeft : isScrollAtRight)) return;
                    }

                    // Only a finger's move has a default the swipe takes over - the scroll it would make. A mouse's is
                    // the selection its drag extends, which the swipe gives the drag back to (see isSelectingText)
                    // rather than prevents: WebKit stops extending a selection under a move that is prevented, and
                    // the swipe would then throw the surface away with the text being selected.
                    if (isTouch(e)) e.preventDefault();
                    e.stopPropagation();
                }
            };

            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                touchOnScrollContainer = false;

                if (startX === -1 || startY === -1) return;

                startX = startY = -1;
                gestureSource = null;
                element.style.transitionDuration = '';
                try {
                    if (onLeftEdge() && diffX < 0) {
                        if ((Math.abs(diffX) / bcr.width) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (onRightEdge() && diffX > 0) {
                        if ((diffX / bcr.width) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    // The two vertical edges weigh the drag against the same content scrolling the drag
                    // itself was checked against, so a gesture that only scrolled the surface never ends by
                    // throwing it away.
                    const scrollable = element.scrollHeight - element.clientHeight;
                    const scrolled = scrollable > 1 && (position === 'bottom'
                        ? element.scrollTop > 1
                        : element.scrollTop < scrollable - 1);

                    if (position === 'top' && diffY < 0 && !scrolled) {
                        if ((Math.abs(diffY) / bcr.height) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (position === 'bottom' && diffY > 0 && !scrolled) {
                        if ((diffY / bcr.height) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }
                } finally {
                    // The transform the drag wrote onto the element is taken off again however the gesture
                    // ended, the dismissal included: an inline transform left behind outlives the gesture and
                    // overrides whatever the stylesheet has to say about where the surface sits, so the next
                    // time it is shown it would come back offset by however far the last drag got. Only the drag's
                    // own is taken off, though: a dismissal that has written a transform of its own over it (a
                    // responsive callout sliding off the screen) keeps it, since putting back the one the drag
                    // started from would leave the closed surface invisible but in place, over the page it covers.
                    if (element.style.transform === dragTransform) {
                        element.style.transform = originalTransform;
                    }

                    if (!disposed) await dotnetObj.invokeMethodAsync('OnEnd', diffX, diffY);
                    diffX = diffY = 0;
                    orientation = BitSwipeOrientation.None;
                }
            };

            // Gives up a drag that has started without ending it: the surface goes back to where it was and
            // the consumer hears the gesture end where it began.
            const abort = () => {
                startX = startY = -1;
                gestureSource = null;
                diffX = diffY = 0;
                orientation = BitSwipeOrientation.None;
                element.style.transitionDuration = '';
                element.style.transform = originalTransform;

                dotnetObj.invokeMethodAsync('OnEnd', 0, 0);
            };

            // A drag that starts on something that takes the pointer for itself is that thing's to handle:
            // a field the caret is moved or the text selected in, a slider, an editable region, a region
            // marked data-no-swipe (a canvas, a table that scrolls sideways) - and a surface nested inside
            // this one, which handles its own swipe and must not drag the one it was opened from along.
            const belongsElsewhere = (target: EventTarget | null) => {
                if (!(target instanceof Element)) return false;

                if (target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"]), [data-no-swipe]')) return true;

                return Swipes._swipes.some(s => s.element !== element && element.contains(s.element) && s.element.contains(target));
            };

            const isSelectingText = () => {
                const selection = window.getSelection();
                if (!selection || selection.isCollapsed || !selection.anchorNode) return false;

                return element.contains(selection.anchorNode) && selection.toString().length > 0;
            };

            // A finger is heard through the touch events and a mouse or a pen through the pointer events, both of them
            // on every device: a touch screen laptop, a tablet with a mouse, a phone with a stylus each have the two,
            // and listening for only the one the device was guessed to have left the other dead there. A finger
            // raises pointer events as well, which are left to the touch events it also raises.
            const onScrollContainerTouchStart = (e: TouchEvent) => {
                touchOnScrollContainer = true;

                // This runs before the surface's own touchstart, so the direction is read here as well.
                rtl = isRtl ?? Utils.isRtl(element);

                // The two flags are the start and the end of the scroll (scrollLeft is 0 at the start in
                // both directions), while the gesture that dismisses the surface runs toward the physical
                // edge it is pinned to. A drag toward the right asks the content to scroll to its left,
                // which is the start left to right and the end right to left, so it is the surface's to
                // take once the content has no further to go that way; a drag toward the left mirrors it.
                const [isScrollAtLeft, isScrollAtRight] = calcScrolls();

                if (onRightEdge() && (rtl ? isScrollAtRight : isScrollAtLeft)) return;
                if (onLeftEdge() && (rtl ? isScrollAtLeft : isScrollAtRight)) return;

                e.stopPropagation();
            };
            // A pen raises both on most touch screens - its pointer events first, then the touch events of the same
            // contact - so its touches take over the gesture its pointerdown has just started, read again in the
            // coordinates the touch events are followed in, and its pointer events are left alone from there on: its
            // pointer events stop the moment the browser pans under it, where its touch events go on. A pen that raises
            // no touch events (Windows) is followed through its pointer events from start to end.
            const onTouchStart = (e: TouchEvent) => {
                if (gestureSource === 'pen' && startX !== -1 && startY !== -1) {
                    gestureSource = 'touch';
                    touchId = e.touches[0].identifier;
                    isPointerGesture = false;
                    startX = getX(e);
                    startY = getY(e);
                    return;
                }

                void onStart(e);
            };
            const ownsTouches = () => gestureSource === null || gestureSource === 'touch';
            const onTouchMove = (e: TouchEvent) => { if (ownsTouches()) void onMove(e); };
            const onTouchEnd = (e: TouchEvent) => { if (ownsTouches()) void onEnd(e); };
            const ownsPointer = (e: PointerEvent) => e.pointerType !== 'touch' && e.pointerType === gestureSource;
            const onPointerStart = (e: PointerEvent) => {
                if (e.pointerType === 'touch' || (gestureSource === 'touch' && startX !== -1)) return;

                void onStart(e);
            };
            const onPointerMove = (e: PointerEvent) => { if (ownsPointer(e)) void onMove(e); };
            const onPointerEnd = (e: PointerEvent) => { if (ownsPointer(e)) void onEnd(e); };
            // A gesture the browser takes away - a system gesture, an alert, the contact of a pen it starts panning
            // under - ends in a cancel rather than an end, and is given up rather than weighed: nothing the gesture
            // did by then was a decision to dismiss the surface. Left unfinished, it would keep the drag's transform
            // on the element, which the next gesture would then record as where the surface sits, and it would keep
            // the pointer events of a mouse turned away as if a finger were still down. The pointerleave that follows
            // a pointercancel then finds no gesture left to end. A touchcancel names the contacts it takes away, and one
            // that leaves the gesture's own touch down (another finger on the surface) leaves the gesture going.
            const onTouchCancel = (e: TouchEvent) => {
                const following = ownsTouches() && startX !== -1;

                if (following && touchId !== null && Array.from(e.changedTouches).every(t => t.identifier !== touchId)) return;

                touchOnScrollContainer = false;

                if (following) abort();
            };
            const onPointerCancel = (e: PointerEvent) => { if (ownsPointer(e) && startX !== -1) abort(); };

            scrollContainer?.addEventListener('touchstart', onScrollContainerTouchStart);
            element.addEventListener('touchstart', onTouchStart);
            element.addEventListener('touchmove', onTouchMove);
            element.addEventListener('touchend', onTouchEnd);
            element.addEventListener('touchcancel', onTouchCancel);
            element.addEventListener('pointerdown', onPointerStart);
            element.addEventListener('pointermove', onPointerMove);
            element.addEventListener('pointerup', onPointerEnd);
            element.addEventListener('pointerleave', onPointerEnd, false);
            element.addEventListener('pointercancel', onPointerCancel);

            const swipe = new BitSwipe(id, element, trigger, dotnetObj);
            swipe.setDisposer(() => {
                disposed = true;
                scrollContainer?.removeEventListener('touchstart', onScrollContainerTouchStart);
                element.removeEventListener('touchstart', onTouchStart);
                element.removeEventListener('touchmove', onTouchMove);
                element.removeEventListener('touchend', onTouchEnd);
                element.removeEventListener('touchcancel', onTouchCancel);
                element.removeEventListener('pointerdown', onPointerStart);
                element.removeEventListener('pointermove', onPointerMove);
                element.removeEventListener('pointerup', onPointerEnd);
                element.removeEventListener('pointerleave', onPointerEnd, false);
                element.removeEventListener('pointercancel', onPointerCancel);
            });
            Swipes._swipes.push(swipe);

            const calcScrolls = () => {
                const isScrollAtLeft = Math.abs(scrollContainer!.scrollLeft) <= 2;
                const isScrollAtRight = Math.abs(scrollContainer!.scrollLeft) + scrollContainer!.clientWidth >= (scrollContainer!.scrollWidth - 2);

                return [isScrollAtLeft, isScrollAtRight];
            }
        }

        public static dispose(id: string) {
            const swipe = Swipes._swipes.find(r => r.id === id);
            if (!swipe) return;

            Swipes._swipes = Swipes._swipes.filter(r => r.id !== id);
            swipe.dispose();
        }
    }

    class BitSwipe {
        id: string;
        element: HTMLElement;
        trigger: number;
        dotnetObj: DotNetObject | undefined;
        disposer: () => void = () => { };

        constructor(id: string, element: HTMLElement, trigger: number, dotnetObj: DotNetObject) {
            this.id = id;
            this.element = element;
            this.trigger = trigger;
            this.dotnetObj = dotnetObj;
        }

        public setDisposer(disposer: () => void) {
            this.disposer = disposer;
        }

        public dispose() {
            this.disposer();
            this.dotnetObj?.dispose();
            this.dotnetObj = undefined;
        }
    }

    // The edge a swipeable surface is pinned to, handed over by name (SwipesJsRuntimeExtensions.BitSwipesSetup)
    // rather than as the ordinal of the C# BitPlacement, so the order of that library-wide enum is no contract
    // with this file. The placements a swipe can never be set up for - Center and the two combined values - have
    // no name here: the C# side resolves its placement to one of these six first (ToPanelSide).
    type BitSwipePosition = 'top' | 'bottom' | 'start' | 'end' | 'left' | 'right';

    enum BitSwipeOrientation {
        None,
        Horizontal,
        Vertical
    }
}

// bit-lazy: teardown Swipes.dispose
