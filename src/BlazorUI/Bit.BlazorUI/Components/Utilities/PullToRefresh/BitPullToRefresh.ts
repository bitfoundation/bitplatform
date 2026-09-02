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
            enabled: boolean,
            dotnetObj: DotNetObject) {
            const anchorEl = anchor ?? document.body as HTMLElement;
            const scrollerEl = scrollerElement ?? ((scrollerSelector && (anchorEl.querySelector(scrollerSelector) ?? document.querySelector(scrollerSelector))) ?? (!!anchor ? anchor.children[0] : anchorEl)) as HTMLElement;

            const options: BitPullToRefreshOptions = { trigger, factor, margin, threshold, enabled };
            const state: BitPullToRefreshState = { diff: 0, startY: -1, refreshing: false };
            const isTouchDevice = Utils.isTouchDevice();

            const getY = (e: TouchEvent | PointerEvent) => isTouchDevice ? (e as TouchEvent).touches[0].screenY : (e as PointerEvent).screenY;

            const onScroll = () => {
                anchorEl.style.touchAction = (options.enabled && scrollerEl.scrollTop === 0) ? 'pan-x pan-down pinch-zoom' : '';
                scrollerEl.style.overscrollBehaviorY = options.enabled ? 'contain' : '';
            };
            const onStart = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (!isTouchDevice && (e as PointerEvent).button !== 0) return;

                if (!options.enabled || state.refreshing || scrollerEl.scrollTop !== 0) {
                    state.startY = -1;
                    return;
                }
                state.startY = getY(e);
                loadingEl.classList.remove('bit-ptr-rtn');
                const bcr = anchorEl.getBoundingClientRect();
                loadingEl.style.width = `${bcr.width}px`;

                await dotnetObj.invokeMethodAsync('OnStart', bcr.top, bcr.left, bcr.width);
            };
            const onMove = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (state.startY === -1 || state.refreshing) return;

                if (scrollerEl.scrollTop !== 0) {
                    state.startY = -1;
                    return;
                }

                let diff = getY(e) - state.startY;

                if (diff < 0) {
                    state.startY = -1;
                    return;
                }

                if (diff <= options.threshold) {
                    // Back inside the dead zone: drop the pull height so a release from here cannot
                    // trigger a refresh with the distance the pull had before it came back.
                    if (state.diff !== 0) {
                        state.diff = 0;
                        loadingEl.style.minHeight = '0';
                        await dotnetObj.invokeMethodAsync('OnMove', 0);
                    }
                    return;
                }

                if (e.cancelable) {
                    e.preventDefault();
                    e.stopPropagation();
                }

                diff = (diff - options.threshold) / options.factor;
                diff = diff > options.trigger ? options.trigger : diff;
                state.diff = diff;
                loadingEl.style.minHeight = `${diff * options.factor + options.margin}px`;

                await dotnetObj.invokeMethodAsync('OnMove', diff);
            };
            const onEnd = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (state.startY === -1 || state.refreshing) return;
                state.startY = -1;

                try {
                    await dotnetObj.invokeMethodAsync('OnEnd', state.diff);

                    if (state.diff >= options.trigger) {
                        state.refreshing = true;
                        await dotnetObj.invokeMethodAsync('Refresh');
                    }
                } finally {
                    state.diff = 0;
                    state.refreshing = false;
                    PullToRefresh.snapBack(loadingEl);
                }
            };
            const onCancel = async (e: TouchEvent | PointerEvent): Promise<void> => {
                if (state.startY === -1 || state.refreshing) return;
                state.startY = -1;

                const diff = state.diff;
                state.diff = 0;
                PullToRefresh.snapBack(loadingEl);

                await dotnetObj.invokeMethodAsync('OnCancel', diff);
            };

            if (isTouchDevice) {
                anchorEl.addEventListener('touchstart', onStart);
                anchorEl.addEventListener('touchmove', onMove, { passive: false });
                anchorEl.addEventListener('touchend', onEnd);
                anchorEl.addEventListener('touchcancel', onCancel);
            } else {
                anchorEl.addEventListener('pointerdown', onStart);
                anchorEl.addEventListener('pointermove', onMove);
                anchorEl.addEventListener('pointerup', onEnd);
                anchorEl.addEventListener('pointerleave', onCancel, false);
                anchorEl.addEventListener('pointercancel', onCancel);
            }
            scrollerEl.addEventListener('scroll', onScroll);
            onScroll();

            const refresher = new BitPullRefresher(id, anchorEl, loadingEl, options, state, dotnetObj, onScroll);
            refresher.setDisposer(() => {
                if (isTouchDevice) {
                    anchorEl.removeEventListener('touchstart', onStart);
                    anchorEl.removeEventListener('touchmove', onMove);
                    anchorEl.removeEventListener('touchend', onEnd);
                    anchorEl.removeEventListener('touchcancel', onCancel);
                } else {
                    anchorEl.removeEventListener('pointerdown', onStart);
                    anchorEl.removeEventListener('pointermove', onMove);
                    anchorEl.removeEventListener('pointerup', onEnd);
                    anchorEl.removeEventListener('pointerleave', onCancel, false);
                    anchorEl.removeEventListener('pointercancel', onCancel);
                }
                scrollerEl.removeEventListener('scroll', onScroll);
                anchorEl.style.touchAction = '';
                scrollerEl.style.overscrollBehaviorY = '';
                loadingEl.style.minHeight = '';
            });
            PullToRefresh._refreshers.push(refresher);
        }

        public static update(
            id: string,
            trigger: number,
            factor: number,
            margin: number,
            threshold: number,
            enabled: boolean) {
            const refresher = PullToRefresh._refreshers.find(r => r.id === id);
            if (!refresher) return;

            refresher.options.trigger = trigger;
            refresher.options.factor = factor;
            refresher.options.margin = margin;
            refresher.options.threshold = threshold;
            refresher.options.enabled = enabled;

            if (!enabled && !refresher.state.refreshing) {
                refresher.state.diff = 0;
                refresher.state.startY = -1;
                PullToRefresh.snapBack(refresher.loadingEl);
            }

            refresher.syncTouchAction();
        }

        public static async refresh(id: string) {
            const refresher = PullToRefresh._refreshers.find(r => r.id === id);
            if (!refresher) return;

            await refresher.refresh();
        }

        public static dispose(id: string) {
            const refresher = PullToRefresh._refreshers.find(r => r.id === id);
            if (!refresher) return;

            PullToRefresh._refreshers = PullToRefresh._refreshers.filter(r => r.id !== id);
            refresher.dispose();
        }

        private static snapBack(loadingEl: HTMLElement) {
            loadingEl.classList.add('bit-ptr-rtn');
            void loadingEl.offsetHeight;
            loadingEl.style.minHeight = '0';
        }
    }

    interface BitPullToRefreshOptions {
        trigger: number;
        factor: number;
        margin: number;
        threshold: number;
        enabled: boolean;
    }

    interface BitPullToRefreshState {
        diff: number;
        startY: number;
        refreshing: boolean;
    }

    class BitPullRefresher {
        id: string;
        anchorEl: HTMLElement;
        loadingEl: HTMLElement;
        options: BitPullToRefreshOptions;
        state: BitPullToRefreshState;
        dotnetObj: DotNetObject;
        syncTouchAction: () => void;
        disposer: () => void = () => { };

        constructor(id: string,
            anchorEl: HTMLElement,
            loadingEl: HTMLElement,
            options: BitPullToRefreshOptions,
            state: BitPullToRefreshState,
            dotnetObj: DotNetObject,
            syncTouchAction: () => void) {
            this.id = id;
            this.anchorEl = anchorEl;
            this.loadingEl = loadingEl;
            this.options = options;
            this.state = state;
            this.dotnetObj = dotnetObj;
            this.syncTouchAction = syncTouchAction;
        }

        public async refresh() {
            if (!this.options.enabled || this.state.refreshing) return;
            this.state.refreshing = true;
            this.state.diff = 0;
            this.state.startY = -1;

            try {
                const bcr = this.anchorEl.getBoundingClientRect();
                this.loadingEl.style.width = `${bcr.width}px`;
                this.loadingEl.classList.add('bit-ptr-rtn');
                void this.loadingEl.offsetHeight;
                this.loadingEl.style.minHeight = `${this.options.trigger * this.options.factor + this.options.margin}px`;

                await this.dotnetObj.invokeMethodAsync('Refresh');
            } finally {
                this.state.refreshing = false;
                this.loadingEl.style.minHeight = '0';
            }
        }

        public setDisposer(disposer: () => void) {
            this.disposer = disposer;
        }

        public dispose() {
            this.disposer();
            this.dotnetObj?.dispose();
        }
    }
}
