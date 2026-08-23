namespace BitBlazorUI {
    export class Pivot {
        private static _instances: Record<string, PivotInstance> = {};

        public static setup(
            id: string,
            header: HTMLElement,
            moreButton: HTMLElement | null,
            isMenu: boolean,
            isSlide: boolean,
            isReorderable: boolean,
            isRtl: boolean,
            isVertical: boolean,
            dotnetObj: DotNetObject) {
            if (!header) return;

            Pivot.dispose(id);

            // the id of the component changes between renders in some scenarios, so any leftover
            // instance of the same header element gets disposed here to not keep observing it.
            Object.keys(Pivot._instances).forEach(key => {
                if (Pivot._instances[key].isHeader(header)) {
                    Pivot.dispose(key);
                }
            });

            const instance = new PivotInstance(id, header, moreButton, isMenu, isSlide, isReorderable, isRtl, isVertical, dotnetObj);
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

        // The order the tabs are declared in, which is the order they are laid out in but not the order
        // the component registers them in: an item rendered after the first pass is created last.
        public static getItemsOrder(header: HTMLElement): string[] {
            if (!header) return [];

            try {
                return Array.from(header.querySelectorAll<HTMLElement>('.bit-pvti:not(.bit-pvt-mor)')).map(el => el.id);
            } catch (e) {
                console.error('BitBlazorUI.Pivot.getItemsOrder:', e);
                return [];
            }
        }

        // Brings a tab back into view after a selection or a focus move that did not come from a click
        // on it (the keyboard, the bound key, the overflow menu). Works off the element itself rather
        // than an instance, since the Scroll behavior sets up no instance at all.
        public static scrollToItem(element: HTMLElement) {
            if (!element || !element.scrollIntoView) return;

            try {
                const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches;
                element.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: reduced ? 'auto' : 'smooth' });
            } catch (e) {
                console.error('BitBlazorUI.Pivot.scrollToItem:', e);
            }
        }

        public static dispose(id: string) {
            const instance = Pivot._instances[id];
            if (!instance) return;
            instance.dispose();
            delete Pivot._instances[id];
        }

        public static disposeInstance(id: string, instance: PivotInstance) {
            // a newer instance may already be registered with this id, so only the
            // instance itself gets disposed here to not take down its replacement.
            if (Pivot._instances[id] === instance) {
                Pivot.dispose(id);
            } else {
                instance.dispose();
            }
        }
    }

    class PivotInstance {
        public static readonly hiddenClass = 'bit-pvt-ovh';

        private id: string;
        private header: HTMLElement;
        private moreButton: HTMLElement | null;
        private isMenu: boolean;
        private isSlide: boolean;
        private isReorderable: boolean;
        private isRtl: boolean;
        private isVertical: boolean;
        private dotnetObj: DotNetObject;
        private disposed: boolean = false;
        private observer: ResizeObserver | null = null;
        private scrollHandler: (() => void) | null = null;
        private dragStartHandler: ((e: DragEvent) => void) | null = null;
        private lastOverflow: string = '';
        private lastSlideState: string = '';

        constructor(
            id: string,
            header: HTMLElement,
            moreButton: HTMLElement | null,
            isMenu: boolean,
            isSlide: boolean,
            isReorderable: boolean,
            isRtl: boolean,
            isVertical: boolean,
            dotnetObj: DotNetObject) {
            this.id = id;
            this.header = header;
            this.moreButton = moreButton;
            this.isMenu = isMenu;
            this.isSlide = isSlide;
            this.isReorderable = isReorderable;
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

                if (this.isReorderable) {
                    // a drag whose data transfer carries nothing never starts in some browsers, and
                    // the handler of the component itself has no way of filling that in from C#.
                    this.dragStartHandler = (e: DragEvent) => {
                        try {
                            if (!e.dataTransfer) return;
                            e.dataTransfer.effectAllowed = 'move';
                            if (e.dataTransfer.types.length === 0) {
                                e.dataTransfer.setData('text/plain', '');
                            }
                        } catch { }
                    };
                    this.header.addEventListener('dragstart', this.dragStartHandler);
                }
            } catch (e) {
                console.error('BitBlazorUI.Pivot.start:', e);
            }

            this.update();
        }

        public update() {
            if (this.disposed) return;

            // the element gets removed from the dom without the component being able to dispose
            // this instance in some scenarios (like a disconnected circuit), so it disposes itself.
            if (this.header.isConnected === false) {
                Pivot.disposeInstance(this.id, this);
                return;
            }

            if (this.isMenu) this.updateMenu();
            if (this.isSlide) this.updateSlide();
        }

        public isHeader(header: HTMLElement) {
            return this.header === header;
        }

        private invoke(method: string, ...args: any[]) {
            if (this.disposed) return;

            try {
                // the dotnet object reference gets disposed before this instance in some scenarios,
                // so the rejection is handled here to not end up as an unhandled promise rejection.
                this.dotnetObj.invokeMethodAsync(method, ...args).catch(() => Pivot.disposeInstance(this.id, this));
            } catch (e) {
                Pivot.disposeInstance(this.id, this);
            }
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

                // reset everything to its natural state before measuring. the overflowed items are
                // hidden with a class of their own rather than with an inline display, which would
                // otherwise wipe out the display the component itself puts on a hidden item.
                items.forEach(it => it.classList.remove(PivotInstance.hiddenClass));
                if (this.moreButton) this.moreButton.style.display = 'none';

                const containerSize = this.isVertical ? this.header.clientHeight : this.header.clientWidth;

                // an item the component itself hides takes part in neither the measuring nor the menu,
                // but keeps its place in the index so that the indexes still address the .NET items.
                const shown = items
                    .map((it, i) => ({ it, i }))
                    .filter(x => window.getComputedStyle(x.it).display !== 'none');

                let total = 0;
                shown.forEach(x => (total += this.outerSize(x.it)));

                let overflowIndexes: number[] = [];

                if (total > containerSize + 1) {
                    if (this.moreButton) this.moreButton.style.display = '';
                    const moreSize = this.moreButton ? this.outerSize(this.moreButton) : 0;
                    const available = containerSize - moreSize;

                    let used = 0;
                    shown.forEach(x => {
                        used += this.outerSize(x.it);
                        if (used > available) {
                            x.it.classList.add(PivotInstance.hiddenClass);
                            overflowIndexes.push(x.i);
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

                this.invoke('OnSetOverflowItems', overflowIndexes);
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

                this.invoke('OnSetSlideState', hasOverflow, atStart, atEnd);
            } catch (e) {
                console.error('BitBlazorUI.Pivot.updateSlide:', e);
            }
        }

        public slide(forward: boolean) {
            if (this.disposed) return;

            try {
                const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches;
                const behavior: ScrollBehavior = reduced ? 'auto' : 'smooth';
                const direction = forward ? 1 : -1;
                if (this.isVertical) {
                    const amount = Math.max(this.header.clientHeight * 0.75, 50);
                    this.header.scrollBy({ top: direction * amount, behavior });
                } else {
                    const amount = Math.max(this.header.clientWidth * 0.75, 50);
                    const sign = this.isRtl ? -1 : 1;
                    this.header.scrollBy({ left: direction * sign * amount, behavior });
                }
            } catch (e) {
                console.error('BitBlazorUI.Pivot.slide:', e);
            }
        }

        public dispose() {
            this.disposed = true;

            try {
                // the items that were folded away are handed back to the header: the overflow
                // behavior may be switching to one that shows all of them again.
                this.getItems().forEach(it => it.classList.remove(PivotInstance.hiddenClass));
                if (this.moreButton) this.moreButton.style.display = 'none';

                if (this.observer) {
                    this.observer.disconnect();
                    this.observer = null;
                }
                if (this.scrollHandler) {
                    this.header.removeEventListener('scroll', this.scrollHandler);
                    this.scrollHandler = null;
                }
                if (this.dragStartHandler) {
                    this.header.removeEventListener('dragstart', this.dragStartHandler);
                    this.dragStartHandler = null;
                }
            } catch (e) {
                console.error('BitBlazorUI.Pivot.dispose:', e);
            }
        }
    }
}
