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
            keyboardTrigger: boolean,
            dotnetObj: DotNetObject) {

            // A setup for an id that is still registered would leave the previous listeners attached and
            // its .NET reference undisposed, so the old trap is torn down before the new one takes over.
            SwipeTrap.dispose(id);

            let diffX = 0;
            let diffY = 0;
            let startX = 0;
            let startY = 0;
            let startTime = 0;
            let touchId = -1;
            let pointerId = -1;
            // A gesture is identified by a flag rather than by a sentinel coordinate: a trap scrolled off
            // the left edge of the viewport reports negative client coordinates that a sentinel would eat.
            let active = false;
            let trapped = false;
            let activeTouch = false;
            let suppressNextClick = false;
            let pointerType = '';
            // Where the press landed: a touch keeps being delivered to it even once it has left the page, so a
            // gesture that started on a node that is gone will never see its end reach the trap.
            let startTarget: EventTarget | null = null;
            let samples: { t: number, x: number, y: number }[] = [];
            let orientation = BitSwipeOrientation.None;
            // How far the surface has to be dragged is a fraction of how big it is, so the box is measured
            // when the gesture starts rather than when it is registered: an element that is resized while it
            // is registered would otherwise be weighed against a box it no longer has.
            let bcr = element.getBoundingClientRect();
            const hasTouch = Utils.isTouchDevice();

            // The calls into .NET that nothing awaits must not surface as unhandled rejections: a key or a move
            // that arrives while the circuit is down, or after a re-setup has disposed this reference, is dropped.
            const invoke = (method: string, ...args: any[]) => {
                dotnetObj.invokeMethodAsync(method, ...args).catch(() => { });
            };

            // OnMove is throttled on both edges: the first move of a window goes at once and the latest one is held
            // for the end of the window, so a pointer that comes to rest is reported where it rests rather than where
            // the last window began. The held move belongs to its gesture alone - reset() drops it, since OnEnd
            // carries the final position anyway - and a new gesture starts with a fresh window.
            const throttledMove = Utils.throttle((...args: any[]) => invoke('OnMove', ...args), throttle, { trailing: true });

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

            // How far a finger slides during what its owner meant as a tap. Below it a press is still a
            // press: the gesture is watched and the default declined, but the click it produces is left alone.
            const TAP_SLOP = 3; // px

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

            // Everything a gesture accumulates is cleared in one place, so an end, a cancel and a dispose
            // cannot each forget a different part of it.
            const reset = () => {
                active = false;
                trapped = false;
                startX = startY = startTime = 0;
                touchId = pointerId = -1;
                diffX = diffY = 0;
                pointerType = '';
                startTarget = null;
                samples = [];
                orientation = BitSwipeOrientation.None;
                throttledMove.cancel();
                element.classList.remove('bit-stp-swp');
                window.removeEventListener('keydown', onEscape, true);
            };

            // A touch gesture whose end can no longer reach the trap: the node under the finger was removed from the
            // page, so its touchend went with it. Such a gesture holds nothing any more - the keys it would take are
            // given back, and it is called off the moment anything asks about it.
            const isStale = () => active && activeTouch && startTarget instanceof Node && startTarget !== element && !startTarget.isConnected;

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (active) {
                    // A second finger must not restart an in-progress gesture. But a gesture whose end never reached
                    // the trap - the element under the finger was removed from the page, so its touchend went with it
                    // - would hold the trap forever, so a press that proves it over calls it off instead: the tracked
                    // pointer pressed again (a mouse is not pressed twice without a release in between), or a touch
                    // list the tracked finger is no longer in.
                    const stale = isTouchEvent(e)
                        ? activeTouch && !Array.prototype.some.call(e.touches, (t: Touch) => t.identifier === touchId)
                        : !activeTouch && e.pointerId === pointerId;
                    if (!stale) return;

                    cancelGesture(e).catch(() => { });
                }

                // A gesture that was trapped arms a click suppressor; a new press means the click it was
                // waiting for never came, and the press's own click must not be the one that is swallowed.
                suppressNextClick = false;

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
                startTime = e.timeStamp;
                startTarget = e.target;
                active = true;

                bcr = element.getBoundingClientRect();

                samples = [{ t: startTime, x: startX, y: startY }];

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY, pointerType);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (!active) return;
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


                // Which axis the gesture moves along is what an Auto lock is resolved from; a fixed lock
                // was told its axis and never asks.
                if (orientation === BitSwipeOrientation.None) {
                    if (thresX && !thresY) {
                        orientation = BitSwipeOrientation.Horizontal;
                    } else if (!thresX && thresY) {
                        orientation = BitSwipeOrientation.Vertical;
                    } else if (thresX && thresY) {
                        // A diagonal move crosses both axes at once (with the default threshold of 0, most
                        // do), and an axis left unpicked here is never picked for the rest of the gesture:
                        // the one moved furthest along wins, and a dead heat goes to the horizontal one.
                        orientation = absX >= absY ? BitSwipeOrientation.Horizontal : BitSwipeOrientation.Vertical;
                    }
                }

                // A fixed lock is declared, not discovered: its axis is the trap's for the whole gesture and
                // the free one is the browser's for the whole gesture, however the gesture started. So the
                // locked axis is taken the moment it moves past the threshold - a swipe that begins as a
                // scroll along the free axis and turns onto the locked one is still the trap's - and the free
                // axis reads zero throughout. Deciding either of those from the axis the gesture picked first
                // is what Auto does, and it is what left a fixed lock behaving like Auto.
                if (orientationLock === BitSwipeOrientation.Horizontal) {
                    diffY = 0;
                    if (thresX) {
                        cancel();
                    }
                } else if (orientationLock === BitSwipeOrientation.Vertical) {
                    diffX = 0;
                    if (thresY) {
                        cancel();
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
                throttledMove(startX, startY, diffX, diffY, velocityX, velocityY, pointerType, e.timeStamp - startTime);

                function cancel() {
                    if (e.cancelable) {
                        e.preventDefault();
                        e.stopPropagation();
                    }

                    // The moment the movement is trapped it is a swipe: selecting text along the way is
                    // the one default the events cannot prevent, so it is turned off by a class for the
                    // rest of the gesture - which also marks the trap as actively swiping for styling.
                    element.classList.add('bit-stp-swp');

                    // A default threshold of zero makes the wobble of a tap enough to reach here, and a tap
                    // is not a swipe: what follows only applies once the movement is past the tap slop.
                    if (Math.abs(diffX) <= TAP_SLOP && Math.abs(diffY) <= TAP_SLOP) return;

                    if (!trapped) {
                        // Escape puts a swipe back the way it puts back a native drag-and-drop. The key is listened for
                        // on the window, since the focus is wherever it was before the press, and only once the gesture
                        // is a swipe: a press that has not moved is not one, and its Escape is the page's.
                        window.addEventListener('keydown', onEscape, true);
                    }

                    trapped = true;

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
                if (!active) return;
                if (isTouchEvent(e) !== activeTouch) return; // the other input's echo of the tracked gesture
                if (isTouchEvent(e)) {
                    if (!getTouch(e)) return; // another finger lifted, not the tracked one
                } else if (e.pointerId !== pointerId) return;
                const sX = startX;
                const sY = startY;
                const dX = diffX;
                const dY = diffY;
                const pT = pointerType;
                const dur = e.timeStamp - startTime;
                const [velocityX, velocityY] = getVelocities(e.timeStamp);

                // A release that ends a trapped swipe is not a click, however the click that follows it is
                // retargeted: the one it would produce is swallowed rather than delivered as a stray tap.
                suppressNextClick = trapped;

                reset();

                try {
                    // A locked axis is the only one that may trigger: the free axis kept its default
                    // behavior, so its movement is a scroll the trap watched, not a swipe it took.
                    const trigX = orientationLock !== BitSwipeOrientation.Vertical;
                    const trigY = orientationLock !== BitSwipeOrientation.Horizontal;

                    // A fractional trigger weighs each axis against its own dimension of the box. A box with
                    // no size on an axis cannot be crossed by a fraction of it, so it falls back to pixels.
                    const fractional = Math.abs(trigger) < 1;
                    const divX = fractional ? (bcr.width || 1) : 1;
                    const divY = fractional ? (bcr.height || 1) : 1;
                    const compX = trigX ? Math.abs(dX) / divX : 0;
                    const compY = trigY ? Math.abs(dY) / divY : 0;

                    // A flick is a release faster than triggerVelocity (px/ms) on an axis the gesture
                    // actually moved along: it triggers even when the distance never reached the trigger point.
                    const flickX = trigX && triggerVelocity > 0 && Math.abs(velocityX) > triggerVelocity && Math.abs(dX) > threshold;
                    const flickY = trigY && triggerVelocity > 0 && Math.abs(velocityY) > triggerVelocity && Math.abs(dY) > threshold;

                    if (compX > Math.abs(trigger) || compY > Math.abs(trigger) || flickX || flickY) {
                        return await dotnetObj.invokeMethodAsync('OnTrigger', dX, dY, velocityX, velocityY, pT, dur);
                    }
                } finally {
                    await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, dX, dY, velocityX, velocityY, pT, false, dur);
                }
            };

            const onCancel = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (!active) return;
                if (isTouchEvent(e) !== activeTouch) return; // the other input's echo of the tracked gesture
                if (isTouchEvent(e)) {
                    if (!getTouch(e)) return; // another finger was canceled, not the tracked one
                } else if ((e as PointerEvent).pointerId !== pointerId) return;

                await cancelGesture(e);
            };

            // A gesture that is called off rather than released: the browser took it over, the pointer left the box
            // before it was trapped, or Escape put it back. Nothing triggers, and OnEnd reports it as canceled.
            const cancelGesture = async (e: Event): Promise<void> => {
                const sX = startX;
                const sY = startY;
                const dX = diffX;
                const dY = diffY;
                const pT = pointerType;
                const dur = e.timeStamp - startTime;

                suppressNextClick = trapped;

                reset();

                await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, dX, dY, 0, 0, pT, true, dur);
            };

            // The press is still down after an Escape, so the release and the click that follow it would land as a
            // gesture's - the release finds no gesture any more, and the click of a trapped swipe is swallowed as ever.
            // The key goes no further: it was the swipe's, not the dialog's or the overlay's the trap may sit in.
            const onEscape = async (e: KeyboardEvent): Promise<void> => {
                if (e.key !== 'Escape' || !trapped) return;

                // A gesture that can no longer end is called off, but the key is not its to take: it goes on to
                // the dialog or the overlay it was meant for.
                if (isStale()) {
                    await cancelGesture(e);
                    return;
                }

                e.preventDefault();
                e.stopPropagation();

                await cancelGesture(e);
            };

            // The keyboard's alternative to the swipe: an arrow key pressed on the trap itself triggers in its own
            // direction. Only the trap's own keys are taken - a key pressed on a descendant is the descendant's, a
            // modified one is the browser's, a held one is one swipe rather than a stream of them - and only along
            // an axis a lock leaves to the trap. The direction crosses to the C# side by name, which reads it into
            // a BitPlacement, so the order of that library-wide enum is no contract with this file.
            const onKeyDown = (e: KeyboardEvent) => {
                if (isStale()) cancelGesture(e).catch(() => { });
                if (!keyboardTrigger || active) return;
                if (e.target !== element) return;
                if (e.defaultPrevented || e.repeat || e.altKey || e.ctrlKey || e.metaKey || e.shiftKey) return;
                if (element.classList.contains('bit-dis')) return;

                const horizontal = orientationLock !== BitSwipeOrientation.Vertical;
                const vertical = orientationLock !== BitSwipeOrientation.Horizontal;

                let direction: 'top' | 'bottom' | 'left' | 'right' | null = null;
                if (horizontal && e.key === 'ArrowRight') direction = 'right';
                else if (horizontal && e.key === 'ArrowLeft') direction = 'left';
                else if (vertical && e.key === 'ArrowUp') direction = 'top';
                else if (vertical && e.key === 'ArrowDown') direction = 'bottom';
                if (direction === null) return;

                // The arrow keys scroll the page by default, which is not what a key the trap answers to should do.
                e.preventDefault();

                invoke('OnKeyTrigger', direction);
            };

            const onLeave = async (e: PointerEvent): Promise<void> => {
                // Before the pointer is captured, leaving the element's box abandons the gesture; once it
                // is captured (the swipe is trapped) the pointer may roam and the gesture ends on pointerup.
                if (e.pointerId !== pointerId) return;
                if (element.hasPointerCapture?.(e.pointerId)) return;

                await onCancel(e);
            };

            // A capture taken away while its gesture is still on - the trap was hidden, or a script released it - is
            // followed by no release the trap would see, so the gesture is called off here. The capture a release ends
            // is lost after that release, when there is no gesture left to call off.
            // The event bubbles, so only the trap's own capture counts: a descendant's - the one a pen gets on the
            // child it pressed, handed over the moment the trap captures the pointer, or one a nested slider takes
            // and releases - is lost while the gesture goes on.
            const onLostCapture = async (e: PointerEvent): Promise<void> => {
                if (e.target !== element) return;
                if (!active || activeTouch || e.pointerId !== pointerId) return;

                await cancelGesture(e);
            };

            // The browser's own drag-and-drop takes the gesture over when it starts on an image, a link or
            // selected text, and the pointer stream stops mid-swipe - so while a gesture is being tracked
            // the native drag is declined.
            const onDragStart = (e: Event) => {
                if (!active) return;
                e.preventDefault();
            };

            const onClick = (e: MouseEvent) => {
                if (!suppressNextClick) return;
                if (e.detail === 0) return; // a keyboard-activated click is nobody's stray tap
                suppressNextClick = false;
                e.preventDefault();
                e.stopPropagation();
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
            element.addEventListener('lostpointercapture', onLostCapture);
            element.addEventListener('dragstart', onDragStart);
            element.addEventListener('keydown', onKeyDown);
            // The click is swallowed in the capture phase so it never reaches the child it was aimed at.
            element.addEventListener('click', onClick, true);

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
                element.removeEventListener('lostpointercapture', onLostCapture);
                element.removeEventListener('dragstart', onDragStart);
                element.removeEventListener('keydown', onKeyDown);
                element.removeEventListener('click', onClick, true);

                reset(); // a dispose mid-gesture must not leave the swiping class behind
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
