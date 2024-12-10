namespace BitBlazorUI {
    export class Swipes {
        private static _swipes: BitSwipe[] = [];

        public static setup(
            id: string,
            trigger: number,
            position: BitSwipePosition,
            isRtl: boolean,
            dotnetObj: DotNetObject) {
            const windowWidth = window.innerWidth;
            if (windowWidth >= Utils.MAX_MOBILE_WIDTH) return;

            const element = document.getElementById(id);
            if (!element) return;

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            let originalTransform: string;
            const bcr = element.getBoundingClientRect();
            const isTouchDevice = Utils.isTouchDevice();

            const getX = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenX : (e as PointerEvent).screenX;
            const getY = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenY : (e as PointerEvent).screenY;

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                startX = getX(e);
                startY = getY(e);

                element.style.transitionDuration = '0s';
                originalTransform = element.style.transform;

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX === -1 && startY === -1) return;

                diffX = getX(e) - startX;
                diffY = getY(e) - startY;

                if (e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                if ((!isRtl && position === BitSwipePosition.Start) || (isRtl && position === BitSwipePosition.End)) {
                    if (diffX < 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if ((!isRtl && position === BitSwipePosition.End) || (isRtl && position === BitSwipePosition.Start)) {
                    if (diffX > 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === BitSwipePosition.Top) {
                    if (diffY < 0) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === BitSwipePosition.Bottom) {
                    if (diffY > 0) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                await dotnetObj.invokeMethodAsync('OnMove', diffX, diffY);
            };

            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                startX = startY = -1;

                element.style.transitionDuration = '';
                try {
                    if (((!isRtl && position === BitSwipePosition.Start) || (isRtl && position === BitSwipePosition.End)) && diffX < 0) {
                        if ((Math.abs(diffX) / bcr.width) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (((!isRtl && position === BitSwipePosition.End) || (isRtl && position === BitSwipePosition.Start)) && diffX > 0) {
                        if ((diffX / bcr.width) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (position === BitSwipePosition.Top && diffY < 0) {
                        if ((Math.abs(diffY) / bcr.height) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (position === BitSwipePosition.Bottom && diffY > 0) {
                        if ((diffY / bcr.height) > trigger) {
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    element.style.transform = originalTransform;
                } finally {
                    await dotnetObj.invokeMethodAsync('OnEnd', diffX, diffY);
                    diffX = diffY = 0;
                }
            };

            const onLeave = (e: PointerEvent) => {
                dotnetObj.invokeMethodAsync('OnEnd', diffX, diffY);

                startX = startY = -1;
                diffX = diffY = 0;
                element.style.transitionDuration = '';
                element.style.transform = originalTransform;
            }

            if (isTouchDevice) {
                element.addEventListener('touchstart', onStart);
                element.addEventListener('touchmove', onMove);
                element.addEventListener('touchend', onEnd);
            } else {
                element.addEventListener('pointerdown', onStart);
                element.addEventListener('pointermove', onMove);
                element.addEventListener('pointerup', onEnd);
                element.addEventListener('pointerleave', onEnd, false);
            }

            const swipe = new BitSwipe(id, element, trigger, dotnetObj);
            swipe.setDisposer(() => {
                if (isTouchDevice) {
                    element.removeEventListener('touchstart', onStart);
                    element.removeEventListener('touchmove', onMove);
                    element.removeEventListener('touchend', onEnd);
                } else {
                    element.removeEventListener('pointerdown', onStart);
                    element.removeEventListener('pointermove', onMove);
                    element.removeEventListener('pointerup', onEnd);
                    element.removeEventListener('pointerleave', onEnd, false);
                }
            });
            Swipes._swipes.push(swipe);
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
        dotnetObj: DotNetObject;
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
            this.dotnetObj.dispose();
        }
    }

    enum BitSwipePosition {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3,
    }
}
