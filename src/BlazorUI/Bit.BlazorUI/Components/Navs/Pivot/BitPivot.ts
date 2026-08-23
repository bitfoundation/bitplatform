namespace BitBlazorUI {
    export class Pivot {
        private static _instances: Record<string, PivotInstance> = {};
        private static _keyHandlers: Record<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }> = {};

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

        // Returns the indexes of the items that ended up folded away, so that a caller which has to act
        // on the new fold right away (the overflow menu handing the focus back) reads it from the call
        // itself rather than from the OnSetOverflowItems callback, which lands a turn later. Null when
        // there is no instance, or none that folds anything, and the caller keeps what it has.
        public static refresh(id: string): number[] | null {
            const instance = Pivot._instances[id];
            if (!instance) return null;
            return instance.update();
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
        // Only the header is scrolled: the scrollIntoView of the item takes every scrollable ancestor
        // of it along, so a pivot sitting below the fold would yank the whole page down to itself as
        // soon as it is rendered on the tab it starts on.
        public static scrollToItem(element: HTMLElement) {
            if (!element || !element.closest) return;

            try {
                const header = element.closest('.bit-pvt-hct') as HTMLElement | null;
                if (!header || !header.scrollBy) return;

                const item = element.getBoundingClientRect();
                const view = header.getBoundingClientRect();

                const top = Pivot.scrollDelta(item.top, item.bottom, view.top, view.bottom);
                const left = Pivot.scrollDelta(item.left, item.right, view.left, view.right);

                if (top === 0 && left === 0) return;

                const reduced = window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches;
                header.scrollBy({ top, left, behavior: reduced ? 'auto' : 'smooth' });
            } catch (e) {
                console.error('BitBlazorUI.Pivot.scrollToItem:', e);
            }
        }

        // How far the header has to move along one axis for the item to sit inside it, which is nothing
        // at all while the item already fits: the nearest edge is the one it is brought to, the way the
        // nearest of scrollIntoView does it.
        private static scrollDelta(start: number, end: number, viewStart: number, viewEnd: number): number {
            if (start < viewStart) return start - viewStart;

            if (end > viewEnd) return end - viewEnd;

            return 0;
        }

        // Suppresses the default behavior (scrolling the page) of the keys the header navigates with.
        // The keyboard logic itself runs in the Blazor keydown handler, which cannot decide to
        // preventDefault per key: its flag is applied by the render that follows the key, so the first
        // press of each of them would scroll the page anyway and the flag it left standing would then
        // swallow the Tab that takes the focus out of the header.
        public static setupKeys(headerId: string, keys: string[]) {
            Pivot.disposeKeys(headerId);

            const header = document.getElementById(headerId);
            if (!header) return;

            const handler = (e: KeyboardEvent) => {
                if (keys.indexOf(e.key) === -1) return;

                // a modified key is a shortcut of the browser or of the operating system rather than one
                // of the keys the header takes.
                if (e.shiftKey || e.ctrlKey || e.altKey || e.metaKey) return;

                // only the tab itself: a header template can hold something interactive of its own, and
                // the space typed into an input there belongs to the input rather than to the tablist.
                // the More button keeps its own space as well, which is what raises the click that opens
                // its menu.
                const target = e.target as HTMLElement | null;
                if (!target || !target.matches || !target.matches('.bit-pvti:not(.bit-pvt-mor)')) return;

                e.preventDefault();
            };

            header.addEventListener('keydown', handler);

            Pivot._keyHandlers[headerId] = { element: header, handler };
        }

        public static disposeKeys(headerId: string) {
            const entry = Pivot._keyHandlers[headerId];
            if (!entry) return;

            entry.element.removeEventListener('keydown', entry.handler);
            delete Pivot._keyHandlers[headerId];
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
        private wheelHandler: ((e: WheelEvent) => void) | null = null;
        private dragStartHandler: ((e: DragEvent) => void) | null = null;
        private slideTimer: number | null = null;
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
                    const throttled = Utils.throttle(() => this.updateSlide(), 100) as () => void;
                    // the throttle only calls on its leading edge, so a scroll that comes to rest inside
                    // its window would otherwise leave the buttons reporting the state the header was in
                    // on the way rather than the one it ended up in.
                    this.scrollHandler = () => { throttled(); this.scheduleSlideUpdate(); };
                    this.header.addEventListener('scroll', this.scrollHandler, { passive: true });

                    if (!this.isVertical) {
                        // the header of the Slide behavior scrolls sideways and hides its scrollbar, so a
                        // wheel over it moves it along instead of scrolling the page past the very header
                        // the user is trying to get through. the page keeps the wheel at either end of it.
                        this.wheelHandler = (e: WheelEvent) => {
                            try {
                                if (e.ctrlKey || e.deltaY === 0 || Math.abs(e.deltaX) > Math.abs(e.deltaY)) return;

                                const maxScroll = this.header.scrollWidth - this.header.clientWidth;
                                if (maxScroll <= 1) return;

                                const abs = Math.abs(this.header.scrollLeft);
                                const forward = e.deltaY > 0;
                                if (forward ? abs >= maxScroll - 1 : abs <= 1) return;

                                e.preventDefault();
                                this.header.scrollBy({ left: (this.isRtl ? -1 : 1) * e.deltaY, behavior: 'auto' });
                            } catch { }
                        };
                        this.header.addEventListener('wheel', this.wheelHandler, { passive: false });
                    }
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

        public update(): number[] | null {
            if (this.disposed) return null;

            // the element gets removed from the dom without the component being able to dispose
            // this instance in some scenarios (like a disconnected circuit), so it disposes itself.
            if (this.header.isConnected === false) {
                Pivot.disposeInstance(this.id, this);
                return null;
            }

            const overflowIndexes = this.isMenu ? this.updateMenu() : null;
            if (this.isSlide) this.updateSlide();

            return overflowIndexes;
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

        private updateMenu(): number[] | null {
            try {
                const items = this.getItems();

                // reset everything to its natural state before measuring. the overflowed items are
                // hidden with a class of their own rather than with an inline display, which would
                // otherwise wipe out the display the component itself puts on a hidden item.
                items.forEach(it => it.classList.remove(PivotInstance.hiddenClass));
                if (this.moreButton) this.moreButton.style.display = 'none';

                const containerSize = this.isVertical ? this.header.clientHeight : this.header.clientWidth;

                // an item the component itself collapses takes part in neither the measuring nor the
                // menu, but keeps its place in the index so that the indexes still address the .NET
                // items. one that it only makes invisible still holds the room it takes in the header,
                // so it is measured along with the rest of them.
                const shown = items
                    .map((it, i) => {
                        const style = window.getComputedStyle(it);
                        return { it, i, display: style.display, visibility: style.visibility };
                    })
                    .filter(x => x.display !== 'none');

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
                        if (used <= available) return;

                        // an invisible item is not something the menu can offer either, and folding it
                        // away would take the room it holds in the header with it, so it is left alone.
                        if (x.visibility === 'hidden') return;

                        x.it.classList.add(PivotInstance.hiddenClass);
                        overflowIndexes.push(x.i);
                    });

                    // if nothing actually overflowed (e.g. only the more button didn't fit) hide it.
                    if (overflowIndexes.length === 0 && this.moreButton) {
                        this.moreButton.style.display = 'none';
                    }
                }

                const serialized = overflowIndexes.join(',');
                if (serialized !== this.lastOverflow) {
                    this.lastOverflow = serialized;

                    this.invoke('OnSetOverflowItems', overflowIndexes);
                }

                return overflowIndexes;
            } catch (e) {
                console.error('BitBlazorUI.Pivot.updateMenu:', e);
                return null;
            }
        }

        // A final read of the header once it has come to rest, which is what the leading-edge throttle
        // of the scroll handler cannot give on its own.
        private scheduleSlideUpdate() {
            if (this.disposed) return;

            if (this.slideTimer !== null) {
                clearTimeout(this.slideTimer);
            }

            this.slideTimer = setTimeout(() => {
                this.slideTimer = null;
                if (this.disposed) return;
                this.updateSlide();
            }, 150) as unknown as number;
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

                // the smooth scroll above lands after the last scroll event the throttle let through,
                // so the buttons are asked to read the header again once it has come to rest.
                this.scheduleSlideUpdate();
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
                if (this.wheelHandler) {
                    this.header.removeEventListener('wheel', this.wheelHandler);
                    this.wheelHandler = null;
                }
                if (this.slideTimer !== null) {
                    clearTimeout(this.slideTimer);
                    this.slideTimer = null;
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
