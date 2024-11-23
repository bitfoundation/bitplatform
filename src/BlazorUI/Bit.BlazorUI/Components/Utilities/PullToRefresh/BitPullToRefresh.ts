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
            threshold ??= 80;
            let startY = -1;

            const onScroll = () => {
                anchorEl.style.touchAction = anchorEl.scrollTop !== 0 ? '' : 'pan-x pan-down pinch-zoom';
            };
            const onStart = async (e: PointerEvent): Promise<void> => {
                startY = e.screenY;
                const bcr = anchorEl.getBoundingClientRect();
                element.style.top = `${bcr.top}px`;
                element.style.left = `${bcr.left}px`;
                element.style.width = `${bcr.width}px`;

                await dotnetObj.invokeMethodAsync('OnStart', bcr.top, bcr.left, bcr.width);
            };
            const onMove = async (e: PointerEvent): Promise<void> => {
                if (startY === -1) return;

                if (anchorEl.scrollTop !== 0) {
                    startY = e.screenY;
                    return;
                }

                let diff = e.screenY - startY;

                if (diff < 0) {
                    startY = e.screenY;
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
            const onEnd = async (e: PointerEvent): Promise<void> => {
                if (startY === -1) return;

                const diff = parseInt(element.style.minHeight);
                await dotnetObj.invokeMethodAsync('OnEnd', diff);
                if (diff >= threshold) {
                    await dotnetObj.invokeMethodAsync('Refresh');
                }
                element.style.minHeight = '0';
                startY = -1;
            };
            const onLeave = (e: PointerEvent) => {
                element.style.minHeight = '0';
                startY = -1;
            }

            anchorEl.addEventListener('pointerdown', onStart);
            anchorEl.addEventListener('pointermove', onMove);
            anchorEl.addEventListener('pointerup', onEnd);
            anchorEl.addEventListener('pointerleave', onLeave, false);
            //anchorEl.addEventListener('pointerout', onOut, false);
            anchorEl.addEventListener('scroll', onScroll);

            const refresher = new BitPullRefresher(id, element, anchorEl, threshold, dotnetObj);
            refresher.setDisposer(() => {
                anchorEl.removeEventListener('pointerdown', onStart);
                anchorEl.removeEventListener('pointermove', onMove);
                anchorEl.removeEventListener('pointerup', onEnd);
                anchorEl.removeEventListener('pointerleave', onLeave, false);
                //anchorEl.removeEventListener('pointerout', onOut, false);
                anchorEl.removeEventListener('scroll', onScroll);
            });
            PullToRefresh._refreshers.push(refresher);
            onScroll();
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