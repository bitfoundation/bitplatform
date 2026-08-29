namespace BitBlazorUI {
    interface SplitterOptions {
        vertical: boolean;
        disabled: boolean;
        collapsible: boolean;
        collapsed: boolean;
        collapsedSize: number;
        keyboardStep: number;
        resetOnDoubleClick: boolean;
        notifyResize: boolean;
        percent: number | null;
        persistKey: string | null;
        persistSession: boolean;
    }

    interface SplitterEntry {
        id: string;
        root: HTMLElement;
        first: HTMLElement;
        gutter: HTMLElement;
        second: HTMLElement;
        options: SplitterOptions;
        controller: AbortController;
        dotnetObj: DotNetObject;
    }

    interface SplitterBounds {
        total: number;
        min: number;
        max: number;
        current: number;
    }

    export class Splitter {
        private static _entries: { [id: string]: SplitterEntry } = {};

        // The splitter a pointer is currently dragging. Only one can be, since the drag holds the pointer
        // capture, and it is what tells a disposal whether the chrome on the body is its own to clear.
        private static _dragging: string | null = null;

        // The custom properties a drag writes onto the root. They are listed once so that taking a snapshot
        // of them, restoring it and clearing it cannot fall out of step with each other.
        private static readonly SIZE_PROPERTIES = ['--first-panel', '--first-panel-grow', '--second-panel', '--second-panel-grow'];

        // Dragging the first panel below this share of its own minimum snaps it shut instead of leaving it
        // as a sliver nobody can read. It only applies to a splitter that was made collapsible; every other
        // one simply stops at the minimum.
        private static readonly SNAP_RATIO = 0.5;

        // The floor the snap falls back to on a panel that declares no minimum of its own, as a share of the
        // whole splitter, so that a collapsible panel can still be closed by dragging it to the edge.
        private static readonly SNAP_FALLBACK_RATIO = 0.05;

        // What a page key, or an arrow key held with shift, is worth in arrow key presses - the coarse step
        // every splitter offers beside the fine one.
        private static readonly COARSE_STEP_FACTOR = 10;

        public static setup(dotnetObj: DotNetObject,
                            root: HTMLElement,
                            first: HTMLElement,
                            gutter: HTMLElement,
                            second: HTMLElement,
                            vertical: boolean,
                            disabled: boolean,
                            collapsible: boolean,
                            collapsed: boolean,
                            collapsedSize: number,
                            keyboardStep: number,
                            resetOnDoubleClick: boolean,
                            notifyResize: boolean,
                            percent: number | null,
                            persistKey: string | null,
                            persistSession: boolean): string {
            if (!root || !first || !gutter || !second) return '';

            const entry: SplitterEntry = {
                id: Utils.uuidv4(),
                root, first, gutter, second,
                dotnetObj,
                controller: new AbortController(),
                options: {
                    vertical, disabled, collapsible, collapsed,
                    collapsedSize, keyboardStep, resetOnDoubleClick, notifyResize,
                    percent, persistKey, persistSession
                }
            };

            Splitter._entries[entry.id] = entry;

            const signal = entry.controller.signal;

            // The separator has to report where it stands from the very first render, and only the browser
            // knows that: the panels start at whatever the parameters and the content make of them.
            Splitter.reportPosition(entry);

            let dragging = false;
            let moved = false;
            let pending = false;
            let startPosition = 0;
            let startSize = 0;
            let latestSize = 0;
            let startPercent = 0;
            let appliedPercent = 0;
            let snapshot: (string)[] = [];
            let rafId = 0;

            const invoke = (method: string, ...args: any[]) => {
                if (signal.aborted) return;

                entry.dotnetObj.invokeMethodAsync(method, ...args)
                     .catch(e => console.error('BitBlazorUI.Splitter:', method, e));
            };

            const flushMove = () => {
                rafId = 0;
                pending = false;
                // Disposing aborts the listeners but not a frame that is already queued, and by the time
                // that frame runs the .NET reference behind it is gone with the component.
                if (signal.aborted || dragging === false) return;

                // The start of the resize is only reported once the pointer has actually moved. A press
                // that goes nowhere - the two that make up a double-click above all - is not a resize, and
                // reporting one would pin the splitter to the position it was already in.
                if (moved === false) {
                    moved = true;
                    invoke('HandleResizeStart', startPercent);
                }

                appliedPercent = Splitter.applySize(entry, latestSize);

                if (entry.options.notifyResize) {
                    invoke('HandleResize', appliedPercent);
                }
            };

            const endDrag = (cancelled: boolean) => {
                if (dragging === false) return;

                if (rafId !== 0) {
                    cancelAnimationFrame(rafId);
                    rafId = 0;
                }

                // A pointer let go of between two frames leaves the last move still waiting for the one
                // that never came. It is run here rather than dropped: releasing the gutter must not undo
                // the part of the drag the browser had not got round to drawing yet.
                if (cancelled === false && pending) {
                    flushMove();
                }

                dragging = false;
                pending = false;
                Splitter._dragging = null;

                Splitter.setDragChrome(entry, false);

                // Escape - and a pointer sequence the browser took away - puts the splitter back exactly as
                // the drag found it, down to whether it had a size of its own at all, so a drag started by
                // mistake costs nothing.
                if (cancelled) {
                    Splitter.restoreProperties(entry, snapshot);
                    Splitter.reportPosition(entry);
                    if (moved) invoke('HandleResizeCancel');
                    return;
                }

                if (moved === false) return;

                const collapse = Splitter.shouldSnapClosed(entry, latestSize);

                invoke('HandleResizeEnd', collapse ? appliedPercent : Splitter.applySize(entry, latestSize), collapse);
            };

            gutter.addEventListener('pointerdown', e => {
                // Secondary and middle buttons open context menus and start autoscroll rather than a drag.
                if (entry.options.disabled || e.button !== 0) return;

                // There is nothing to drag on a folded panel - it is held at its collapsed size rather than
                // at a share of the splitter - so the press opens it again instead, which is the only thing
                // a pointer could sensibly mean there.
                if (entry.options.collapsed) {
                    if (entry.options.collapsible === false) return;

                    e.preventDefault();
                    invoke('HandleToggleCollapse');
                    return;
                }

                const bounds = Splitter.getBounds(entry);
                if (bounds === null) return;

                dragging = true;
                moved = false;
                Splitter._dragging = entry.id;
                startPosition = entry.options.vertical ? e.clientY : e.clientX;
                startSize = bounds.current;
                latestSize = bounds.current;
                startPercent = Splitter.toPercent(bounds.current, bounds.total);
                appliedPercent = startPercent;
                snapshot = Splitter.captureProperties(entry);

                // The press must not select the text on either side of the gutter, and the gutter has to
                // keep the pointer even when it runs off the splitter, over an iframe or out of the window -
                // without the capture, a fast drag simply stops wherever the pointer left the element.
                e.preventDefault();
                gutter.focus();
                try { gutter.setPointerCapture(e.pointerId); } catch { }

                Splitter.setDragChrome(entry, true);
            }, { signal });

            gutter.addEventListener('pointermove', e => {
                if (dragging === false) return;

                const bounds = Splitter.getBounds(entry);
                if (bounds === null) return;

                const position = entry.options.vertical ? e.clientY : e.clientX;
                const delta = (position - startPosition) * Splitter.getAxisSign(entry);

                const size = Splitter.clamp(startSize + delta, bounds.min, bounds.max);
                if (moved === false && size === startSize) return;

                latestSize = size;
                pending = true;

                // Pointer moves arrive faster than the page is laid out, so the panels are only written to
                // once per frame; the last position seen in that frame is the one that counts.
                if (rafId === 0) {
                    rafId = requestAnimationFrame(flushMove);
                }
            }, { signal });

            gutter.addEventListener('pointerup', () => endDrag(false), { signal });
            gutter.addEventListener('lostpointercapture', () => endDrag(false), { signal });
            gutter.addEventListener('pointercancel', () => endDrag(true), { signal });

            gutter.addEventListener('dblclick', () => {
                if (entry.options.disabled || entry.options.resetOnDoubleClick === false) return;

                // The reset itself is left to .NET: a splitter whose position the page owns is not the
                // gutter's to give away, and what comes back is a sync call that puts the panels wherever
                // the page decided they belong.
                invoke('HandleReset');
            }, { signal });

            gutter.addEventListener('keydown', e => {
                if (entry.options.disabled) return;
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                if (e.key === 'Escape') {
                    if (dragging === false) return;
                    e.preventDefault();
                    endDrag(true);
                    return;
                }

                if (e.key === 'Enter') {
                    if (entry.options.collapsible === false) return;
                    e.preventDefault();
                    invoke('HandleToggleCollapse');
                    return;
                }

                const vertical = entry.options.vertical;
                let step = 0;

                if (vertical) {
                    if (e.key === 'ArrowUp') step = -1;
                    else if (e.key === 'ArrowDown') step = 1;
                } else {
                    if (e.key === 'ArrowLeft') step = -1;
                    else if (e.key === 'ArrowRight') step = 1;
                }

                // A page key is the coarse version of the same move, and so is an arrow key held with shift.
                let factor = e.shiftKey && step !== 0 ? Splitter.COARSE_STEP_FACTOR : 1;

                if (step === 0) {
                    if (e.key === 'PageUp') { step = -1; factor = Splitter.COARSE_STEP_FACTOR; }
                    else if (e.key === 'PageDown') { step = 1; factor = Splitter.COARSE_STEP_FACTOR; }
                }

                const home = e.key === 'Home';
                const end = e.key === 'End';

                if (step === 0 && home === false && end === false) return;

                // A folded panel is not at a share of the splitter, so moving that share would only change
                // where it lands once it is opened again - a key press that appears to do nothing.
                if (entry.options.collapsed) return;

                const bounds = Splitter.getBounds(entry);
                if (bounds === null) return;

                // The arrow keys move the gutter, so they must not also scroll the page under it.
                e.preventDefault();

                const target = home
                    ? bounds.min
                    : end
                        ? bounds.max
                        : bounds.current + step * factor * Splitter.getAxisSign(entry) * entry.options.keyboardStep;

                const size = Splitter.clamp(target, bounds.min, bounds.max);

                invoke('HandleResizeEnd', Splitter.applySize(entry, size), false);
            }, { signal });

            // Where the splitter was left the last time is only worth restoring once everything else is
            // wired up, and it is .NET that applies it: the position belongs to the component, which may
            // have a page holding it one way and refuse the restore outright.
            const stored = Splitter.readStored(entry);
            if (stored !== null && (stored.p !== null || stored.c)) {
                invoke('HandleRestore', stored.p, stored.c);
            }

            return entry.id;
        }

        public static update(id: string,
                             vertical: boolean,
                             disabled: boolean,
                             collapsible: boolean,
                             collapsed: boolean,
                             collapsedSize: number,
                             keyboardStep: number,
                             resetOnDoubleClick: boolean,
                             notifyResize: boolean,
                             percent: number | null,
                             persistKey: string | null,
                             persistSession: boolean): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            entry.options = {
                vertical, disabled, collapsible, collapsed,
                collapsedSize, keyboardStep, resetOnDoubleClick, notifyResize,
                percent, persistKey, persistSession
            };

            // The size of a panel is held as a share of the splitter rather than as a width or a height, so
            // turning the splitter on its side keeps the split where it was and only the reported position
            // of the separator - which the fold of a panel changes too - has to catch up.
            Splitter.reportPosition(entry);

            Splitter.writeStored(entry);
        }

        /// Puts the panels where .NET says they belong: at the given share of the splitter, or back on the
        /// sizes its parameters declare when there is no share to hold them at.
        public static sync(id: string, percent: number | null): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            if (percent === null || percent === undefined) {
                Splitter.clearSize(entry);
            } else {
                Splitter.applyPercent(entry, percent);
            }
        }

        public static dispose(id: string): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            // A splitter taken off the page in the middle of a drag would otherwise leave the body wearing
            // the resize cursor and unable to select text.
            if (Splitter._dragging === id) {
                Splitter.setDragChrome(entry, false);
                Splitter._dragging = null;
            }

            entry.controller.abort();
            entry.dotnetObj?.dispose();

            delete Splitter._entries[id];
        }



        // The two panels are measured rather than told: the minimum and the maximum of each of them can come
        // from the parameters, from a class the app put on the panel or from the content itself, and the
        // browser is the only place all three have already been resolved into one number.
        private static getBounds(entry: SplitterEntry): SplitterBounds | null {
            const vertical = entry.options.vertical;

            const firstRect = entry.first.getBoundingClientRect();
            const secondRect = entry.second.getBoundingClientRect();

            const firstSize = vertical ? firstRect.height : firstRect.width;
            const secondSize = vertical ? secondRect.height : secondRect.width;

            // The gutter is left out of it: what the two panels share between them is the room they are
            // actually laid out in, whatever the gutter takes and whatever padding sits around them.
            const total = firstSize + secondSize;
            if (total <= 0) return null;

            const firstStyle = getComputedStyle(entry.first);
            const secondStyle = getComputedStyle(entry.second);

            const firstMin = Splitter.parseSize(vertical ? firstStyle.minHeight : firstStyle.minWidth, total, 0);
            const firstMax = Splitter.parseSize(vertical ? firstStyle.maxHeight : firstStyle.maxWidth, total, total);
            const secondMin = Splitter.parseSize(vertical ? secondStyle.minHeight : secondStyle.minWidth, total, 0);
            const secondMax = Splitter.parseSize(vertical ? secondStyle.maxHeight : secondStyle.maxWidth, total, total);

            // Everything the second panel asks for is a bound on the first one from the other side, since
            // what one of them does not take the other one does.
            let min = Math.max(0, firstMin, total - secondMax);
            let max = Math.min(total, firstMax, total - secondMin);

            // Constraints that cannot all be met at once - a pair of minimums wider than the splitter - are
            // resolved in favour of the first panel rather than left as a range that runs backwards.
            if (max < min) max = min;

            return { total, min, max, current: firstSize };
        }

        // A drag that ends far enough inside the minimum of a collapsible panel closes it rather than
        // leaving it at a size the minimum would have refused anyway.
        private static shouldSnapClosed(entry: SplitterEntry, size: number): boolean {
            if (entry.options.collapsible === false) return false;

            const bounds = Splitter.getBounds(entry);
            if (bounds === null) return false;

            const threshold = bounds.min > 0
                ? bounds.min * Splitter.SNAP_RATIO
                : bounds.total * Splitter.SNAP_FALLBACK_RATIO;

            return size <= Math.max(threshold, entry.options.collapsedSize);
        }

        private static applySize(entry: SplitterEntry, size: number): number {
            const bounds = Splitter.getBounds(entry);

            return Splitter.applyPercent(entry, Splitter.toPercent(size, bounds === null ? 0 : bounds.total));
        }

        private static applyPercent(entry: SplitterEntry, percent: number): number {
            const value = Splitter.clamp(percent, 0, 100);

            // The split is written as a share rather than as a length, so it survives the container being
            // resized, and the second panel is released at the same time: two pinned panels and a gutter
            // between them cannot add up to the splitter at every width, one pinned panel and a filler can.
            entry.root.style.setProperty('--first-panel', value + '%');
            entry.root.style.setProperty('--first-panel-grow', '0');
            entry.root.style.setProperty('--second-panel', '0px');
            entry.root.style.setProperty('--second-panel-grow', '1');

            Splitter.setPositionAttributes(entry, value);

            return value;
        }

        private static clearSize(entry: SplitterEntry): void {
            Splitter.SIZE_PROPERTIES.forEach(name => entry.root.style.removeProperty(name));

            // The panels fall back to what the parameters make of them, which is only known after the layout
            // has been redone.
            requestAnimationFrame(() => {
                if (Splitter._entries[entry.id]) Splitter.reportPosition(entry);
            });
        }

        private static captureProperties(entry: SplitterEntry): string[] {
            return Splitter.SIZE_PROPERTIES.map(name => entry.root.style.getPropertyValue(name));
        }

        private static restoreProperties(entry: SplitterEntry, snapshot: string[]): void {
            Splitter.SIZE_PROPERTIES.forEach((name, index) => {
                const value = snapshot[index];

                if (value) {
                    entry.root.style.setProperty(name, value);
                } else {
                    entry.root.style.removeProperty(name);
                }
            });
        }

        private static reportPosition(entry: SplitterEntry): void {
            const bounds = Splitter.getBounds(entry);
            if (bounds === null) return;

            Splitter.setPositionAttributes(entry, Splitter.toPercent(bounds.current, bounds.total));
        }

        private static setPositionAttributes(entry: SplitterEntry, percent: number): void {
            // A separator that cannot be moved is not the widget form of the role, and the position of a
            // widget is not something the plain rule is allowed to report - including the one it was left
            // wearing from back when it could still be dragged.
            if (entry.options.disabled) {
                entry.gutter.removeAttribute('aria-valuenow');
                entry.gutter.removeAttribute('aria-valuetext');
                return;
            }

            // Rounded the same way .NET rounds the value it renders, so the two of them cannot disagree
            // over the position the separator reports.
            const rounded = (Math.round(percent * 100) / 100).toString();

            entry.gutter.setAttribute('aria-valuenow', rounded);
            entry.gutter.setAttribute('aria-valuetext', rounded + '%');
        }

        // The drag is dressed on the body rather than on the splitter: the pointer is captured, so it spends
        // most of the drag outside the element whose cursor and text selection would otherwise apply.
        private static setDragChrome(entry: SplitterEntry, dragging: boolean): void {
            const body = document.body;

            if (dragging) {
                entry.root.classList.add('bit-spl-drg');
                body.style.userSelect = 'none';
                body.style.webkitUserSelect = 'none';
                body.style.overscrollBehavior = 'none';
                body.style.cursor = entry.options.vertical ? 'row-resize' : 'col-resize';
            } else {
                entry.root.classList.remove('bit-spl-drg');
                body.style.userSelect = '';
                body.style.webkitUserSelect = '';
                body.style.overscrollBehavior = '';
                body.style.cursor = '';
            }
        }

        // Right to left turns the row around, so the first panel sits at the right of the splitter and a
        // drag to the right makes it smaller. A splitter laid out in a column is unaffected: writing
        // direction says nothing about which way is down.
        private static getAxisSign(entry: SplitterEntry): number {
            if (entry.options.vertical) return 1;

            return getComputedStyle(entry.root).direction === 'rtl' ? -1 : 1;
        }

        private static parseSize(value: string, total: number, fallback: number): number {
            if (!value || value === 'none' || value === 'auto') return fallback;

            const parsed = parseFloat(value);
            if (isNaN(parsed)) return fallback;

            // A minimum or a maximum given as a percentage is resolved against the room the two panels
            // share, which is the same thing every size here is measured against.
            return value.indexOf('%') >= 0 ? (total * parsed) / 100 : parsed;
        }

        private static toPercent(size: number, total: number): number {
            if (total <= 0) return 0;

            // Four decimals is finer than any display can show and keeps the value short enough to stay
            // readable in the style attribute it ends up in.
            return Math.round(Splitter.clamp((size / total) * 100, 0, 100) * 10000) / 10000;
        }

        private static clamp(value: number, min: number, max: number): number {
            return value < min ? min : value > max ? max : value;
        }

        // Web storage is not always there to be had - a private window, a browser told to keep no site data,
        // a page served from a file - and reaching for it throws rather than answering, so every call to it
        // is one the splitter can do without.
        private static getStore(entry: SplitterEntry): Storage | null {
            if (!entry.options.persistKey) return null;

            try {
                return entry.options.persistSession ? window.sessionStorage : window.localStorage;
            } catch {
                return null;
            }
        }

        private static readStored(entry: SplitterEntry): { p: number | null, c: boolean } | null {
            const store = Splitter.getStore(entry);
            if (store === null) return null;

            try {
                const raw = store.getItem(Splitter.storageKey(entry));
                if (!raw) return null;

                const parsed = JSON.parse(raw);
                const percent = typeof parsed?.p === 'number' ? Splitter.clamp(parsed.p, 0, 100) : null;

                return { p: percent, c: parsed?.c === true };
            } catch {
                // Whatever is under the key is not something this splitter wrote, so it is ignored rather
                // than allowed to take the layout down with it.
                return null;
            }
        }

        private static writeStored(entry: SplitterEntry): void {
            const store = Splitter.getStore(entry);
            if (store === null) return;

            try {
                store.setItem(Splitter.storageKey(entry), JSON.stringify({
                    p: entry.options.percent,
                    c: entry.options.collapsed
                }));
            } catch {
                // A storage that is full, or one the browser refuses to write to, is not a reason for the
                // splitter to stop working.
            }
        }

        private static storageKey(entry: SplitterEntry): string {
            return 'bit-splitter-' + entry.options.persistKey;
        }
    }
}
