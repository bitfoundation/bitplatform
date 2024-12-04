namespace BitBlazorUI {
    export class Panel {
        private static _panels: BitPanel[] = [];

        public static setup(
            id: string,
            trigger: number,
            position: BitPanelPosition,
            isRtl: boolean,
            dotnetObj: DotNetObject) {
            const element = document.getElementById(id);
            if (!element) return;
            const bcr = element.getBoundingClientRect();

            let diffX = 0;
            let diffY = 0;
            let startX = -1;
            let startY = -1;
            let originalTransform: string;
            const isTouchDevice = Utils.isTouchDevice();

            const getX = (e: TouchEvent | PointerEvent) => (e instanceof TouchEvent) ? e.touches[0].screenX : e.screenX;
            const getY = (e: TouchEvent | PointerEvent) => (e instanceof TouchEvent) ? e.touches[0].screenY : e.screenY;

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

                if ((!isRtl && position === BitPanelPosition.Start) || (isRtl && position === BitPanelPosition.End)) {
                    if (diffX < 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if ((!isRtl && position === BitPanelPosition.End) || (isRtl && position === BitPanelPosition.Start)) {
                    if (diffX > 0) {
                        element.style.transform = `translateX(${diffX}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === BitPanelPosition.Top) {
                    if (diffY < 0) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                if (position === BitPanelPosition.Bottom) {
                    if (diffY > 0) {
                        element.style.transform = `translateY(${diffY}px)`;
                    } else {
                        element.style.transform = originalTransform;
                    }
                }

                await dotnetObj.invokeMethodAsync('OnMove', diffX, diffY);
            };

            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                startX = -1;
                startY = -1;

                element.style.transitionDuration = '';
                try {
                    if (((!isRtl && position === BitPanelPosition.Start) || (isRtl && position === BitPanelPosition.End)) && diffX < 0) {
                        if ((Math.abs(diffX) / bcr.width) > trigger) {
                            diffX = diffY = 0;
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (((!isRtl && position === BitPanelPosition.End) || (isRtl && position === BitPanelPosition.Start)) && diffX > 0) {
                        if ((diffX / bcr.width) > trigger) {
                            diffX = diffY = 0;
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (position === BitPanelPosition.Top && diffY < 0) {
                        if ((Math.abs(diffY) / bcr.height) > trigger) {
                            diffX = diffY = 0;
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }

                    if (position === BitPanelPosition.Bottom && diffY > 0) {
                        if ((diffY / bcr.height) > trigger) {
                            diffX = diffY = 0;
                            return await dotnetObj.invokeMethodAsync('OnClose');
                        }
                    }
                } finally {
                    element.style.transform = originalTransform;
                    await dotnetObj.invokeMethodAsync('OnEnd', diffX, diffY);
                }
            };

            const onLeave = (e: PointerEvent) => {
                if (startX === -1 && startY === -1) return;
                startX = -1;
                startY = -1;
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

            const panel = new BitPanel(id, element, trigger, dotnetObj);
            panel.setDisposer(() => {
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
            Panel._panels.push(panel);
        }

        public static dispose(id: string) {
            const panel = Panel._panels.find(r => r.id === id);
            if (!panel) return;

            Panel._panels = Panel._panels.filter(r => r.id !== id);
            panel.dispose();
        }
    }

    class BitPanel {
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

    enum BitPanelPosition {
        Start = 0,
        End = 1,
        Top = 2,
        Bottom = 3,
    }
}
