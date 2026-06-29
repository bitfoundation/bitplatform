namespace BitBlazorUI {
    export class Pivot {
        private static _instances: Record<string, PivotInstance> = {};

        public static setup(
            id: string,
            header: HTMLElement,
            moreButton: HTMLElement | null,
            isMenu: boolean,
            isSlide: boolean,
            isRtl: boolean,
            dotnetObj: DotNetObject) {
            if (!header) return;

            Pivot.dispose(id);

            const instance = new PivotInstance(id, header, moreButton, isMenu, isSlide, isRtl, dotnetObj);
            Pivot._instances[id] = instance;
            instance.start();
        }

        public static refresh(id: string) {
            const instance = Pivot._instances[id];
            if (!instance) return;
            instance.update();
        }

        public static slide(id: string, forward: boolean) {
            const instance = Pivot._instances[id];
            if (!instance) return;
            instance.slide(forward);
        }

        public static dispose(id: string) {
            const instance = Pivot._instances[id];
            if (!instance) return;
            instance.dispose();
            delete Pivot._instances[id];
        }
    }

    class PivotInstance {
        private id: string;
        private header: HTMLElement;
        private moreButton: HTMLElement | null;
        private isMenu: boolean;
        private isSlide: boolean;
        private isRtl: boolean;
        private dotnetObj: DotNetObject;
        private observer: ResizeObserver | null = null;
        private scrollHandler: (() => void) | null = null;
        private lastOverflow: string = '';
        private lastSlideState: string = '';

        constructor(
            id: string,
            header: HTMLElement,
            moreButton: HTMLElement | null,
            isMenu: boolean,
            isSlide: boolean,
            isRtl: boolean,
            dotnetObj: DotNetObject) {
            this.id = id;
            this.header = header;
            this.moreButton = moreButton;
            this.isMenu = isMenu;
            this.isSlide = isSlide;
            this.isRtl = isRtl;
            this.dotnetObj = dotnetObj;
        }

        public start() {
            try {
                this.observer = new ResizeObserver(() => this.update());
                this.observer.observe(this.header);

                if (this.isSlide) {
                    this.scrollHandler = Utils.throttle(() => this.updateSlide(), 100) as () => void;
                    this.header.addEventListener('scroll', this.scrollHandler, { passive: true });
                }
            } catch (e) {
                console.error('BitBlazorUI.Pivot.start:', e);
            }

            this.update();
        }

        public update() {
            if (this.isMenu) this.updateMenu();
            if (this.isSlide) this.updateSlide();
        }

        private getItems(): HTMLElement[] {
            return Array.from(this.header.querySelectorAll<HTMLElement>('.bit-pvti:not(.bit-pvt-mor)'));
        }

        private static outerWidth(el: HTMLElement): number {
            const style = window.getComputedStyle(el);
            const marginLeft = parseFloat(style.marginLeft) || 0;
            const marginRight = parseFloat(style.marginRight) || 0;
            return el.offsetWidth + marginLeft + marginRight;
        }

        private updateMenu() {
            try {
                const items = this.getItems();

                // reset everything to its natural state before measuring.
                items.forEach(it => (it.style.display = ''));
                if (this.moreButton) this.moreButton.style.display = 'none';

                const containerWidth = this.header.clientWidth;

                let total = 0;
                items.forEach(it => (total += PivotInstance.outerWidth(it)));

                let overflowIndexes: number[] = [];

                if (total > containerWidth + 1) {
                    if (this.moreButton) this.moreButton.style.display = '';
                    const moreWidth = this.moreButton ? PivotInstance.outerWidth(this.moreButton) : 0;
                    const available = containerWidth - moreWidth;

                    let used = 0;
                    items.forEach((it, i) => {
                        used += PivotInstance.outerWidth(it);
                        if (used > available) {
                            it.style.display = 'none';
                            overflowIndexes.push(i);
                        }
                    });

                    // if nothing actually overflowed (e.g. only the more button didn't fit) hide it.
                    if (overflowIndexes.length === 0 && this.moreButton) {
                        this.moreButton.style.display = 'none';
                    }
                }

                const serialized = overflowIndexes.join(',');
                if (serialized === this.lastOverflow) return;
                this.lastOverflow = serialized;

                this.dotnetObj.invokeMethodAsync('OnSetOverflowItems', overflowIndexes);
            } catch (e) {
                console.error('BitBlazorUI.Pivot.updateMenu:', e);
            }
        }

        private updateSlide() {
            try {
                const scrollLeft = this.header.scrollLeft;
                const maxScroll = this.header.scrollWidth - this.header.clientWidth;
                const hasOverflow = maxScroll > 1;

                let atStart: boolean;
                let atEnd: boolean;

                if (this.isRtl) {
                    const abs = Math.abs(scrollLeft);
                    atStart = abs <= 1;
                    atEnd = abs >= maxScroll - 1;
                } else {
                    atStart = scrollLeft <= 1;
                    atEnd = scrollLeft >= maxScroll - 1;
                }

                const serialized = `${hasOverflow}|${atStart}|${atEnd}`;
                if (serialized === this.lastSlideState) return;
                this.lastSlideState = serialized;

                this.dotnetObj.invokeMethodAsync('OnSetSlideState', hasOverflow, atStart, atEnd);
            } catch (e) {
                console.error('BitBlazorUI.Pivot.updateSlide:', e);
            }
        }

        public slide(forward: boolean) {
            try {
                const amount = Math.max(this.header.clientWidth * 0.75, 50);
                const direction = forward ? 1 : -1;
                const sign = this.isRtl ? -1 : 1;
                this.header.scrollBy({ left: direction * sign * amount, behavior: 'smooth' });
            } catch (e) {
                console.error('BitBlazorUI.Pivot.slide:', e);
            }
        }

        public dispose() {
            try {
                if (this.observer) {
                    this.observer.disconnect();
                    this.observer = null;
                }
                if (this.scrollHandler) {
                    this.header.removeEventListener('scroll', this.scrollHandler);
                    this.scrollHandler = null;
                }
            } catch (e) {
                console.error('BitBlazorUI.Pivot.dispose:', e);
            }
        }
    }
}
