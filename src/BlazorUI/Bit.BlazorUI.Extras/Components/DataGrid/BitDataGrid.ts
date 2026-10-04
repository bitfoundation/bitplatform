namespace BitBlazorUI {
    export class DataGrid {
        // Infinite scrolling is the one feature that genuinely needs to read scroll
        // position (which Blazor's scroll EventArgs do not expose), so this watches
        // the viewport and notifies .NET when the user nears the end.
        public static initInfiniteScroll(viewport: HTMLElement, dotNetRef: DotNetObject, threshold: number) {
            const distance = threshold ?? 200;
            let ticking = false;
            let disposed = false;
            // Guards against firing OnInfiniteScrollNearEndAsync again while a prior invocation is still
            // in flight, which would otherwise overlap loads and duplicate interop on rapid scrolling.
            let pending = false;

            const check = () => {
                ticking = false;
                if (disposed || !viewport || pending) return;
                const remaining = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
                if (remaining <= distance) {
                    pending = true;
                    // The circuit may disconnect (navigation, refresh) between the disposed check and
                    // this async call, so swallow the resulting rejection to avoid unhandled console errors.
                    // Only re-check once the load settles if the .NET callback reports more data was
                    // appended and remains; otherwise stop, so end-of-data (a no-op load) doesn't spin
                    // this check()->invoke->check() loop forever.
                    // Defer the follow-up near-end check with requestAnimationFrame so it runs only
                    // after Blazor has rendered the freshly appended rows; reading scrollHeight in the
                    // synchronous continuation would otherwise observe stale layout. The disposed guard
                    // is preserved so a circuit teardown between callback and frame stops the loop.
                    dotNetRef.invokeMethodAsync<boolean>('OnInfiniteScrollNearEndAsync')
                        .then(
                            (more) => { pending = false; if (!disposed && more) requestAnimationFrame(check); },
                            () => { pending = false; }
                        );
                }
            };

            const onScroll = () => {
                if (!ticking) {
                    ticking = true;
                    requestAnimationFrame(check);
                }
            };

            viewport.addEventListener('scroll', onScroll, { passive: true });
            // Initial check so a first batch that doesn't fill the viewport keeps loading.
            setTimeout(check, 0);

            return {
                check: () => check(),
                scrollToTop: () => { if (viewport) viewport.scrollTop = 0; },
                dispose: () => { disposed = true; viewport.removeEventListener('scroll', onScroll); }
            };
        }

        // Reports the viewport's horizontal scroll offset and width to .NET so column virtualization
        // can pick the visible column window. rAF-throttled, with a px hysteresis so tiny scroll
        // deltas (well inside the overscan) don't cause interop chatter; a viewport resize always
        // re-reports. RTL browsers use negative scrollLeft - normalized on the .NET side.
        public static initHorizontalScroll(viewport: HTMLElement, dotNetRef: DotNetObject, hysteresis: number) {
            const threshold = hysteresis > 0 ? hysteresis : 100;
            let lastLeft = Number.NEGATIVE_INFINITY;
            let lastWidth = -1;
            let ticking = false;
            let disposed = false;

            const report = () => {
                ticking = false;
                if (disposed) return;
                const left = viewport.scrollLeft;
                const width = viewport.clientWidth;
                if (Math.abs(left - lastLeft) < threshold && width === lastWidth) return;
                lastLeft = left;
                lastWidth = width;
                dotNetRef.invokeMethodAsync('OnHorizontalScrollAsync', left, width).catch(() => { });
            };
            const onScroll = () => {
                if (!ticking) {
                    ticking = true;
                    requestAnimationFrame(report);
                }
            };

            viewport.addEventListener('scroll', onScroll, { passive: true });
            const resizeObserver = typeof ResizeObserver !== 'undefined'
                ? new ResizeObserver(() => { lastLeft = Number.NEGATIVE_INFINITY; onScroll(); })
                : null;
            resizeObserver?.observe(viewport);
            setTimeout(report, 0);

            return {
                dispose: () => {
                    disposed = true;
                    viewport.removeEventListener('scroll', onScroll);
                    resizeObserver?.disconnect();
                }
            };
        }

        // Touch/pen drag-and-drop for row and column reordering. Native HTML5 drag-and-drop (which the
        // grid uses for mouse) does not fire on touch devices, so this drives the same .NET reorder
        // pipeline from pointer events instead. Mouse pointers are ignored here to avoid double-handling.
        // Rows are identified by their data-ri (dataset index) attribute, columns by data-col (column id);
        // both attributes are only rendered while the corresponding reorder feature is enabled.
        public static initPointerReorder(root: HTMLElement, dotNetRef: DotNetObject) {
            let dragging: { kind: 'row' | 'col', from: string } | null = null;
            let overEl: HTMLElement | null = null;
            // The pointer that started the drag. Its originating element captures it so move/up keep
            // firing even when a touch/pen drag leaves the grid (otherwise the drag would silently
            // stall, leaving `dragging` and the drop-target highlight stuck), and move/up/cancel only
            // act for this pointer so a second concurrent touch can't mutate or end the drag.
            let activePointerId: number | null = null;

            const clearOver = () => { overEl?.classList.remove('bit-dtg-drop-target'); overEl = null; };

            const startDrag = (e: PointerEvent, drag: { kind: 'row' | 'col', from: string }) => {
                dragging = drag;
                activePointerId = e.pointerId;
                try { (e.target as HTMLElement).setPointerCapture(e.pointerId); } catch { /* pointer no longer active */ }
                e.preventDefault();
            };

            const onPointerDown = (e: PointerEvent) => {
                if (e.pointerType === 'mouse') return; // mouse uses native HTML5 DnD
                if (dragging) return; // a drag is already in progress on another pointer
                const target = e.target as HTMLElement | null;
                if (!target) return;

                const handle = target.closest('.bit-dtg-drag-handle');
                if (handle) {
                    const row = handle.closest('[data-ri]') as HTMLElement | null;
                    if (!row) return;
                    startDrag(e, { kind: 'row', from: row.getAttribute('data-ri')! });
                    return;
                }

                const hcell = target.closest('.bit-dtg-hcell[data-col]') as HTMLElement | null;
                if (hcell && !target.closest('.bit-dtg-resizer')) {
                    startDrag(e, { kind: 'col', from: hcell.getAttribute('data-col')! });
                }
            };

            const onPointerMove = (e: PointerEvent) => {
                if (!dragging || e.pointerId !== activePointerId) return;
                // elementFromPoint hit-tests the layout under the finger regardless of event capture.
                const under = document.elementFromPoint(e.clientX, e.clientY) as HTMLElement | null;
                const candidate = dragging.kind === 'row'
                    ? under?.closest('[data-ri]') as HTMLElement | null
                    : under?.closest('.bit-dtg-hcell[data-col]') as HTMLElement | null;
                if (candidate !== overEl) {
                    clearOver();
                    if (candidate && root.contains(candidate)) {
                        overEl = candidate;
                        overEl.classList.add('bit-dtg-drop-target');
                    }
                }
            };

            const onPointerUp = (e: PointerEvent) => {
                if (!dragging || e.pointerId !== activePointerId) return;
                const attr = dragging.kind === 'row' ? 'data-ri' : 'data-col';
                const to = overEl?.getAttribute(attr);
                const { kind, from } = dragging;
                dragging = null;
                activePointerId = null;
                clearOver();
                if (to != null && to !== from) {
                    // Swallow rejections: the circuit may be tearing down mid-drop.
                    if (kind === 'row') {
                        dotNetRef.invokeMethodAsync('OnPointerRowDropAsync', parseInt(from, 10), parseInt(to, 10)).catch(() => { });
                    } else {
                        dotNetRef.invokeMethodAsync('OnPointerColumnDropAsync', from, to).catch(() => { });
                    }
                }
            };

            const onPointerCancel = (e: PointerEvent) => {
                if (e.pointerId !== activePointerId) return;
                dragging = null;
                activePointerId = null;
                clearOver();
            };

            root.addEventListener('pointerdown', onPointerDown);
            root.addEventListener('pointermove', onPointerMove);
            root.addEventListener('pointerup', onPointerUp);
            root.addEventListener('pointercancel', onPointerCancel);

            return {
                dispose: () => {
                    root.removeEventListener('pointerdown', onPointerDown);
                    root.removeEventListener('pointermove', onPointerMove);
                    root.removeEventListener('pointerup', onPointerUp);
                    root.removeEventListener('pointercancel', onPointerCancel);
                    clearOver();
                }
            };
        }

        // Publishes the height of the sticky header and footer on the viewport as --bit-dtg-head-h/--bit-dtg-foot-h,
        // which the stylesheet turns into scroll padding: whatever the browser scrolls into view inside the viewport
        // (a cell the arrow keys move to, a checkbox reached with Tab) then stops below the header and above the
        // footer instead of under them (WCAG 2.4.11). Both can change height at any time (wrapped titles, a filter
        // row, a footer appearing), and so does the table they sit in, which is what is observed.
        public static observeStickyBands(viewport: HTMLElement) {
            if (!viewport || typeof ResizeObserver === 'undefined') return { dispose: () => { } };

            const update = () => {
                const table = viewport.firstElementChild;
                const header = table?.querySelector<HTMLElement>(':scope > .bit-dtg-header');
                const footer = table?.querySelector<HTMLElement>(':scope > .bit-dtg-footer');
                viewport.style.setProperty('--bit-dtg-head-h', `${header?.offsetHeight ?? 0}px`);
                viewport.style.setProperty('--bit-dtg-foot-h', `${footer?.offsetHeight ?? 0}px`);
            };

            const observer = new ResizeObserver(update);
            observer.observe(viewport);
            if (viewport.firstElementChild) observer.observe(viewport.firstElementChild);
            update();

            return { dispose: () => observer.disconnect() };
        }

        // Syncs the "some but not all rows selected" state onto the select-all checkbox.
        // indeterminate is a DOM property with no attribute equivalent, so Blazor markup can't set it.
        public static setIndeterminate(element: HTMLInputElement, value: boolean) {
            if (element) element.indeterminate = value;
        }

        // Whether the element renders right to left. A grid whose Dir is unset inherits the page's
        // direction and one whose Dir is Auto takes it from its content, so neither is known to .NET;
        // the computed style resolves both, the dir attribute of every ancestor included.
        public static isRtl(element: HTMLElement): boolean {
            return element ? getComputedStyle(element).direction === 'rtl' : false;
        }

        // Puts the focus back into a column header after its column moved: moving an element in the DOM drops the
        // focus it held. The sort button is the header's own control, so it is preferred; a header that is not
        // sortable falls back to its first focusable child (the resize handle).
        public static focusHeader(root: HTMLElement, columnId: string) {
            const header = root?.querySelector<HTMLElement>(`.bit-dtg-header-row .bit-dtg-hcell[data-col="${CSS.escape(columnId)}"]`);
            if (!header) return;
            const target = header.querySelector<HTMLElement>('button.bit-dtg-htext')
                ?? header.querySelector<HTMLElement>('button, [tabindex]:not([tabindex="-1"])');
            target?.focus();
        }

        // Moves the focus into an editor that has just opened, so Enter/F2, a double-click or the Edit button
        // leave the user typing rather than on a cell (or a button) that no longer holds the control. A blank
        // column id means the row's first editor. Only the cells of this grid count, not those of a grid nested
        // in a detail row. Text is selected, so typing replaces the value the way it does in a spreadsheet.
        // An editor opened by typing into its cell (typed) takes the keys gathered while it was opening instead -
        // see the cell key guard - and an input event tells .NET about them as if they had been typed into it.
        public static focusEditor(root: HTMLElement, columnId: string, typed?: boolean) {
            if (!root) return;
            const text = typedText.get(root)?.text;
            typedText.delete(root);
            const cells = Array.from(root.querySelectorAll<HTMLElement>('[data-bit-dtg-edit]'))
                .filter(c => c.closest('.bit-dtg') === root);
            const cell = (columnId ? cells.find(c => c.dataset.bitDtgEdit === columnId) : undefined) ?? cells[0];
            const target = cell ? focusableControls(cell)[0] : undefined;
            if (!target) return;
            target.focus();
            if (!(target instanceof HTMLInputElement) || !['text', 'search', 'number', 'email', 'tel', 'url'].includes(target.type)) return;
            if (typed) {
                // A key that reached .NET after the editor had already claimed the gathered text asks again, with
                // nothing left to hand over: the editor keeps what it holds, unselected, so typing runs on.
                if (text === undefined) return;
                // Inserted the way typing inserts it, replacing the selected value: a number input refuses a value
                // set from script that is not yet a number ("12." on the way to "12.5"), but keeps one typed into it.
                // Both paths raise the input event that tells .NET.
                try { target.select(); } catch { }
                const inserted = text.length > 0 ? document.execCommand('insertText', false, text) : document.execCommand('delete');
                if (!inserted) {
                    target.value = text;
                    target.dispatchEvent(new Event('input', { bubbles: true }));
                }
                return;
            }
            try { target.select(); } catch { }
        }

        // Moves the focus to the first focusable match of the selectors, tried in order, inside this grid - not inside
        // a grid nested in one of its detail rows. Used when the control that held the focus is about to go away (a
        // clear button that disappears with what it cleared) or to move (a keyed item that follows its column).
        public static focusFirst(root: HTMLElement, selectors: string[]) {
            if (!root) return;
            for (const selector of selectors ?? []) {
                let matches: HTMLElement[];
                try {
                    matches = Array.from(root.querySelectorAll<HTMLElement>(selector));
                } catch {
                    continue;
                }
                const target = matches.find(el => el.closest('.bit-dtg') === root && isFocusable(el));
                if (target) {
                    target.focus();
                    return;
                }
            }
        }

        // Puts the focus on a row's command button (Edit) once the Save/Cancel button that held it is gone.
        public static focusRowCommand(root: HTMLElement, ariaRowIndex: number) {
            const row = Array.from(root?.querySelectorAll<HTMLElement>(`.bit-dtg-row[aria-rowindex="${ariaRowIndex}"]`) ?? [])
                .find(r => r.closest('.bit-dtg') === root);
            row?.querySelector<HTMLElement>('.bit-dtg-cell-command button:not([disabled])')?.focus();
        }

        // Cell edit mode commits when the focus leaves the open cell. A focusout whose relatedTarget is still
        // inside the cell (a custom EditTemplate with several controls) is a move within it, not a departure -
        // and so is one into a popup the cell's editor opened (a dropdown's list, a date picker's calendar), which
        // the callout JS moves to the body and which only the opener's aria-controls / aria-owns ties back to it.
        // The edit then commits once the focus or a press lands outside both. The cell carries the number of its
        // edit, so a late report cannot commit the edit opened after it.
        public static initCellEditBlur(root: HTMLElement, dotNetRef: DotNetObject) {
            if (!root) return { dispose: () => { } };
            // The edit whose focus went into one of its popups, still to commit when the user moves on from it.
            let away: { cell: HTMLElement, version: number } | null = null;
            // The last element pressed: a press on a part of a popup that takes no focus blurs with no relatedTarget.
            let pressed: Node | null = null;
            const commit = (version: number) => dotNetRef.invokeMethodAsync('OnCellEditBlurAsync', version);
            const onFocusOut = (e: FocusEvent) => {
                const cell = (e.target as HTMLElement | null)?.closest<HTMLElement>('[data-bit-dtg-edit-version]');
                if (!cell || cell.closest('.bit-dtg') !== root) return;
                const version = Number(cell.dataset.bitDtgEditVersion);
                const next = (e.relatedTarget as Node | null) ?? pressed;
                if (next && cell.contains(next)) return;
                if (next && isOwnedBy(cell, next)) {
                    away = { cell, version };
                    return;
                }
                away = null;
                commit(version);
            };
            // Capture phase, so a popup that stops the event still reports where the user went.
            const onElsewhere = (e: Event) => {
                if (e.type === 'pointerdown') pressed = e.target as Node | null;
                if (!away) return;
                const target = e.target as Node | null;
                if (!away.cell.isConnected) {
                    away = null;
                    return;
                }
                if (target && (away.cell.contains(target) || isOwnedBy(away.cell, target))) return;
                const version = away.version;
                away = null;
                commit(version);
            };
            // A press is only the cause of the blur it is in the middle of, never of a later one (Tab out to the browser).
            const onReleased = () => pressed = null;
            root.addEventListener('focusout', onFocusOut);
            document.addEventListener('focusin', onElsewhere, true);
            document.addEventListener('pointerdown', onElsewhere, true);
            document.addEventListener('pointerup', onReleased, true);
            document.addEventListener('pointercancel', onReleased, true);
            return {
                dispose: () => {
                    root.removeEventListener('focusout', onFocusOut);
                    document.removeEventListener('focusin', onElsewhere, true);
                    document.removeEventListener('pointerdown', onElsewhere, true);
                    document.removeEventListener('pointerup', onReleased, true);
                    document.removeEventListener('pointercancel', onReleased, true);
                }
            };
        }

        // Measures an element's rendered width. Used when a column resize starts so the drag begins
        // from the column's real on-screen width even when its Width is expressed in %/fr units
        // (which .NET cannot resolve to pixels on its own).
        public static getWidth(element: HTMLElement): number {
            return element ? element.getBoundingClientRect().width : 0;
        }

        // Measures the widest rendered content of one column (its header included) so .NET can
        // auto-fit the column to it. Cells clip their overflow, so scrollWidth - not the box width -
        // is what reports the untruncated content; the horizontal padding is added back because
        // scrollWidth excludes it on a flex container.
        public static measureColumnContentWidth(root: HTMLElement, ariaColIndex: number): number {
            if (!root) return 0;
            const cells = root.querySelectorAll(`[aria-colindex="${ariaColIndex}"]`);
            let widest = 0;
            cells.forEach(node => {
                const cell = node as HTMLElement;
                // A spanning cell's content belongs to several columns, so it would over-size this one.
                if (cell.style.gridColumn) return;
                // The filter row's editors stretch to the column (.bit-dtg-filter-wrap is width:100%),
                // so measuring that cell would report the column's current width back as its content:
                // a fitted column could then never shrink, and a narrow one would snap to the width of
                // the operator dropdown. Auto-fit is about the header and the data, so skip it.
                if (cell.closest('.bit-dtg-filter-row')) return;
                const styles = getComputedStyle(cell);
                const padding = parseFloat(styles.paddingLeft || '0') + parseFloat(styles.paddingRight || '0');
                // The header holds the sort/group/resize affordances next to its label, so measure its
                // children's extent rather than the label alone. Both measurements exclude the cell's
                // own padding, which the widest calculation below adds back exactly once.
                let content = cell.scrollWidth;
                for (let i = 0; i < cell.children.length; i++) {
                    const child = cell.children[i] as HTMLElement;
                    if (child.classList.contains('bit-dtg-resizer')) continue;
                    content = Math.max(content, child.scrollWidth);
                }
                widest = Math.max(widest, content + padding);
            });
            // A couple of pixels of slack keeps the fitted column from re-clipping on sub-pixel rounding.
            return widest > 0 ? Math.ceil(widest) + 2 : 0;
        }

        // Copies text to the system clipboard, reporting whether it landed. The async Clipboard API is
        // unavailable on insecure origins and can be denied by permission, so this falls back to the
        // legacy execCommand path over an off-screen textarea before giving up.
        public static async copyToClipboard(text: string): Promise<boolean> {
            try {
                if (navigator.clipboard && window.isSecureContext) {
                    await navigator.clipboard.writeText(text);
                    return true;
                }
            } catch { /* fall through to the legacy path */ }

            try {
                const area = document.createElement('textarea');
                area.value = text;
                // Keep it out of view and unfocusable-by-scroll so copying doesn't jump the page.
                area.setAttribute('readonly', '');
                area.style.position = 'fixed';
                area.style.top = '-1000px';
                area.style.opacity = '0';
                document.body.appendChild(area);
                area.select();
                const ok = document.execCommand('copy');
                document.body.removeChild(area);
                return ok;
            } catch {
                return false;
            }
        }

        // Samples the grid's rendered theme (computed styles of representative cells) so a styled
        // Excel export can bake the on-screen colors/fonts into the workbook. The grid's colors come
        // from CSS theme variables that .NET cannot resolve, so this is the only faithful source.
        // Selection/editing/message rows are skipped: their cells are recolored by transient state.
        public static getExportStyles(root: HTMLElement): object | null {
            if (!root) return null;

            const toHex = (color: string | null | undefined) => {
                if (!color) return null;
                const m = color.match(/rgba?\(\s*(\d+)[,\s]+(\d+)[,\s]+(\d+)(?:[,\s/]+([\d.]+%?))?\s*\)/);
                if (!m) return null;
                if (m[4] !== undefined && parseFloat(m[4]) === 0) return null; // fully transparent
                const hex = (n: string) => parseInt(n, 10).toString(16).padStart(2, '0');
                return `#${hex(m[1])}${hex(m[2])}${hex(m[3])}`;
            };
            const styleOf = (el: Element | null | undefined) => el ? getComputedStyle(el as HTMLElement) : null;
            const isBold = (s: CSSStyleDeclaration | null) => !!s && (s.fontWeight === 'bold' || parseInt(s.fontWeight, 10) >= 600);
            const isItalic = (s: CSSStyleDeclaration | null) => !!s && s.fontStyle.includes('italic');

            const header = styleOf(root.querySelector('.bit-dtg-header-row .bit-dtg-hcell'));
            const rows = root.querySelectorAll(
                '.bit-dtg-body > .bit-dtg-row:not(.bit-dtg-selected):not(.bit-dtg-editing):not(.bit-dtg-message-row):not(.bit-dtg-placeholder-row)');
            const cellOdd = styleOf(rows[0]?.querySelector('.bit-dtg-cell'));
            const cellEven = styleOf(rows[1]?.querySelector('.bit-dtg-cell'));
            if (!header && !cellOdd) return null;

            const rowBackground = toHex(cellOdd?.backgroundColor);
            const evenBackground = toHex(cellEven?.backgroundColor);
            return {
                headerBackground: toHex(header?.backgroundColor),
                headerForeground: toHex(header?.color),
                headerBold: isBold(header),
                headerItalic: isItalic(header),
                rowBackground: rowBackground,
                rowForeground: toHex(cellOdd?.color),
                rowBold: isBold(cellOdd),
                rowItalic: isItalic(cellOdd),
                // Only report a stripe when the second row really renders differently (grid is striped).
                stripeBackground: evenBackground && evenBackground !== rowBackground ? evenBackground : null,
                borderColor: toHex(styleOf(rows[0])?.borderBottomColor),
            };
        }

        // Triggers a client-side file download for the given text content. Used by CSV export so the
        // (potentially large) CSV is generated only on demand instead of living in a DOM attribute and
        // being regenerated on every render. Uses a Blob + object URL to avoid data-URI length limits.
        public static download(fileName: string, content: string, mimeType: string) {
            DataGrid.downloadBlob(fileName, new Blob([content], { type: mimeType || 'text/plain;charset=utf-8' }));
        }

        // Binary variant used by the Excel export: the .NET side sends the workbook bytes as base64.
        public static downloadBase64(fileName: string, base64: string, mimeType: string) {
            const binary = atob(base64);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
            DataGrid.downloadBlob(fileName, new Blob([bytes], { type: mimeType || 'application/octet-stream' }));
        }

        private static downloadBlob(fileName: string, blob: Blob) {
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName || 'download';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            // Revoke after the click has been dispatched so the download isn't cancelled prematurely.
            setTimeout(() => URL.revokeObjectURL(url), 0);
        }
    }

    // Reorder drag handles move rows with ArrowUp/ArrowDown. The browser's default for those keys is to
    // scroll the page/grid, which must be cancelled *before* the event reaches Blazor's .NET handler.
    // Blazor evaluates @onkeydown:preventDefault at render time, so it can't decide based on the upcoming
    // key and lags a keystroke behind. A single capture-phase listener decides per-key up front and only
    // cancels the arrow keys on a focused drag handle, so Tab/Enter/Space keep working and the .NET
    // keydown handler still runs to actually move the row.
    // The column headers own two more keys the same way: a focused resize handle (a separator) moves its edge with
    // the arrows and takes it to its limits with Home/End, and Ctrl+Left/Right anywhere in a reorderable header
    // (one that carries data-col) moves the column. Left alone, both would also scroll the viewport sideways.
    const resizerKeys = new Set(['ArrowLeft', 'ArrowRight', 'Home', 'End', 'Enter']);
    let reorderKeyGuardInstalled = false;
    function installReorderKeyGuard() {
        if (reorderKeyGuardInstalled || typeof document === 'undefined') return;
        reorderKeyGuardInstalled = true;
        document.addEventListener('keydown', (e: KeyboardEvent) => {
            const target = e.target as HTMLElement | null;
            if (target?.classList?.contains('bit-dtg-resizer') && resizerKeys.has(e.key) && !e.altKey && !e.metaKey) {
                e.preventDefault();
                return;
            }
            if (e.ctrlKey && !e.altKey && !e.metaKey && !e.shiftKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight') &&
                target?.closest?.('.bit-dtg-hcell[data-col]')) {
                e.preventDefault();
                return;
            }
            if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
            if (target?.classList?.contains('bit-dtg-drag-handle')) {
                // Don't cancel the default while the row is being edited: keyboard reordering is
                // short-circuited in that state (matching the .NET handler and the draggable guard),
                // so swallowing the arrow keys here would needlessly block scrolling during an edit.
                if (target.closest('.bit-dtg-row')?.classList?.contains('bit-dtg-editing')) return;
                e.preventDefault();
            }
        }, { capture: true });
    }

    installReorderKeyGuard();

    // A column's resize handle sits inside its header, which is draggable while the column can be reordered. Firefox
    // starts the header's native drag from a press on the handle (the press is not cancelled - that would also keep
    // the handle from taking the focus), so the drag that should move the column's edge moved the column instead.
    // The handle pressed last is remembered, and a drag of the header holding it is cancelled before it starts;
    // the resize overlay then receives the pointer moves as it does in other browsers.
    let pressedResizer: Element | null = null;
    let resizeDragGuardInstalled = false;
    function installResizeDragGuard() {
        if (resizeDragGuardInstalled || typeof document === 'undefined') return;
        resizeDragGuardInstalled = true;
        document.addEventListener('pointerdown', (e: PointerEvent) => {
            pressedResizer = (e.target as Element | null)?.closest?.('.bit-dtg-resizer') ?? null;
        }, { capture: true });
        const release = () => pressedResizer = null;
        document.addEventListener('pointerup', release, { capture: true });
        document.addEventListener('dragend', release, { capture: true });
        document.addEventListener('dragstart', (e: DragEvent) => {
            const target = e.target as Node | null;
            if (pressedResizer && target?.contains?.(pressedResizer)) e.preventDefault();
        }, { capture: true });
    }

    installResizeDragGuard();

    // A focused, navigable data cell owns the arrow / page / home / end / enter / escape / F2 keys
    // (cell-to-cell movement and the edit lifecycle). Their browser defaults -- scrolling the
    // page/grid, submitting a surrounding form, resetting an input -- must be cancelled *before* the
    // event reaches Blazor's .NET handler. As with the reorder guard, @onkeydown:preventDefault can't
    // do this (it's evaluated at render time, can't know the upcoming key, and lags one keystroke), so
    // a single capture-phase listener decides per-key up front. Tab and ordinary typing are left
    // untouched so focus can still leave the grid and editors keep receiving characters.
    // Space is grid-owned too: it toggles the focused row's selection, so its page-scroll default must
    // be cancelled exactly like the arrow keys'.
    const cellNavKeys = new Set([
        'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
        'Home', 'End', 'PageUp', 'PageDown', 'Enter', 'Escape', 'F2', ' '
    ]);
    // Keys that should stay with a self-managed control nested inside a cell. Escape is intentionally
    // excluded so it keeps bubbling to the grid as the universal "cancel edit" affordance, while the
    // navigation keys, Enter (commit), F2 (enter edit) and Space are kept with the embedded control.
    // Space matters because a row acting as its own detail toggle (see BitDataGridRow) handles it on
    // the row element: letting it bubble would type-and-toggle in an editor input, and would toggle a
    // second time on top of the click a nested button synthesizes from Space.
    const nestedControlKeys = new Set([
        'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
        'Home', 'End', 'PageUp', 'PageDown', 'Enter', 'F2', ' '
    ]);
    // Controls inside an editor that have their own Enter/Escape semantics and must not have those
    // keys cancelled by the grid (buttons, selects, textareas, links and contenteditable regions).
    // Plain text INPUTs are intentionally excluded so the edit-flow keeps treating Enter as commit and
    // Escape as cancel for grid-owned editor inputs (see the edit-row branch below).
    function isSelfManagedEditKeyControl(el: HTMLElement): boolean {
        if (el.isContentEditable) return true;
        switch (el.tagName) {
            case 'BUTTON':
            case 'SELECT':
            case 'TEXTAREA':
            case 'A':
                return true;
            default:
                return false;
        }
    }
    // Controls nested inside a read-only / navigable templated cell that own their own keyboard
    // behavior and must be excluded from grid key routing. This is the self-managed set plus INPUT:
    // a plain text input dropped into a custom (non-editing) cell template needs its caret movement,
    // typing, Enter and F2 to stay with the input rather than triggering cell navigation. The edit-row
    // Enter/Escape flow deliberately uses isSelfManagedEditKeyControl instead, so a grid-owned editor
    // input still commits on Enter and cancels on Escape.
    function isSelfManagedCellKeyControl(el: HTMLElement): boolean {
        return el.tagName === 'INPUT' || isSelfManagedEditKeyControl(el);
    }
    // Ctrl/⌘+C (copy the selection) and Ctrl/⌘+A (select every row of the view) are handled by the
    // focused cell's .NET handler, so their browser defaults have to go the same way the arrow keys'
    // do: left alone, Ctrl+A would also run the document's own select-all -- painting a text selection
    // over the grid -- and the Ctrl+C that follows would race the native copy of that selection against
    // the grid's own clipboard write. Both shortcuts are conditional on the grid's parameters, which
    // the root element publishes (see BitDataGrid.razor); a grid that owns neither leaves them to the
    // browser. Alt+ combinations are not the shortcut and stay untouched.
    function isGridOwnedShortcut(cell: HTMLElement, e: KeyboardEvent): boolean {
        if (!(e.ctrlKey || e.metaKey) || e.altKey) return false;
        const key = e.key.toLowerCase();
        if (key !== 'a' && key !== 'c') return false;
        const root = cell.closest('.bit-dtg') as HTMLElement | null;
        if (!root) return false;
        return key === 'c'
            ? root.hasAttribute('data-bit-dtg-copy')
            : root.hasAttribute('data-bit-dtg-select-all');
    }

    // The controls of an editor that can actually take the focus, in DOM order: enabled, not a hidden input,
    // not hidden themselves or inside a hidden/inert subtree, and rendered (display:none leaves no client
    // rects). Opening an editor focuses the first; Cell-mode Tab treats the first and last as the boundaries.
    const focusableSelector = 'input:not([disabled]), select:not([disabled]), textarea:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])';
    function focusableControls(container: HTMLElement): HTMLElement[] {
        return Array.from(container.querySelectorAll<HTMLElement>(focusableSelector)).filter(isFocusable);
    }
    // Whether a node sits in a popup the container opened: inside an element that a control in the container names
    // with aria-controls or aria-owns - the one tie a popup moved to the body keeps with its opener - or, a few levels
    // deep, in a popup opened from such a popup (a submenu).
    function isOwnedBy(container: HTMLElement, node: Node, depth = 0): boolean {
        if (depth > 3) return false;
        for (let el = node instanceof Element ? node : node.parentElement; el; el = el.parentElement) {
            if (!el.id) continue;
            const id = CSS.escape(el.id);
            const opener = document.querySelector(`[aria-controls~="${id}"], [aria-owns~="${id}"]`);
            if (!opener || opener === el) continue;
            if (container.contains(opener) || isOwnedBy(container, opener, depth + 1)) return true;
        }
        return false;
    }
    function isFocusable(el: HTMLElement): boolean {
        return el.matches(focusableSelector)
            && !(el instanceof HTMLInputElement && el.type === 'hidden')
            && !el.closest('[hidden], [inert]')
            && el.getClientRects().length > 0
            && getComputedStyle(el).visibility !== 'hidden';
    }

    // Typing into a focused cell opens its editor (see BitDataGrid.OpensEditorByTyping), but the editor exists only
    // once .NET has rendered it - a round trip, over a network in Blazor Server - and every key typed before then
    // lands on the cell. They are gathered here, per grid, and handed to the editor when it takes the focus. A gather
    // left unclaimed (the edit was refused) goes stale after a pause, so it cannot leak into a later edit.
    const typedText = new WeakMap<HTMLElement, { text: string, at: number }>();
    const typedTextLifetime = 1500;
    function gatherTypedKey(cell: HTMLElement, e: KeyboardEvent): boolean {
        const kind = cell.getAttribute('data-bit-dtg-typable');
        if (!kind || e.ctrlKey || e.metaKey || e.altKey) return false;
        const root = cell.closest('.bit-dtg') as HTMLElement | null;
        if (!root) return false;
        const now = Date.now();
        const last = typedText.get(root);
        const gathering = !!last && now - last.at < typedTextLifetime;
        const clears = e.key === 'Backspace';
        // Space selects the row, so it cannot start a gather - but once one is under way it is a typed space (the
        // edit has opened in .NET by the time it gets there, and the opening cell no longer takes the key).
        if (!clears && (e.key.length !== 1 || (e.key === ' ' && !gathering))) return false;
        // A number starts the way BitDataGrid.IsTypingKey lets it (no exponent: that key opens nothing in .NET, so
        // gathering it would only prefix the next gather); an exponent can follow once one is under way.
        if (!clears && kind === 'number' && !(gathering ? /[0-9+\-.,eE]/ : /[0-9+\-.,]/).test(e.key)) return false;
        typedText.set(root, { text: clears ? '' : (gathering ? last!.text : '') + e.key, at: now });
        return true;
    }

    let cellKeyGuardInstalled = false;
    function installCellKeyGuard() {
        if (cellKeyGuardInstalled || typeof document === 'undefined') return;
        cellKeyGuardInstalled = true;
        document.addEventListener('keydown', (e: KeyboardEvent) => {
            const target = e.target as HTMLElement | null;
            if (!target) return;

            // A self-managed interactive control (button/select/textarea/link/contenteditable) can be
            // focused *inside* a navigable or editing cell via a custom cell/edit template. The cell's
            // @onkeydown handler is wired through Blazor's document-level delegation, so a grid-owned key
            // pressed on such a descendant would still bubble up to the cell and trigger cell navigation
            // (Arrow/Home/End/Page/F2) or commit/cancel the edit (Enter/Escape) -- stealing the key from
            // the embedded control. Stop propagation here so the key stays with the control; its native
            // behavior is preserved because preventDefault is intentionally not called. This is checked
            // before the cell-target branch below, which only matches when the cell itself is focused.
            // The grid's own editors (.bit-dtg-editor) are the exception: their Enter commits and their other
            // keys are ignored by the editing cell, so they bubble on to it. So is a row's reorder handle for
            // ArrowUp/ArrowDown: they are its own keys, handled by its @onkeydown (BitDataGridRow's
            // HandleReorderKeyDown), which stopping them here would never reach; their scrolling default is
            // cancelled by the reorder guard above, and the row's own handler takes only Enter/Space.
            const ownerCell = target.closest('.bit-dtg-cell') as HTMLElement | null;
            if (ownerCell && ownerCell !== target && nestedControlKeys.has(e.key) && isSelfManagedCellKeyControl(target)
                && !target.classList.contains('bit-dtg-editor')
                && !((e.key === 'ArrowUp' || e.key === 'ArrowDown') && target.classList.contains('bit-dtg-drag-handle'))) {
                e.stopPropagation();
                return;
            }

            // In Cell mode Tab moves the edit to the next cell, which the editing cell's .NET handler does; the
            // browser's own Tab would leave the grid (the other cells are out of the tab order). A Tab that
            // stays among the controls of one custom editor is left native and kept from that handler.
            if (e.key === 'Tab') {
                const editingCell = target.closest<HTMLElement>('.bit-dtg-cell-editing');
                if (!editingCell) return;
                const controls = focusableControls(editingCell);
                const index = controls.indexOf(target);
                const staysInside = e.shiftKey ? index > 0 : index >= 0 && index < controls.length - 1;
                if (staysInside) e.stopPropagation();
                else e.preventDefault();
                return;
            }

            // The navigable cell is the focused element itself (a div.bit-dtg-cell with a tabindex).
            // Suppress the grid-owned keys here so arrow/page/home/end never scroll the viewport.
            if (target.classList?.contains('bit-dtg-cell') && target.hasAttribute('tabindex')) {
                // Gathering comes first: a Space typed while an editor is opening is text, not the selection key.
                if (gatherTypedKey(target, e)) e.preventDefault();
                else if (cellNavKeys.has(e.key)) e.preventDefault();
                else if (isGridOwnedShortcut(target, e)) e.preventDefault();
                return;
            }

            // The row itself is the detail toggle when the toggle column is hidden and cell navigation
            // is off (see BitDataGridRow): it is focusable and activates on Enter/Space like a button,
            // so Space must not also scroll the page. Enter has no default worth cancelling on a div.
            if (e.key === ' ' && target.classList?.contains('bit-dtg-row') && target.hasAttribute('tabindex')) {
                e.preventDefault();
                return;
            }

            // While inline-editing the focus sits on the editor input inside the row, so only the edit
            // lifecycle keys (Enter commits, Escape cancels) are grid-owned; cancel their native
            // actions but leave caret movement and typing to the input.
            if ((e.key === 'Enter' || e.key === 'Escape') &&
                (target.closest('.bit-dtg-row')?.classList?.contains('bit-dtg-editing') || target.closest('.bit-dtg-cell-editing'))) {
                // Don't swallow these keys for nested controls that own their keyboard behavior:
                // a <button> activates on Enter, a <select> opens/commits a choice, a <textarea>
                // inserts a newline, and a contenteditable region edits text. Suppressing here would
                // break those controls. Plain editor inputs aren't excluded, so Enter still avoids a
                // surrounding form submit and Escape still avoids a native input reset for them.
                if (isSelfManagedEditKeyControl(target)) return;
                e.preventDefault();
            }
        }, { capture: true });
    }
    installCellKeyGuard();
}
