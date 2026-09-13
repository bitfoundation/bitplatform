namespace BitBlazorUI {
    interface SplitterOptions {
        vertical: boolean;
        disabled: boolean;
        collapsible: boolean;
        collapseSecond: boolean;
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

        // The splitters a pointer is currently dragging. There is normally one, but a second finger can take
        // hold of a second splitter, so the chrome on the body is only cleared once the last of them is done
        // with it. It is also what tells a disposal whether that chrome is its own to clear.
        private static _dragging: { [id: string]: boolean } = {};

        // The custom properties a drag writes onto the root. They are listed once so that taking a snapshot
        // of them, restoring it and clearing it cannot fall out of step with each other.
        private static readonly SIZE_PROPERTIES = ['--first-panel', '--first-panel-grow', '--second-panel', '--second-panel-grow'];

        // The control on the gutter that folds the panel away. A press on it is not a drag of the gutter it
        // sits on, and the two presses that make it up are not a double-click on the gutter either.
        private static readonly COLLAPSE_BUTTON_SELECTOR = '.bit-spl-cbt';

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
                            options: SplitterOptions): string {
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
                options
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

            // Where the pointer asked the gutter to go, before the constraints of the panels had their say.
            // The panels are never laid out there; what it is kept for is the snap, which has to be able to
            // tell a drag that came to rest on the minimum from one that went on pushing past it, and the
            // clamped size is the same number in both cases.
            let latestRaw = 0;
            let startPercent = 0;
            let appliedPercent = 0;
            let lastToggle = 0;
            let keyPercent = 0;
            let keyTimer = 0;
            let keyResizing = false;
            let snapshot: (string)[] = [];
            let rafId = 0;

            // Measuring the panels is what forces the browser to lay the page out, so a drag takes one
            // measurement per pointer move and everything that runs off the back of it - the frame that
            // writes the new size, the snap that decides whether the panel closes - is handed the same one
            // rather than asking for its own. What cannot change under a drag is measured once at its start.
            let latestBounds: SplitterBounds | null = null;
            let axisSign = 1;
            let previewOffset = 0;

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
                    Splitter.showPreview(entry, latestSize, previewOffset);

                    if (entry.options.notifyResize) {
                        invoke('HandleResize', Splitter.previewPercent(latestSize, latestBounds));
                    }

                    return;
                }

                const applied = Splitter.applySize(entry, latestSize, latestBounds);
                if (applied === null) return;

                appliedPercent = applied;

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
                delete Splitter._dragging[entry.id];

                Splitter.setDragChrome(entry, false);
                Splitter.hidePreview(entry);

                // Escape - and a pointer sequence the browser took away - puts the splitter back exactly as
                // the drag found it, down to whether it had a size of its own at all, so a drag started by
                // mistake costs nothing.
                if (cancelled) {
                    Splitter.restoreProperties(entry, snapshot);
                    Splitter.reportPosition(entry);
                    if (moved) invoke('HandleResizeCancel', startPercent);
                    return;
                }

                if (moved === false) return;

                // Nothing could be measured, so there is no position to report as the one the drag reached -
                // and the panels were never moved to one either. It is the same outcome as an abandoned drag.
                if (latestBounds === null) {
                    invoke('HandleResizeCancel', startPercent);
                    return;
                }

                const collapse = Splitter.shouldSnapClosed(entry, latestRaw, latestBounds);

                // A drag that closes the panel is still reported at the position it reached, whether or not
                // the panels were laid out there along the way - a lazy splitter and an eager one describe
                // the same gesture the same way.
                const percent = collapse
                    ? Splitter.toPercent(latestSize, latestBounds.total)
                    : Splitter.applySize(entry, latestSize, latestBounds);

                invoke('HandleResizeEnd', percent === null ? appliedPercent : percent, collapse);
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

                // The panels have moved, so a page watching a resize as it happens is told - the keyboard is
                // as much a way of resizing a splitter as the pointer is.
                if (entry.options.notifyResize) {
                    invoke('HandleResize', percent);
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

                // The control that folds the panel away sits on the gutter, so a press meant for it would
                // otherwise start a drag of the very thing it is about to fold.
                if (Splitter.isCollapseButton(e.target)) return;

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
                Splitter._dragging[entry.id] = true;
                latestBounds = bounds;
                axisSign = Splitter.getAxisSign(entry);
                previewOffset = entry.options.lazyResize ? Splitter.getPreviewOffset(entry) : 0;
                startPosition = entry.options.vertical ? e.clientY : e.clientX;
                startSize = bounds.current;
                latestSize = bounds.current;
                latestRaw = bounds.current;
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

                latestBounds = bounds;

                const position = entry.options.vertical ? e.clientY : e.clientX;
                const delta = (position - startPosition) * axisSign;

                const raw = Splitter.applyDragStep(entry, startSize + delta);
                const size = Splitter.clamp(raw, bounds.min, bounds.max);

                // Held to the room the panels have between them rather than to their constraints: past the
                // edge of the splitter there is nothing left to aim at, but everything between the minimum
                // and that edge is a drag pushing the panel shut.
                latestRaw = Splitter.clamp(raw, 0, bounds.space);

                // A drag that has gone past what the panel is allowed to be, and far enough into the edge to
                // close it, has moved on even though the panel it is pushing has not: a panel sitting on its
                // minimum only ever answers such a drag by shutting, so the snap is all there is left for it
                // to reach and the gesture counts as a move of its own.
                const pushingClosed = raw !== size && Splitter.shouldSnapClosed(entry, latestRaw, bounds);

                // A pointer that has not moved the gutter has not started a resize - a press that goes
                // nowhere, the two that make up a double-click above all, is not one.
                if (moved === false && size === startSize && pushingClosed === false) return;

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

            gutter.addEventListener('dblclick', e => {
                if (entry.options.disabled) return;

                // Two presses on the control that folds the panel away are two folds, not a reset of the
                // gutter underneath it.
                if (Splitter.isCollapseButton(e.target)) return;

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

                    // Towards the panel that folds closes it and away from it opens it again, whichever
                    // end of the splitter that panel is at and whichever way round the writing direction
                    // puts the two of them.
                    const towardsPanel = entry.options.collapseSecond ? towardsEnd : towardsStart;
                    const awayFromPanel = entry.options.collapseSecond ? towardsStart : towardsEnd;
                    const collapse = Splitter.getAxisSign(entry) > 0 ? towardsPanel : awayFromPanel;

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

                // Home and End are the two ends of the range the panel may move through, so they are taken
                // as they are rather than put on the grid a step gives the drag: a rounded limit is one the
                // key was pressed to reach and stopped short of.
                const target = home
                    ? bounds.min
                    : end
                        ? bounds.max
                        : Splitter.applyKeyStep(entry,
                                                bounds.current,
                                                step * factor * Splitter.getAxisSign(entry) * entry.options.keyboardStep);

                const size = Splitter.clamp(target, bounds.min, bounds.max);

                const applied = Splitter.applySize(entry, size, bounds);
                if (applied === null) return;

                notifyKeyResize(applied, e.repeat, Splitter.toPercent(bounds.current, bounds.total));
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

        public static update(id: string, options: SplitterOptions): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            const previous = entry.options;

            entry.options = options;

            // A splitter that stops being resizable in the middle of a drag is put back where the drag
            // found it: going on moving a layout the page has just closed to negotiation would be the one
            // thing closing it was meant to prevent.
            if (options.disabled && Splitter._dragging[id]) {
                entry.cancelDrag();
            }

            // The size of a panel is held as a share of the splitter rather than as a width or a height, so
            // turning the splitter on its side keeps the split where it was and only the reported position
            // of the separator - which the fold of a panel changes too - has to catch up.
            Splitter.reportPosition(entry);

            // A splitter standing at nothing worth remembering is written down only when that is itself the
            // change - the reset of a position that had been remembered. One that never had a position of
            // its own, because nothing has moved it yet or the page refused the restore, would otherwise
            // take what an earlier visit left under the key away on the first parameter that changes.
            if (Splitter.isStorable(options) || Splitter.isStorable(previous)) {
                Splitter.writeStored(entry);
            }
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

        /// Measures the share of the splitter the first panel takes up at this moment. It is the one thing
        /// .NET cannot work out for itself: until the gutter has been moved the split is whatever the panel
        /// sizes, the constraints and the content between them made of it, and only the browser has that.
        public static getPercent(id: string): number | null {
            const entry = Splitter._entries[id];
            if (!entry) return null;

            const bounds = Splitter.getBounds(entry);
            if (bounds === null) return null;

            return Splitter.toPercent(bounds.current, bounds.total);
        }

        public static dispose(id: string): void {
            const entry = Splitter._entries[id];
            if (!entry) return;

            // A splitter taken off the page in the middle of a drag would otherwise leave the body wearing
            // the resize cursor and unable to select text.
            if (Splitter._dragging[id]) {
                delete Splitter._dragging[id];
                Splitter.setDragChrome(entry, false);
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

        // A key moves the gutter of a splitter with a step of its own from one line of the grid to the next.
        // The step is rounded away from where the gutter stands rather than to the nearest line, because a
        // keyboard step smaller than the grid - which is what the two defaults are - would otherwise round
        // straight back onto the line the gutter is already on and move nothing at all. It also puts a
        // splitter that was laid out off the grid back onto it with the first press.
        private static applyKeyStep(entry: SplitterEntry, current: number, delta: number): number {
            const step = entry.options.dragStep;
            const target = current + delta;

            if (step <= 0) return target;

            return delta > 0
                ? Math.ceil(target / step) * step
                : Math.floor(target / step) * step;
        }

        // A drag that ends far enough inside the minimum of a collapsible panel closes it rather than
        // leaving it at a size the minimum would have refused anyway. What is measured is the panel that
        // folds, which is the second one on a splitter that was told to fold that one: the far end of the
        // range is the edge it is being dragged into.
        private static shouldSnapClosed(entry: SplitterEntry, size: number, bounds: SplitterBounds | null): boolean {
            if (entry.options.collapsible === false) return false;

            if (bounds === null) return false;

            const second = entry.options.collapseSecond;
            const panelSize = second ? bounds.space - size : size;

            // A snap distance of its own is the whole answer: the panel closes within that many pixels of
            // its own edge of the splitter and nowhere else.
            if (entry.options.snapSize > 0) return panelSize <= entry.options.snapSize;

            // Everything the panels are allowed to do is held in the range the first one may move through,
            // so the smallest the second one may be is the room left over at the far end of it.
            const panelMin = second ? bounds.space - bounds.max : bounds.min;

            const threshold = panelMin > 0
                ? panelMin * Splitter.SNAP_RATIO
                : bounds.space * Splitter.SNAP_FALLBACK_RATIO;

            return panelSize <= Math.max(threshold, entry.options.collapsedSize);
        }

        // Nothing is written without a measurement to write it against: a share worked out from a splitter
        // that could not be measured is zero whatever the panels are actually doing, and applying it would
        // fold the first panel away over a layout the browser simply had not settled yet.
        private static applySize(entry: SplitterEntry, size: number, bounds: SplitterBounds | null): number | null {
            if (bounds === null || bounds.total <= 0) return null;

            return Splitter.applyPercent(entry, Splitter.toPercent(size, bounds.total));
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
        private static showPreview(entry: SplitterEntry, size: number, offset: number): void {
            if (!entry.preview) return;

            const distance = Math.max(0, size) + offset + 'px';

            entry.preview.classList.add('bit-spl-prv-on');

            if (entry.options.vertical) {
                entry.preview.style.insetBlockStart = distance;
                entry.preview.style.insetInlineStart = '';
            } else {
                // The inline start of the splitter is its right edge in a right-to-left page, which is the
                // same edge the first panel is measured from, so the offset needs no sign of its own.
                entry.preview.style.insetInlineStart = distance;
                entry.preview.style.insetBlockStart = '';
            }
        }

        private static hidePreview(entry: SplitterEntry): void {
            if (!entry.preview) return;

            entry.preview.classList.remove('bit-spl-prv-on');
        }

        // A line placed out of the flow is measured from the inside of the root's border, while the panels
        // start after its padding as well - so a splitter given padding of its own would draw the line that
        // much short of the gutter it stands for. It cannot change under a drag, so it is read once.
        private static getPreviewOffset(entry: SplitterEntry): number {
            try {
                const style = getComputedStyle(entry.root);
                const value = parseFloat(entry.options.vertical ? style.paddingBlockStart : style.paddingInlineStart);

                return isNaN(value) ? 0 : value;
            } catch {
                return 0;
            }
        }

        private static previewPercent(size: number, bounds: SplitterBounds | null): number {
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
                return;
            }

            entry.root.classList.remove('bit-spl-drg');

            // A second finger can be dragging a second splitter, and the page is still being resized until
            // the last of them lets go.
            if (Object.keys(Splitter._dragging).length > 0) return;

            body.classList.remove('bit-spl-drg-bdy');
            body.classList.remove('bit-spl-drg-vrt');
        }

        // A press that lands on the control folding the panel away is that control's, not the gutter's,
        // however much of the gutter the control happens to cover.
        private static isCollapseButton(target: EventTarget | null): boolean {
            return target instanceof Element && target.closest(Splitter.COLLAPSE_BUTTON_SELECTOR) !== null;
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

        // Whether there is anything about where the splitter stands worth remembering: a share it was
        // dragged to, or a panel folded away. A splitter at neither is what a restore reads as nothing to
        // restore, so writing that state down is only ever the clearing of one that was remembered.
        private static isStorable(options: SplitterOptions): boolean {
            return (options.percent !== null && options.percent !== undefined) || options.collapsed;
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
