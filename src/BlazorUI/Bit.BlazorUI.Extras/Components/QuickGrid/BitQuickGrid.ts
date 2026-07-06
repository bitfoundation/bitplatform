namespace BitBlazorUI {
    export class QuickGrid {
        public static init(tableElement: any) {
            // Tracks the drag handles this init() bound so stop() can remove their listeners too,
            // preventing handlers from accumulating across repeated init()/stop() cycles.
            const boundDragHandles: { handle: any, listener: any }[] = [];
            // Holds the teardown for an in-progress column resize drag (the document-level move/up
            // listeners installed by handleMouseDown) so stop() can detach them even if disposal
            // happens mid-drag, before the pointer is released.
            const dragState: { cleanup: (() => void) | null } = { cleanup: null };
            QuickGrid.enableColumnResizing(tableElement, boundDragHandles, dragState);

            const bodyClickHandler = (event: any) => {
                const columnOptionsElement = tableElement.tHead.querySelector('.bit-qkg-cop');
                if (columnOptionsElement && event.composedPath().indexOf(columnOptionsElement) < 0) {
                    tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
                }
            };
            const keyDownHandler = (event: any) => {
                const columnOptionsElement = tableElement.tHead.querySelector('.bit-qkg-cop');
                if (columnOptionsElement && event.key === "Escape") {
                    tableElement.dispatchEvent(new CustomEvent('closecolumnoptions', { bubbles: true }));
                }
            };

            document.body.addEventListener('click', bodyClickHandler);
            document.body.addEventListener('mousedown', bodyClickHandler); // Otherwise it seems strange that it doesn't go away until you release the mouse button
            document.body.addEventListener('keydown', keyDownHandler);

            return {
                stop: () => {
                    document.body.removeEventListener('click', bodyClickHandler);
                    document.body.removeEventListener('mousedown', bodyClickHandler);
                    document.body.removeEventListener('keydown', keyDownHandler);

                    // Detach any document-level listeners left by an in-progress resize drag and clear
                    // the active drag state, so disposal/re-init mid-drag can't keep mutating a stale th.
                    if (dragState.cleanup) {
                        dragState.cleanup();
                        dragState.cleanup = null;
                    }

                    // Remove the per-handle drag listeners and clear the bound marker so a later
                    // init() can rebind the same surviving elements without duplicating handlers.
                    boundDragHandles.forEach(({ handle, listener }) => {
                        handle.removeEventListener('mousedown', listener);
                        handle.removeEventListener('touchstart', listener);
                        delete handle.__bitQkgResizeBound;
                    });
                    boundDragHandles.length = 0;
                }
            };
        }

        public static checkColumnOptionsPosition(tableElement: any) {
            const colOptions = tableElement.tHead && tableElement.tHead.querySelector('.bit-qkg-cop'); // Only match within *our* thead, not nested tables
            if (colOptions) {
                // We want the options popup to be positioned over the grid, not overflowing on either side, because it's possible that
                // beyond either side is off-screen or outside the scroll range of an ancestor
                const gridRect = tableElement.getBoundingClientRect();
                const optionsRect = colOptions.getBoundingClientRect();
                const leftOverhang = Math.max(0, gridRect.left - optionsRect.left);
                const rightOverhang = Math.max(0, optionsRect.right - gridRect.right);
                if (leftOverhang || rightOverhang) {
                    // In the unlikely event that it overhangs both sides, we'll center it
                    const applyOffset = leftOverhang && rightOverhang ? (leftOverhang - rightOverhang) / 2 : (leftOverhang - rightOverhang);
                    colOptions.style.transform = `translateX(${applyOffset}px)`;
                } else {
                    // Clear any offset left over from a previous opening so the popup isn't misaligned.
                    colOptions.style.transform = '';
                }

                if (typeof colOptions.scrollIntoViewIfNeeded === 'function') {
                    colOptions.scrollIntoViewIfNeeded();
                } else {
                    // Fall back to a nearest-edge scroll so browsers without scrollIntoViewIfNeeded
                    // don't scroll more aggressively than needed (the default scrollIntoView() can
                    // jump the popup fully into view and shift the grid).
                    colOptions.scrollIntoView({ block: 'nearest', inline: 'nearest' });
                }

                const autoFocusElem = colOptions.querySelector('[autofocus]');
                if (autoFocusElem) {
                    autoFocusElem.focus();
                }
            }
        }

        private static enableColumnResizing(tableElement: any, boundDragHandles: { handle: any, listener: any }[], dragState: { cleanup: (() => void) | null }) {
            tableElement.tHead.querySelectorAll('.bit-qkg-drg').forEach((handle: any) => {
                // Bind each handle only once. A surviving handle (reused by Blazor's diffing across
                // re-renders) would otherwise accumulate a fresh listener on every init() call.
                if (handle.__bitQkgResizeBound) return;
                handle.__bitQkgResizeBound = true;

                handle.addEventListener('mousedown', handleMouseDown);
                if ('ontouchstart' in window) {
                    handle.addEventListener('touchstart', handleMouseDown);
                }
                boundDragHandles.push({ handle, listener: handleMouseDown });

                function handleMouseDown(evt: any) {
                    evt.preventDefault();
                    evt.stopPropagation();

                    const th = handle.parentElement;
                    const startPageX = evt.touches ? evt.touches[0].pageX : evt.pageX;
                    const originalColumnWidth = th.offsetWidth;
                    const rtlMultiplier = window.getComputedStyle(th, null).getPropertyValue('direction') === 'rtl' ? -1 : 1;
                    let updatedColumnWidth = 0;

                    function handleMouseMove(evt: any) {
                        evt.stopPropagation();
                        const newPageX = evt.touches ? evt.touches[0].pageX : evt.pageX;
                        // Clamp to a minimum width so a column can't collapse to (or below) zero while dragging.
                        const minColumnWidth = 20;
                        const nextWidth = Math.max(minColumnWidth, originalColumnWidth + (newPageX - startPageX) * rtlMultiplier);
                        if (Math.abs(nextWidth - updatedColumnWidth) > 0) {
                            updatedColumnWidth = nextWidth;
                            th.style.width = `${updatedColumnWidth}px`;
                        }
                    }

                    function handleMouseUp() {
                        document.body.removeEventListener('mousemove', handleMouseMove);
                        document.body.removeEventListener('mouseup', handleMouseUp);
                        document.body.removeEventListener('touchmove', handleMouseMove);
                        document.body.removeEventListener('touchend', handleMouseUp);
                        document.body.removeEventListener('touchcancel', handleMouseUp);
                        dragState.cleanup = null;
                    }

                    if (window.TouchEvent && evt instanceof TouchEvent) {
                        document.body.addEventListener('touchmove', handleMouseMove, { passive: true });
                        document.body.addEventListener('touchend', handleMouseUp, { passive: true });
                        // A touch gesture can be interrupted (e.g. by the system) without firing touchend,
                        // which would leave the move/end listeners attached. Tear down on touchcancel too.
                        document.body.addEventListener('touchcancel', handleMouseUp, { passive: true });
                    } else {
                        document.body.addEventListener('mousemove', handleMouseMove, { passive: true });
                        document.body.addEventListener('mouseup', handleMouseUp, { passive: true });
                    }

                    // Expose this drag's teardown so stop() can detach the document-level listeners if
                    // the grid is disposed/re-initialized before the pointer is released.
                    dragState.cleanup = handleMouseUp;
                }
            });
        }
    }
}
