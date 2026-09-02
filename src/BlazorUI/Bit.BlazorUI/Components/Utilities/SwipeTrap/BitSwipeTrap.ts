namespace BitBlazorUI {
    export class SwipeTrap {
        private static _swipeTraps: BitSwipeTrap[] = [];

        public static setup(
            id: string,
            element: HTMLElement,
            trigger: number,
            triggerVelocity: number,
            threshold: number,
            throttle: number,
            orientationLock: BitSwipeOrientation,
            touchOnly: boolean,
            skipSelector: string | null,
            dotnetObj: DotNetObject) {

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            let touchId = -1;
            let pointerId = -1;
            let activeTouch = false;
            let pointerType = '';
            let samples: { t: number, x: number, y: number }[] = [];
            let orientation = BitSwipeOrientation.None;
            // How far the surface has to be dragged is a fraction of how big it is, so the box is measured
            // when the gesture starts rather than when it is registered: an element that is resized while it
            // is registered would otherwise be weighed against a box it no longer has.
            let bcr = element.getBoundingClientRect();
            const hasTouch = Utils.isTouchDevice();
            const throttledMove = Utils.throttle((sx: number, sy: number, dx: number, dy: number, vx: number, vy: number, pt: string) => dotnetObj.invokeMethodAsync('OnMove', sx, sy, dx, dy, vx, vy, pt), throttle);

            const isTouchEvent = (e: TouchEvent | PointerEvent): e is TouchEvent => 'changedTouches' in e;

            const getTouch = (e: TouchEvent) => {
                for (let i = 0; i < e.changedTouches.length; i++) {
                    if (e.changedTouches[i].identifier === touchId) return e.changedTouches[i];
                }
                return undefined;
            };
            const getX = (e: TouchEvent | PointerEvent) => isTouchEvent(e) ? (getTouch(e)?.clientX ?? NaN) : e.clientX;
            const getY = (e: TouchEvent | PointerEvent) => isTouchEvent(e) ? (getTouch(e)?.clientY ?? NaN) : e.clientY;

            // The velocity is measured over the recent samples only, not the whole gesture: a swipe that
            // rests and then flicks would otherwise be averaged down to a slow drag. Averaging a window of
            // samples rather than dividing the last two also keeps the touch jitter out of the number.
            const VELOCITY_WINDOW = 100; // ms

            const pushSample = (t: number, x: number, y: number) => {
                samples.push({ t, x, y });
                while (samples.length > 2 && samples[0].t < t - VELOCITY_WINDOW) {
                    samples.shift();
                }
            };

            const getVelocities = (now: number) => {
                if (samples.length < 2) return [0, 0];

                const last = samples[samples.length - 1];
                // A pointer that rested before the release has gone quiet: stale samples describe the
                // movement before the rest, not the release, so they must not count as a flick.
                if (now - last.t > VELOCITY_WINDOW) return [0, 0];

                const first = samples[0];
                const dt = last.t - first.t;
                if (dt <= 0) return [0, 0];
                return [(last.x - first.x) / dt, (last.y - first.y) / dt];
            };

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX !== -1 || startY !== -1) return; // a second finger must not restart an in-progress gesture
                if (element.classList.contains('bit-dis')) return;

                // A gesture that starts on an opted-out descendant (an input, a nested slider) is the
                // descendant's, not the trap's.
                if (skipSelector) {
                    try {
                        const skipped = (e.target as Element | null)?.closest?.(skipSelector);
                        if (skipped && element.contains(skipped)) return;
                    } catch { } // an invalid selector must not break the gesture
                }

                if (isTouchEvent(e)) {
                    touchId = e.changedTouches[0].identifier;
                    activeTouch = true;
                    pointerType = 'touch';
                } else {
                    // On a touch device the touches arrive through the touch listeners; the pointer
                    // listeners are there for the mouse and the pen, so a touch's pointer echo is skipped.
                    if (hasTouch && e.pointerType === 'touch') return;
                    if (touchOnly && e.pointerType === 'mouse') return;
                    if (e.button !== 0 || e.isPrimary === false) return;
                    pointerId = e.pointerId;
                    activeTouch = false;
                    pointerType = e.pointerType;
                }

                startX = getX(e);
                startY = getY(e);

                bcr = element.getBoundingClientRect();

                samples = [{ t: e.timeStamp, x: startX, y: startY }];

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY, pointerType);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX === -1 || startY === -1) return;
                if (isTouchEvent(e) !== activeTouch) return; // the other input's echo of the tracked gesture
                if (!isTouchEvent(e) && e.pointerId !== pointerId) return;

                const x = getX(e);
                const y = getY(e);
                if (isNaN(x) || isNaN(y)) return; // a move of another finger, not the tracked one

                diffX = x - startX;
                diffY = y - startY;

                pushSample(e.timeStamp, x, y);

                const absX = Math.abs(diffX);
                const absY = Math.abs(diffY);
                const thresX = absX > threshold;
                const thresY = absY > threshold;


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
                } else if (orientationLock === BitSwipeOrientation.Auto) {
                    // Auto locks to whichever axis the gesture picks first: that axis is trapped and
                    // the other one reports zero for the rest of the gesture.
                    if (orientation === BitSwipeOrientation.Horizontal) {
                        cancel();
                        diffY = 0;
                    } else if (orientation === BitSwipeOrientation.Vertical) {
                        cancel();
                        diffX = 0;
                    }
                } else if ((thresX || thresY)) {
                    cancel();
                }

                const [velocityX, velocityY] = getVelocities(e.timeStamp);
                throttledMove(startX, startY, diffX, diffY, velocityX, velocityY, pointerType);

                function cancel() {
                    if (e.cancelable) {
                        e.preventDefault();
                        e.stopPropagation();
                    }

                    // The moment the movement is trapped it is a swipe: selecting text along the way is
                    // the one default the events cannot prevent, so it is turned off by a class for the
                    // rest of the gesture - which also marks the trap as actively swiping for styling.
                    element.classList.add('bit-stp-swp');

                    // Once the movement is far enough to be trapped it is a swipe, not a click, so the
                    // pointer is captured: the gesture then survives leaving the element's box, and the
                    // click that would otherwise land on a child at release is retargeted away from it.
                    // Capturing on pointerdown instead would steal every click inside the trap.
                    if (!isTouchEvent(e) && !element.hasPointerCapture?.((e as PointerEvent).pointerId)) {
                        try { element.setPointerCapture((e as PointerEvent).pointerId); } catch { }
                    }
                }
            };

            // The gesture's state is captured and reset before anything is awaited: a new gesture that
            // starts while the .NET callbacks are in flight must not have its state clobbered, nor leak
            // its own diffs into the callbacks of the gesture that just ended.
            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX == -1 || startY == -1) return;
                if (isTouchEvent(e) !== activeTouch) return; // the other input's echo of the tracked gesture
                if (isTouchEvent(e)) {
                    if (!getTouch(e)) return; // another finger lifted, not the tracked one
                } else if (e.pointerId !== pointerId) return;
                const sX = startX;
                const sY = startY;
                const dX = diffX;
                const dY = diffY;
                const pT = pointerType;
                const [velocityX, velocityY] = getVelocities(e.timeStamp);

                startX = startY = -1;
                touchId = pointerId = -1;
                diffX = diffY = 0;
                pointerType = '';
                samples = [];
                orientation = BitSwipeOrientation.None;
                element.classList.remove('bit-stp-swp');

                try {
                    // A locked axis is the only one that may trigger: the free axis kept its default
                    // behavior, so its movement is a scroll the trap watched, not a swipe it took.
                    const trigX = orientationLock !== BitSwipeOrientation.Vertical;
                    const trigY = orientationLock !== BitSwipeOrientation.Horizontal;

                    // A fractional trigger weighs each axis against its own dimension of the box.
                    const divX = ((Math.abs(trigger) < 1) ? bcr.width : 1);
                    const divY = ((Math.abs(trigger) < 1) ? bcr.height : 1);
                    const compX = trigX ? Math.abs(dX) / divX : 0;
                    const compY = trigY ? Math.abs(dY) / divY : 0;

                    // A flick is a release faster than triggerVelocity (px/ms) on an axis the gesture
                    // actually moved along: it triggers even when the distance never reached the trigger point.
                    const flickX = trigX && triggerVelocity > 0 && Math.abs(velocityX) > triggerVelocity && Math.abs(dX) > threshold;
                    const flickY = trigY && triggerVelocity > 0 && Math.abs(velocityY) > triggerVelocity && Math.abs(dY) > threshold;

                    if (compX > Math.abs(trigger) || compY > Math.abs(trigger) || flickX || flickY) {
                        return await dotnetObj.invokeMethodAsync('OnTrigger', dX, dY, velocityX, velocityY, pT);
                    }
                } finally {
                    await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, dX, dY, velocityX, velocityY, pT, false);
                }
            };

            const onCancel = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX == -1 || startY == -1) return;
                if (isTouchEvent(e) !== activeTouch) return; // the other input's echo of the tracked gesture
                if (!isTouchEvent(e) && (e as PointerEvent).pointerId !== pointerId) return;
                const sX = startX;
                const sY = startY;
                const dX = diffX;
                const dY = diffY;
                const pT = pointerType;

                startX = startY = -1;
                touchId = pointerId = -1;
                diffX = diffY = 0;
                pointerType = '';
                samples = [];
                orientation = BitSwipeOrientation.None;
                element.classList.remove('bit-stp-swp');

                await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, dX, dY, 0, 0, pT, true);
            };

            const onLeave = async (e: PointerEvent): Promise<void> => {
                // Before the pointer is captured, leaving the element's box abandons the gesture; once it
                // is captured (the swipe is trapped) the pointer may roam and the gesture ends on pointerup.
                if (e.pointerId !== pointerId) return;
                if (element.hasPointerCapture?.(e.pointerId)) return;

                await onCancel(e);
            };

            if (hasTouch) {
                // touchmove is registered non-passive explicitly: trapping the swipe means calling
                // preventDefault on it, which a passive listener is not allowed to do.
                element.addEventListener('touchstart', onStart, { passive: true });
                element.addEventListener('touchmove', onMove, { passive: false });
                element.addEventListener('touchend', onEnd);
                element.addEventListener('touchcancel', onCancel);
            }

            // The pointer listeners are always on: a hybrid device (a laptop with a touchscreen) reports
            // as a touch device, and without them its mouse and pen could not swipe at all.
            element.addEventListener('pointerdown', onStart);
            element.addEventListener('pointermove', onMove);
            element.addEventListener('pointerup', onEnd);
            element.addEventListener('pointercancel', onCancel);
            element.addEventListener('pointerleave', onLeave);

            const swipeTrap = new BitSwipeTrap(id, element, trigger, dotnetObj);

            swipeTrap.setRemoveHandlersFn(() => {
                if (hasTouch) {
                    element.removeEventListener('touchstart', onStart);
                    element.removeEventListener('touchmove', onMove);
                    element.removeEventListener('touchend', onEnd);
                    element.removeEventListener('touchcancel', onCancel);
                }

                element.removeEventListener('pointerdown', onStart);
                element.removeEventListener('pointermove', onMove);
                element.removeEventListener('pointerup', onEnd);
                element.removeEventListener('pointercancel', onCancel);
                element.removeEventListener('pointerleave', onLeave);

                element.classList.remove('bit-stp-swp'); // a dispose mid-gesture must not leave the class behind
            });
            SwipeTrap._swipeTraps.push(swipeTrap);
        }

        public static dispose(id: string) {
            const swipeTrap = SwipeTrap._swipeTraps.find(r => r.id === id);
            if (!swipeTrap) return;

            SwipeTrap._swipeTraps = SwipeTrap._swipeTraps.filter(r => r.id !== id);
            swipeTrap.dispose();
        }
    }

    class BitSwipeTrap {
        id: string;
        element: HTMLElement;
        trigger: number;
        dotnetObj: DotNetObject;
        removeHandlers: () => void = () => { };

        constructor(id: string, element: HTMLElement, trigger: number, dotnetObj: DotNetObject) {
            this.id = id;
            this.element = element;
            this.trigger = trigger;
            this.dotnetObj = dotnetObj;
        }
        public setRemoveHandlersFn(removeHandlersFn: () => void) {
            this.removeHandlers = removeHandlersFn;
        }

        public dispose() {
            this.removeHandlers();
            this.dotnetObj?.dispose();
        }
    }

    enum BitSwipeOrientation {
        None,
        Horizontal,
        Vertical,
        Auto
    }

}
