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

        // Triggers a client-side file download for the given text content. Used by CSV export so the
        // (potentially large) CSV is generated only on demand instead of living in a DOM attribute and
        // being regenerated on every render. Uses a Blob + object URL to avoid data-URI length limits.
        public static download(fileName: string, content: string, mimeType: string) {
            const blob = new Blob([content], { type: mimeType || 'text/plain;charset=utf-8' });
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
    let reorderKeyGuardInstalled = false;
    function installReorderKeyGuard() {
        if (reorderKeyGuardInstalled || typeof document === 'undefined') return;
        reorderKeyGuardInstalled = true;
        document.addEventListener('keydown', (e: KeyboardEvent) => {
            if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown') return;
            const target = e.target as HTMLElement | null;
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

    // A focused, navigable data cell owns the arrow / page / home / end / enter / escape / F2 keys
    // (cell-to-cell movement and the edit lifecycle). Their browser defaults -- scrolling the
    // page/grid, submitting a surrounding form, resetting an input -- must be cancelled *before* the
    // event reaches Blazor's .NET handler. As with the reorder guard, @onkeydown:preventDefault can't
    // do this (it's evaluated at render time, can't know the upcoming key, and lags one keystroke), so
    // a single capture-phase listener decides per-key up front. Tab and ordinary typing are left
    // untouched so focus can still leave the grid and editors keep receiving characters.
    const cellNavKeys = new Set([
        'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
        'Home', 'End', 'PageUp', 'PageDown', 'Enter', 'Escape', 'F2'
    ]);
    // Keys that should stay with a self-managed control nested inside a cell. Escape is intentionally
    // excluded so it keeps bubbling to the grid as the universal "cancel edit" affordance, while the
    // navigation keys, Enter (commit) and F2 (enter edit) are kept with the embedded control.
    const nestedControlKeys = new Set([
        'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
        'Home', 'End', 'PageUp', 'PageDown', 'Enter', 'F2'
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
            const ownerCell = target.closest('.bit-dtg-cell') as HTMLElement | null;
            if (ownerCell && ownerCell !== target && nestedControlKeys.has(e.key) && isSelfManagedCellKeyControl(target)) {
                e.stopPropagation();
                return;
            }

            // The navigable cell is the focused element itself (a div.bit-dtg-cell with a tabindex).
            // Suppress the grid-owned keys here so arrow/page/home/end never scroll the viewport.
            if (target.classList?.contains('bit-dtg-cell') && target.hasAttribute('tabindex')) {
                if (cellNavKeys.has(e.key)) e.preventDefault();
                return;
            }

            // While inline-editing the focus sits on the editor input inside the row, so only the edit
            // lifecycle keys (Enter commits, Escape cancels) are grid-owned; cancel their native
            // actions but leave caret movement and typing to the input.
            if ((e.key === 'Enter' || e.key === 'Escape') &&
                target.closest('.bit-dtg-row')?.classList?.contains('bit-dtg-editing')) {
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
