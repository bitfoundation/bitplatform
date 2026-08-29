namespace BitBlazorUI {
    interface SplitterOptions {
        vertical: boolean;
        disabled: boolean;
        collapsible: boolean;
        collapsed: boolean;
        collapsedSize: number;
        keyboardStep: number;
        dragStep: number;
        snapSize: number;
        lazyResize: boolean;
        resetOnDoubleClick: boolean;
        notifyResize: boolean;
        notifyDoubleClick: boolean;
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
        preview: HTMLElement;
        options: SplitterOptions;
        controller: AbortController;
        dotnetObj: DotNetObject;
        observer: ResizeObserver | null;
        cancelDrag: () => void;
    }

    interface SplitterBounds {
        // Everything the splitter is laid out in, the gutter included. A share of the splitter is measured
        // against this, because the flex basis it is written back as is resolved against the same box.
        total: number;
        // What is left for the two panels once the gutter has taken its own width - the range a size can
        // actually be moved through.
        space: number;
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
        // room the panels have between them, so that a collapsible panel can still be closed by dragging it
        // to the edge.
        private static readonly SNAP_FALLBACK_RATIO = 0.05;

        // What a page key, or an arrow key held with shift, is worth in arrow key presses - the coarse step
        // every splitter offers beside the fine one.
        private static readonly COARSE_STEP_FACTOR = 10;

        // How long after a press on a folded gutter a second one is taken to be the other half of a double
        // click rather than a fold of its own, so that double-clicking a closed panel does not shut it
        // again the moment it opens.
        private static readonly DOUBLE_CLICK_WINDOW = 500;

        // How long the repeats of a held arrow key are gathered up for before the position they have reached
        // is reported. The panels move on every one of them; what is coalesced is only the trip to .NET,
        // which on a server-side circuit is a trip over the network.
        private static readonly KEY_REPEAT_DELAY = 150;

        public static setup(dotnetObj: DotNetObject,
                            root: HTMLElement,
                            first: HTMLElement,
                            gutter: HTMLElement,
                            second: HTMLElement,
                            preview: HTMLElement,
                            vertical: boolean,
                            disabled: boolean,
                            collapsible: boolean,
                            collapsed: boolean,
                            collapsedSize: number,
                            keyboardStep: number,
                            dragStep: number,
                            snapSize: number,
                            lazyResize: boolean,
                            resetOnDoubleClick: boolean,
                            notifyResize: boolean,
                            notifyDoubleClick: boolean,
                            percent: number | null,
                            persistKey: string | null,
                            persistSession: boolean): string {
            if (!root || !first || !gutter || !second) return '';

            // Everything the setup registers is found again by this id, and .NET takes an empty one for a
            // splitter that was never set up - so a browser that could not produce one is left with the plain
            // panels rather than with listeners nothing can reach or dispose.
            const id = Utils.uuidv4();
            if (!id) return '';

            const entry: SplitterEntry = {
                id,
                root, first, gutter, second, preview,
                dotnetObj,
                controller: new AbortController(),
                observer: null,
                cancelDrag: () => { },
                options: {
                    vertical, disabled, collapsible, collapsed,
                    collapsedSize, keyboardStep, dragStep, snapSize, lazyResize,
                    resetOnDoubleClick, notifyResize, notifyDoubleClick,
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
            let lastToggle = 0;
            let keyPercent = 0;
            let keyTimer = 0;
            let keyResizing = false;
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

                // A lazy splitter is not laid out again on every frame: the panels stay where they are and
                // only a line moves, so a drag costs one reflow at its end rather than one per frame - which
                // is what makes a panel full of heavy content draggable at all.
                if (entry.options.lazyResize) {
                    Splitter.showPreview(entry, latestSize);

                    if (entry.options.notifyResize) {
                        invoke('HandleResize', Splitter.previewPercent(entry, latestSize));
                    }

                    return;
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
                Splitter.hidePreview(entry);

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

            // The panels have already moved by the time this runs; what it reports is where they ended up.
            // A held key is gathered into one report every so often instead of one per repeat, and whatever
            // is still waiting when the key comes up is sent at once rather than kept back.
            const flushKeyResize = () => {
                if (keyTimer === 0) return;

                clearTimeout(keyTimer);
                keyTimer = 0;
                keyResizing = false;

                if (signal.aborted === false) invoke('HandleResizeEnd', keyPercent, false);
            };

            const notifyKeyResize = (percent: number, repeat: boolean, from: number) => {
                keyPercent = percent;

                // A move made with the keyboard is a resize like a drag is, so it is opened the same way,
                // with where the gutter stood before the key moved it. A run of repeats is one resize rather
                // than one per repeat, which is how the end of it is reported too.
                if (keyResizing === false) {
                    keyResizing = true;
                    invoke('HandleResizeStart', from);
                }

                // A press of its own is reported as it happens. Whatever a previous run of repeats had
                // waiting is dropped rather than sent: this press is where the gutter is now.
                if (repeat === false) {
                    if (keyTimer !== 0) {
                        clearTimeout(keyTimer);
                        keyTimer = 0;
                    }

                    keyResizing = false;
                    invoke('HandleResizeEnd', percent, false);
                    return;
                }

                if (keyTimer !== 0) return;

                keyTimer = setTimeout(() => {
                    keyTimer = 0;
                    keyResizing = false;
                    if (signal.aborted === false) invoke('HandleResizeEnd', keyPercent, false);
                }, Splitter.KEY_REPEAT_DELAY);
            };

            entry.cancelDrag = () => endDrag(true);

            gutter.addEventListener('pointerdown', e => {
                // Secondary and middle buttons open context menus and start autoscroll rather than a drag.
                if (entry.options.disabled || e.button !== 0) return;

                // There is nothing to drag on a folded panel - it is held at its collapsed size rather than
                // at a share of the splitter - so the press opens it again instead, which is the only thing
                // a pointer could sensibly mean there.
                if (entry.options.collapsed) {
                    if (entry.options.collapsible === false) return;

                    e.preventDefault();

                    // The second press of a double-click is not a fold of its own: without this, double
                    // clicking a closed panel would open it and shut it again in the same gesture.
                    if (e.timeStamp - lastToggle < Splitter.DOUBLE_CLICK_WINDOW) return;

                    lastToggle = e.timeStamp;
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
                // Taking the focus must not scroll the gutter into view as well: a page that jumps under a
                // drag has moved the very thing the pointer is aiming at.
                gutter.focus({ preventScroll: true });
                try { gutter.setPointerCapture(e.pointerId); } catch { }

                Splitter.setDragChrome(entry, true);
            }, { signal });

            gutter.addEventListener('pointermove', e => {
                if (dragging === false) return;

                const bounds = Splitter.getBounds(entry);
                if (bounds === null) return;

                const position = entry.options.vertical ? e.clientY : e.clientX;
                const delta = (position - startPosition) * Splitter.getAxisSign(entry);

                const size = Splitter.clamp(Splitter.applyDragStep(entry, startSize + delta), bounds.min, bounds.max);
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

            // A menu opening over the gutter takes the drag away from the pointer, so it is put back where
            // it started rather than left running against a pointer nobody is reporting any more.
            gutter.addEventListener('contextmenu', () => endDrag(true), { signal });

            // A window handed to something else altogether will never report the release either, but what
            // was dragged up to that point is kept: the alternative is a splitter that undoes a resize
            // because the reader looked at another window in the middle of it.
            window.addEventListener('blur', () => endDrag(false), { signal });

            gutter.addEventListener('dblclick', () => {
                if (entry.options.disabled) return;

                if (entry.options.notifyDoubleClick) {
                    invoke('HandleGutterDoubleClick');
                }

                // A folded panel has no size of its own to hand back to its parameters, and the press that
                // opened it has already been acted on.
                if (entry.options.collapsed || entry.options.resetOnDoubleClick === false) return;

                // The reset itself is left to .NET: a splitter whose position the page owns is not the
                // gutter's to give away, and what comes back is a sync call that puts the panels wherever
                // the page decided they belong.
                invoke('HandleReset');
            }, { signal });

            gutter.addEventListener('keydown', e => {
                if (entry.options.disabled) return;
                if (e.altKey || e.metaKey) return;

                // Control with an arrow key folds the panel away and opens it again, which is the shortcut
                // a splitter is expected to answer to beside Enter.
                if (e.ctrlKey) {
                    if (entry.options.collapsible === false) return;

                    const towardsStart = entry.options.vertical ? e.key === 'ArrowUp' : e.key === 'ArrowLeft';
                    const towardsEnd = entry.options.vertical ? e.key === 'ArrowDown' : e.key === 'ArrowRight';

                    if (towardsStart === false && towardsEnd === false) return;

                    e.preventDefault();

                    // Towards the start of the splitter closes the panel and away from it opens it again,
                    // whichever way round the writing direction puts the two of them.
                    const collapse = Splitter.getAxisSign(entry) > 0 ? towardsStart : towardsEnd;

                    if (collapse !== entry.options.collapsed) invoke('HandleToggleCollapse');
                    return;
                }

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

                const size = Splitter.clamp(Splitter.applyDragStep(entry, target), bounds.min, bounds.max);

                notifyKeyResize(Splitter.applySize(entry, size), e.repeat, Splitter.toPercent(bounds.current, bounds.total));
            }, { signal });

            gutter.addEventListener('keyup', () => flushKeyResize(), { signal });
            gutter.addEventListener('blur', () => flushKeyResize(), { signal });

            // The share a panel pinned to a length takes up changes with every resize of the container
            // around it, and so does the range its constraints leave it - so what the separator reports is
            // measured again rather than left at whatever it was when the splitter was first laid out.
            if (typeof ResizeObserver !== 'undefined') {
                let observerRaf = 0;

                entry.observer = new ResizeObserver(() => {
                    if (dragging || observerRaf !== 0) return;

                    observerRaf = requestAnimationFrame(() => {
                        observerRaf = 0;
                        if (signal.aborted || dragging) return;

                        Splitter.reportPosition(entry);
                    });
                });

                entry.observer.observe(entry.root);
            }

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
                             dragStep: number,
                             snapSize: number,
                             lazyResize: boolean,
                             resetOnDoubleClick: boolean,
                             notifyResize: boolean,
                             notifyDoubleClick: boolean,
                             percent: number | null,
                             persistKey: string | null,
                             persistSession: boolean): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            entry.options = {
                vertical, disabled, collapsible, collapsed,
                collapsedSize, keyboardStep, dragStep, snapSize, lazyResize,
                resetOnDoubleClick, notifyResize, notifyDoubleClick,
                percent, persistKey, persistSession
            };

            // A splitter that stops being resizable in the middle of a drag is put back where the drag
            // found it: going on moving a layout the page has just closed to negotiation would be the one
            // thing closing it was meant to prevent.
            if (disabled && Splitter._dragging === id) {
                entry.cancelDrag();
            }

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

            entry.observer?.disconnect();
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
            const gutterRect = entry.gutter.getBoundingClientRect();
            const secondRect = entry.second.getBoundingClientRect();

            const firstSize = vertical ? firstRect.height : firstRect.width;
            const gutterSize = vertical ? gutterRect.height : gutterRect.width;
            const secondSize = vertical ? secondRect.height : secondRect.width;

            // What the two panels have to share is the room left once the gutter has taken its own, while a
            // share of the splitter is measured against all three together: the percentage a size is written
            // back as is resolved against the box the gutter sits in too, so measuring one way and writing
            // the other would move the panel a little further than the pointer with every drag.
            const space = firstSize + secondSize;
            const total = space + gutterSize;
            if (space <= 0) return null;

            const firstStyle = getComputedStyle(entry.first);
            const secondStyle = getComputedStyle(entry.second);

            const firstMin = Splitter.parseSize(vertical ? firstStyle.minHeight : firstStyle.minWidth, total, 0);
            const firstMax = Splitter.parseSize(vertical ? firstStyle.maxHeight : firstStyle.maxWidth, total, space);
            const secondMin = Splitter.parseSize(vertical ? secondStyle.minHeight : secondStyle.minWidth, total, 0);
            const secondMax = Splitter.parseSize(vertical ? secondStyle.maxHeight : secondStyle.maxWidth, total, space);

            // Everything the second panel asks for is a bound on the first one from the other side, since
            // what one of them does not take the other one does.
            let min = Math.max(0, firstMin, space - secondMax);
            let max = Math.min(space, firstMax, space - secondMin);

            // Constraints that cannot all be met at once - a pair of minimums wider than the splitter - are
            // resolved in favour of the first panel rather than left as a range that runs backwards.
            if (max < min) max = min;

            return { total, space, min, max, current: firstSize };
        }

        // A splitter given a step of its own moves from one multiple of it to the next rather than to
        // wherever the pointer happens to be, which is what lines a panel up with a grid or a column.
        private static applyDragStep(entry: SplitterEntry, size: number): number {
            const step = entry.options.dragStep;

            return step > 0 ? Math.round(size / step) * step : size;
        }

        // A drag that ends far enough inside the minimum of a collapsible panel closes it rather than
        // leaving it at a size the minimum would have refused anyway.
        private static shouldSnapClosed(entry: SplitterEntry, size: number): boolean {
            if (entry.options.collapsible === false) return false;

            // A snap distance of its own is the whole answer: the panel closes within that many pixels of
            // the start of the splitter and nowhere else.
            if (entry.options.snapSize > 0) return size <= entry.options.snapSize;

            const bounds = Splitter.getBounds(entry);
            if (bounds === null) return false;

            const threshold = bounds.min > 0
                ? bounds.min * Splitter.SNAP_RATIO
                : bounds.space * Splitter.SNAP_FALLBACK_RATIO;

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

        // The line a lazy drag moves instead of the panels. It is placed where the gutter would end up, in
        // the same box the panels are laid out in, and it is taken down again the moment the drag is over -
        // whether the drag was kept or thrown away.
        private static showPreview(entry: SplitterEntry, size: number): void {
            if (!entry.preview) return;

            const offset = Math.max(0, size) + 'px';

            entry.preview.classList.add('bit-spl-prv-on');

            if (entry.options.vertical) {
                entry.preview.style.insetBlockStart = offset;
                entry.preview.style.insetInlineStart = '';
            } else {
                // The inline start of the splitter is its right edge in a right-to-left page, which is the
                // same edge the first panel is measured from, so the offset needs no sign of its own.
                entry.preview.style.insetInlineStart = offset;
                entry.preview.style.insetBlockStart = '';
            }
        }

        private static hidePreview(entry: SplitterEntry): void {
            if (!entry.preview) return;

            entry.preview.classList.remove('bit-spl-prv-on');
        }

        private static previewPercent(entry: SplitterEntry, size: number): number {
            const bounds = Splitter.getBounds(entry);

            return Splitter.toPercent(size, bounds === null ? 0 : bounds.total);
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
        // most of the drag outside the element whose cursor and text selection would otherwise apply. It is
        // a class rather than a handful of inline styles, so a page that had a cursor or a user-select of
        // its own on the body still has it once the drag is over - and so that a stylesheet can reach the
        // one thing a captured pointer still cannot cross, the iframes on the page.
        private static setDragChrome(entry: SplitterEntry, dragging: boolean): void {
            const body = document.body;

            if (dragging) {
                entry.root.classList.add('bit-spl-drg');
                body.classList.add('bit-spl-drg-bdy');
                body.classList.toggle('bit-spl-drg-vrt', entry.options.vertical);
            } else {
                entry.root.classList.remove('bit-spl-drg');
                body.classList.remove('bit-spl-drg-bdy');
                body.classList.remove('bit-spl-drg-vrt');
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

            // A minimum or a maximum given as a percentage is resolved against the box the panels are laid
            // out in, which is the whole splitter rather than the room left over between the gutter and its
            // two edges.
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
