namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(id: string, element: HTMLElement, anchor: string, threshold: number, dotnetObj: DotNetObject) {
            const anchorEl = document.querySelector(anchor) as HTMLElement;
            let touchStartY = 0;

            const onTouchStart = async (e: TouchEvent): Promise<void> => {
                touchStartY = e.touches[0].clientY;
                const bcr = anchorEl.getBoundingClientRect();
                element.style.top = `${bcr.top}px`;
                element.style.left = `${bcr.left}px`;
                element.style.width = `${bcr.width}px`;

                await dotnetObj.invokeMethodAsync('TouchStart', bcr.top, bcr.left, bcr.width);
            };
            const onTouchMove = async (e: TouchEvent): Promise<void> => {
                if (anchorEl.scrollTop !== 0) {
                    touchStartY = e.touches[0].clientY;
                    return;
                }

                const touchY = e.touches[0].clientY;
                let diff = touchY - touchStartY;

                if (diff < 0) {
                    touchStartY = e.touches[0].clientY;
                    return;
                }

                if (e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                diff = diff > threshold ? threshold : diff;
                element.style.minHeight = `${diff}px`;

                await dotnetObj.invokeMethodAsync('TouchMove', diff);
            };
            const onTouchEnd = async (e: TouchEvent): Promise<void> => {
                const diff = parseInt(element.style.minHeight);
                await dotnetObj.invokeMethodAsync('TouchEnd', diff);
                if (diff >= threshold) {
                    await dotnetObj.invokeMethodAsync('Refresh');
                }
                element.style.minHeight = '0';
            };

            anchorEl.addEventListener('touchstart', onTouchStart);
            anchorEl.addEventListener('touchmove', onTouchMove);
            anchorEl.addEventListener('touchend', onTouchEnd);

            const refresher = new BitPullRefresher(id, element, anchor, threshold, dotnetObj);
            refresher.setDisposer(() => {
                anchorEl.removeEventListener('touchstart', onTouchStart);
                anchorEl.removeEventListener('touchmove', onTouchMove);
                anchorEl.removeEventListener('touchend', onTouchEnd);
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
        anchor: string;
        threshold: number;
        dotnetObj: DotNetObject;
        disposer: () => void = () => { };

        constructor(id: string, element: HTMLElement, anchor: string, threshold: number, dotnetObj: DotNetObject) {
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