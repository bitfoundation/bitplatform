namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(
            id: string,
            anchor: HTMLElement | undefined,
            loadingEl: HTMLElement,
            scrollerElement: HTMLElement | undefined,
            scrollerSelector: string | undefined,
            trigger: number,
            factor: number,
            margin: number,
            threshold: number,
            dotnetObj: DotNetObject) {
            const anchorEl = anchor ?? document.body as HTMLElement;
            const scrollerEl = scrollerElement ?? ((scrollerSelector && document.querySelector(scrollerSelector)) ?? (!!anchor ? anchor.children[0] : anchorEl)) as HTMLElement;

            let diff = 0;
            let startY = -1;
            let refreshing = false;
            const isTouchDevice = Utils.isTouchDevice();

            const getY = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenY : (e as PointerEvent).screenY;

            const onScroll = () => {
                anchorEl.style.touchAction = scrollerEl.scrollTop === 0 ? 'pan-x pan-down pinch-zoom' : "";
            };
            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (scrollerEl.scrollTop !== 0 || refreshing) {
                    startY = -1;
                    return;
                }
                startY = getY(e);
                const bcr = anchorEl.getBoundingClientRect();
                loadingEl.style.width = `${bcr.width}px`;

                await dotnetObj.invokeMethodAsync('OnStart', bcr.top, bcr.left, bcr.width);
            };
            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startY === -1 || refreshing) return;

                if (scrollerEl.scrollTop !== 0) {
                    startY = -1;
                    return;
                }

                diff = getY(e) - startY;

                if (diff < 0) {
                    startY = -1;
                    return;
                }

                if (diff > threshold && e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                diff = diff / factor;
                diff = diff > trigger ? trigger : diff;
                loadingEl.style.minHeight = `${diff * factor + margin}px`;

                await dotnetObj.invokeMethodAsync('OnMove', diff);
            };
            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (startY === -1 || refreshing) return;
                startY = -1;

                try {
                    await dotnetObj.invokeMethodAsync('OnEnd', diff);

                    if (diff >= trigger) {
                        refreshing = true;
                        await dotnetObj.invokeMethodAsync('Refresh');
                    }
                } finally {
                    diff = 0;
                    refreshing = false;
                    loadingEl.style.minHeight = '0';
                }
            };
            const onLeave = (e: PointerEvent) => {
                if (startY === -1) return;
                loadingEl.style.minHeight = '0';
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
            scrollerEl.addEventListener('scroll', onScroll);
            onScroll();

            const refresher = new BitPullRefresher(id, anchor, loadingEl, scrollerElement, scrollerSelector, trigger, factor, margin, threshold, dotnetObj);
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
                scrollerEl.removeEventListener('scroll', onScroll);
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
        anchor: HTMLElement | undefined;
        loadingEl: HTMLElement;
        scrollerElement: HTMLElement | undefined;
        scrollerSelector: string | undefined;
        trigger: number;
        factor: number;
        margin: number;
        threshold: number;
        dotnetObj: DotNetObject;
        disposer: () => void = () => { };

        constructor(id: string,
            anchor: HTMLElement | undefined,
            loadingEl: HTMLElement,
            scrollerElement: HTMLElement | undefined,
            scrollerSelector: string | undefined,
            trigger: number,
            factor: number,
            margin: number,
            threshold: number,
            dotnetObj: DotNetObject) {
            this.id = id;
            this.anchor = anchor;
            this.loadingEl = loadingEl;
            this.scrollerElement = scrollerElement;
            this.scrollerSelector = scrollerSelector;
            this.trigger = trigger;
            this.factor = factor;
            this.margin = margin;
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