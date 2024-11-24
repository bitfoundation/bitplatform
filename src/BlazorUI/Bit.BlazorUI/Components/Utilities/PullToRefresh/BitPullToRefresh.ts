namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(
            id: string,
            element: HTMLElement,
            anchorElement: HTMLElement | undefined,
            anchorSelector: string | undefined,
            threshold: number | undefined,
            dotnetObj: DotNetObject) {
            const anchorEl = anchorElement ?? document.querySelector(anchorSelector ?? 'body') as HTMLElement;
            const isTouchDevice = Utils.isTouchDevice();
            threshold ??= 80;
            let startY = -1;

            const getY = (e: TouchEvent | PointerEvent) => {
                if (e instanceof TouchEvent) {
                    return e.touches[0].screenY;
                }

                return e.screenY;
            }

            const onScroll = () => {
                anchorEl.style.touchAction = anchorEl.scrollTop !== 0 ? '' : 'pan-x pan-down pinch-zoom';
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
                    startY = getY(e);
                    return;
                }

                let diff = getY(e) - startY;

                if (diff < 0) {
                    startY = getY(e);
                    return;
                }

                if (e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                diff = diff > threshold ? threshold : diff;
                element.style.minHeight = `${diff}px`;

                await dotnetObj.invokeMethodAsync('OnMove', diff);
            };
            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startY === -1) return;

                const diff = parseInt(element.style.minHeight);
                await dotnetObj.invokeMethodAsync('OnEnd', isNaN(diff) ? 0 : diff);
                if (diff >= threshold) {
                    await dotnetObj.invokeMethodAsync('Refresh');
                }
                element.style.minHeight = '0';
                startY = -1;
            };
            const onLeave = (e: TouchEvent | PointerEvent) => {
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

            const refresher = new BitPullRefresher(id, element, anchorEl, threshold, dotnetObj);
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
                    anchorEl.removeEventListener('scroll', onScroll);
                }
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