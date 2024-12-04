namespace BitBlazorUI {
    export class SwipeTrap {
        private static _swipeTraps: BitSwipeTrap[] = [];

        public static setup(
            id: string, 
            element: HTMLElement,
            trigger: number,
            threshold: number,
            dotnetObj: DotNetObject) {
            const bcr = element.getBoundingClientRect();

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            const isTouchDevice = Utils.isTouchDevice();

            const getX = (e: TouchEvent | PointerEvent) => (e instanceof TouchEvent) ? e.touches[0].screenX : e.screenX;
            const getY = (e: TouchEvent | PointerEvent) => (e instanceof TouchEvent) ? e.touches[0].screenY : e.screenY;

            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                startX = getX(e);
                startY = getY(e);

                await dotnetObj.invokeMethodAsync('OnStart', startX, startY);
            };

            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startX === -1 && startY === -1) return;

                diffX = getX(e) - startX;
                diffY = getY(e) - startY;

                if ((Math.abs(diffX) > threshold || Math.abs(diffY) > threshold) && e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                await dotnetObj.invokeMethodAsync('OnMove', startX, startY, diffX, diffY);
            };

            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                const sX = startX;
                const sY = startY;

                startX = startY = -1;

                try {
                    if ((Math.abs(diffX) / bcr.width) > trigger || (Math.abs(diffY) / bcr.height) > trigger) {
                        diffX = diffY = 0;
                        return await dotnetObj.invokeMethodAsync('OnTrigger');
                    }
                } finally {
                    await dotnetObj.invokeMethodAsync('OnEnd', sX, sY, diffX, diffY);
                }
            };

            const onLeave = (e: PointerEvent) => {
                startX = startY = -1;
                diffX = diffY = 0;
            }

            if (isTouchDevice) {
                element.addEventListener('touchstart', onStart);
                element.addEventListener('touchmove', onMove);
                element.addEventListener('touchend', onEnd);
            } else {
                element.addEventListener('pointerdown', onStart);
                element.addEventListener('pointermove', onMove);
                element.addEventListener('pointerup', onEnd);
                element.addEventListener('pointerleave', onLeave, false);
            }

            const swipeTrap = new BitSwipeTrap(id, element, trigger, dotnetObj);

            swipeTrap.setRemoveHandlersFn(() => {
                if (isTouchDevice) {
                    element.removeEventListener('touchstart', onStart);
                    element.removeEventListener('touchmove', onMove);
                    element.removeEventListener('touchend', onEnd);
                } else {
                    element.removeEventListener('pointerdown', onStart);
                    element.removeEventListener('pointermove', onMove);
                    element.removeEventListener('pointerup', onEnd);
                    element.removeEventListener('pointerleave', onLeave, false);
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
}
