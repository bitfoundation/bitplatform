namespace BitBlazorUI {
    export class SwipeTrap {
        private static _swipeTraps: BitSwipeTrap[] = [];

        public static setup(
            id: string,
            element: HTMLElement,
            trigger: number,
            threshold: number,
            throttle: number,
            orientationLock: BitSwipeOrientation,
            dotnetObj: DotNetObject) {
            const bcr = element.getBoundingClientRect();

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            let orientation = BitSwipeOrientation.None;
            const isTouchDevice = Utils.isTouchDevice();
            const throttledMove = Utils.throttle((sx: number, sy: number, dx: number, dy: number) => dotnetObj.invokeMethodAsync('OnMove', sx, sy, dx, dy), throttle);

            const getX = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenX : (e as PointerEvent).screenX;
            const getY = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenY : (e as PointerEvent).screenY;

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                startX = getX(e);
                startY = getY(e);

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX === -1 || startY === -1) return;

                diffX = getX(e) - startX;
                diffY = getY(e) - startY;

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
                } else if ((thresX || thresY)) {
                    cancel();
                }

                throttledMove(startX, startY, diffX, diffY);

                function cancel() {
                    if (e.cancelable) {
                        e.preventDefault();
                        e.stopPropagation();
                    }
                }
            };

            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX == -1 || startY == -1) return;
                const sX = startX;
                const sY = startY;

                startX = startY = -1;

                try {
                    const div = ((Math.abs(trigger) < 1) ? bcr.width : 1);
                    const compX = Math.abs(diffX) / div;
                    const compY = Math.abs(diffY) / div;
                    if (compX > Math.abs(trigger) || compY > Math.abs(trigger)) {
                        return await dotnetObj.invokeMethodAsync('OnTrigger', diffX, diffY);
                    }
                } finally {
                    await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, diffX, diffY);
                    diffX = diffY = 0;
                    orientation = BitSwipeOrientation.None;
                }
            };

            const onLeave = (e: PointerEvent) => {
                if (startX == -1 || startY == -1) return;
                dotnetObj.invokeMethodAsync('OnEnd', startX, startY, diffX, diffY);
                startX = startY = -1;
                diffX = diffY = 0;
                orientation = BitSwipeOrientation.None;
            }

            if (isTouchDevice) {
                element.addEventListener('touchstart', onStart);
                element.addEventListener('touchmove', onMove);
                element.addEventListener('touchend', onEnd);
                element.addEventListener('touchcancel', onEnd);
            } else {
                element.addEventListener('pointerdown', onStart);
                element.addEventListener('pointermove', onMove);
                element.addEventListener('pointerup', onEnd);
                element.addEventListener('pointerleave', onEnd, false);
            }

            const swipeTrap = new BitSwipeTrap(id, element, trigger, dotnetObj);

            swipeTrap.setRemoveHandlersFn(() => {
                if (isTouchDevice) {
                    element.removeEventListener('touchstart', onStart);
                    element.removeEventListener('touchmove', onMove);
                    element.removeEventListener('touchend', onEnd);
                    element.removeEventListener('touchcancel', onEnd);
                } else {
                    element.removeEventListener('pointerdown', onStart);
                    element.removeEventListener('pointermove', onMove);
                    element.removeEventListener('pointerup', onEnd);
                    element.removeEventListener('pointerleave', onEnd, false);
                }
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
            this.dotnetObj.dispose();
        }
    }

    enum BitSwipeOrientation {
        None,
        Horizontal,
        Vertical
    }

}
