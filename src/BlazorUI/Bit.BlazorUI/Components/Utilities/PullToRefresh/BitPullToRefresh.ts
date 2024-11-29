namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(
            id: string,
            element: HTMLElement,
            anchorElement: HTMLElement | undefined,
            anchorSelector: string | undefined,
            trigger: number | undefined,
            factor: number | undefined,
            margin: number | undefined,
            threshold: number | undefined,
            dotnetObj: DotNetObject) {
            const anchorEl = anchorElement ?? document.querySelector(anchorSelector ?? 'body') as HTMLElement;
            const isTouchDevice = Utils.isTouchDevice();
            trigger ??= 80;
            factor ??= 2;
            margin ??= 30;
            threshold ??= 10;
            let startY = -1;

            const getY = (e: TouchEvent | PointerEvent) => {
                return (e instanceof TouchEvent)
                    ? e.touches[0].screenY
                    : e.screenY;
            }

            const onScroll = () => {
                anchorEl.style.touchAction = anchorEl.scrollTop === 0 ? 'pan-x pan-down pinch-zoom' : "";
            };
            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (anchorEl.scrollTop !== 0) {
                    startY = -1;
                    return;
                }
                startY = getY(e);
                const bcr = anchorEl.getBoundingClientRect();
                element.style.top = `${bcr.top}px`;
                element.style.left = `${bcr.left}px`;
                element.style.width = `${bcr.width}px`;
                
                await dotnetObj.invokeMethodAsync('OnStart', bcr.top, bcr.left, bcr.width);
            };
            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startY === -1) return;

                if (anchorEl.scrollTop !== 0) {
                    //startY = getY(e);
                    startY = -1;
                    return;
                }

                let diff = getY(e) - startY;

                if (diff < 0) {
                    //startY = getY(e);
                    startY = -1;
                    return;
                }

                if (diff > threshold && e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                diff = diff / factor;
                diff = diff > trigger ? trigger : diff;
                element.style.minHeight = `${diff * factor + margin}px`;

                await dotnetObj.invokeMethodAsync('OnMove', diff);
            };
            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                //if (startY === -1) return;

                let diff = parseInt(element.style.minHeight);
                diff = isNaN(diff) ? 0 : diff;
                diff = (diff - margin) / factor;

                await dotnetObj.invokeMethodAsync('OnEnd', diff);

                startY = -1;
                if (diff >= trigger) {
                    await dotnetObj.invokeMethodAsync('Refresh');
                }
                element.style.minHeight = '0';
            };
            const onLeave = (e: PointerEvent) => {
                if (startY === -1) return;
                element.style.minHeight = '0';
                startY = -1;
            }

            if (isTouchDevice) {
                anchorEl.addEventListener('touchstart', onStart);
                anchorEl.addEventListener('touchmove', onMove);
                anchorEl.addEventListener('touchend', onEnd);
            } else {
                anchorEl.addEventListener('pointerdown', onStart);
                anchorEl.addEventListener('pointermove', onMove);
                anchorEl.addEventListener('pointerup', onEnd);
                anchorEl.addEventListener('pointerleave', onLeave, false);
                //anchorEl.addEventListener('pointerout', onOut, false);
            }
            anchorEl.addEventListener('scroll', onScroll);
            onScroll();

            const refresher = new BitPullRefresher(id, element, anchorEl, trigger, dotnetObj);
            refresher.setDisposer(() => {
                if (isTouchDevice) {
                    anchorEl.removeEventListener('touchstart', onStart);
                    anchorEl.removeEventListener('touchmove', onMove);
                    anchorEl.removeEventListener('touchend', onEnd);
                } else {
                    anchorEl.removeEventListener('pointerdown', onStart);
                    anchorEl.removeEventListener('pointermove', onMove);
                    anchorEl.removeEventListener('pointerup', onEnd);
                    anchorEl.removeEventListener('pointerleave', onLeave, false);
                    //anchorEl.removeEventListener('pointerout', onOut, false);
                }
                anchorEl.removeEventListener('scroll', onScroll);
            });
            PullToRefresh._refreshers.push(refresher);
        }

        public static dispose(id: string) {
            const refresher = PullToRefresh._refreshers.find(r => r.id === id);
            if (!refresher) return;

            PullToRefresh._refreshers = PullToRefresh._refreshers.filter(r => r.id !== id);
            refresher.dispose();
        }

    }

    class BitPullRefresher {
        id: string;
        element: HTMLElement;
        anchor: HTMLElement;
        threshold: number;
        dotnetObj: DotNetObject;
        disposer: () => void = () => { };

        constructor(id: string, element: HTMLElement, anchor: HTMLElement, threshold: number, dotnetObj: DotNetObject) {
            this.id = id;
            this.element = element;
            this.anchor = anchor;
            this.threshold = threshold;
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
}