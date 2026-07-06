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
            isVertical: boolean,
            dotnetObj: DotNetObject) {
            if (!header) return;

            Pivot.dispose(id);

            const instance = new PivotInstance(id, header, moreButton, isMenu, isSlide, isRtl, isVertical, dotnetObj);
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
        private isVertical: boolean;
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
            isVertical: boolean,
            dotnetObj: DotNetObject) {
            this.id = id;
            this.header = header;
            this.moreButton = moreButton;
            this.isMenu = isMenu;
            this.isSlide = isSlide;
            this.isRtl = isRtl;
            this.isVertical = isVertical;
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

        private outerSize(el: HTMLElement): number {
            const style = window.getComputedStyle(el);
            if (this.isVertical) {
                const marginTop = parseFloat(style.marginTop) || 0;
                const marginBottom = parseFloat(style.marginBottom) || 0;
                return el.offsetHeight + marginTop + marginBottom;
            }
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

                const containerSize = this.isVertical ? this.header.clientHeight : this.header.clientWidth;

                let total = 0;
                items.forEach(it => (total += this.outerSize(it)));

                let overflowIndexes: number[] = [];

                if (total > containerSize + 1) {
                    if (this.moreButton) this.moreButton.style.display = '';
                    const moreSize = this.moreButton ? this.outerSize(this.moreButton) : 0;
                    const available = containerSize - moreSize;

                    let used = 0;
                    items.forEach((it, i) => {
                        used += this.outerSize(it);
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
                let atStart: boolean;
                let atEnd: boolean;
                let hasOverflow: boolean;

                if (this.isVertical) {
                    const scrollTop = this.header.scrollTop;
                    const maxScroll = this.header.scrollHeight - this.header.clientHeight;
                    hasOverflow = maxScroll > 1;
                    atStart = scrollTop <= 1;
                    atEnd = scrollTop >= maxScroll - 1;
                } else {
                    const scrollLeft = this.header.scrollLeft;
                    const maxScroll = this.header.scrollWidth - this.header.clientWidth;
                    hasOverflow = maxScroll > 1;

                    if (this.isRtl) {
                        const abs = Math.abs(scrollLeft);
                        atStart = abs <= 1;
                        atEnd = abs >= maxScroll - 1;
                    } else {
                        atStart = scrollLeft <= 1;
                        atEnd = scrollLeft >= maxScroll - 1;
                    }
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
                const direction = forward ? 1 : -1;
                if (this.isVertical) {
                    const amount = Math.max(this.header.clientHeight * 0.75, 50);
                    this.header.scrollBy({ top: direction * amount, behavior: 'smooth' });
                } else {
                    const amount = Math.max(this.header.clientWidth * 0.75, 50);
                    const sign = this.isRtl ? -1 : 1;
                    this.header.scrollBy({ left: direction * sign * amount, behavior: 'smooth' });
                }
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
