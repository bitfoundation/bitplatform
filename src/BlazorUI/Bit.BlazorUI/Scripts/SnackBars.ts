namespace BitBlazorUI {
    export class SnackBars {
        private static _isInitialized = false;
        private static _hotkeys: Map<string, string[]> = new Map();
        private static _swipes: Map<string, BitSnackBarSwipe> = new Map();

        /**
         * Registers the keyboard shortcut that moves the focus to a snack bar host.
         * The keys are matched against KeyboardEvent.code, with the modifier property names
         * ('altKey', 'ctrlKey', 'shiftKey', 'metaKey') standing for the modifiers, which is the
         * shape Radix Toast uses for the same feature.
         */
        public static registerHotkey(id: string, keys: string[]) {
            if (!id || !keys || keys.length === 0) {
                SnackBars.unregisterHotkey(id);
                return;
            }

            SnackBars._hotkeys.set(id, keys);

            SnackBars.init();
        }

        public static unregisterHotkey(id: string) {
            SnackBars._hotkeys.delete(id);
        }

        /**
         * Makes the items of a snack bar host draggable out of the way with a pointer.
         * One listener per host rather than one per item: the item a drag belongs to is found from the
         * event's target, so an item that arrives later needs no setup and one that leaves needs no teardown.
         */
        public static registerSwipe(id: string, threshold: number, dotnetObj: DotNetObject) {
            const element = document.getElementById(id);
            if (!element) return;

            SnackBars.unregisterSwipe(id);

            const swipe: BitSnackBarSwipe = {
                element,
                threshold: threshold > 0 ? threshold : 50,
                dotnetObj,
                onPointerDown: null!
            };

            swipe.onPointerDown = (e: PointerEvent) => SnackBars.startSwipe(swipe, e);

            element.addEventListener('pointerdown', swipe.onPointerDown);

            SnackBars._swipes.set(id, swipe);
        }

        public static unregisterSwipe(id: string) {
            const swipe = SnackBars._swipes.get(id);
            if (!swipe) return;

            swipe.element.removeEventListener('pointerdown', swipe.onPointerDown);

            SnackBars._swipes.delete(id);
        }

        private static startSwipe(swipe: BitSnackBarSwipe, e: PointerEvent) {
            // Only the primary button of a mouse drags; the other buttons belong to the context menu and to
            // whatever the browser does with them.
            if (e.button !== 0) return;

            const item = (e.target as HTMLElement)?.closest?.('.bit-snb-itm') as HTMLElement | null;
            if (!item) return;

            const itemId = item.getAttribute('data-bit-snb-id');
            if (!itemId) return;

            const startX = e.clientX;
            let dragging = false;

            const onMove = (move: PointerEvent) => {
                const dx = move.clientX - startX;

                // A few pixels of slop before this counts as a drag, so a click that wobbles is still a click
                // and a vertical scroll that starts on a notification is still a scroll.
                if (!dragging && Math.abs(dx) < 6) return;

                dragging = true;

                item.style.transition = 'none';
                item.style.transform = `translateX(${dx}px)`;
                item.style.opacity = `${Math.max(0, 1 - Math.abs(dx) / (swipe.threshold * 3))}`;
            };

            const onUp = (up: PointerEvent) => {
                document.removeEventListener('pointermove', onMove);
                document.removeEventListener('pointerup', onUp);
                document.removeEventListener('pointercancel', onCancel);

                if (!dragging) return;

                // The click that follows a real drag is not one the user meant, so it is swallowed before it
                // reaches the item's own click handler.
                document.addEventListener('click', (click: Event) => {
                    click.stopPropagation();
                    click.preventDefault();
                }, { capture: true, once: true });

                const dx = up.clientX - startX;

                if (Math.abs(dx) >= swipe.threshold) {
                    // The item carries on the way it was thrown rather than snapping back to play the standard
                    // exit animation. The marker is a data attribute rather than a class because the class
                    // attribute is Blazor's to write, and the next render would take a class of ours away again.
                    item.setAttribute('data-bit-snb-swiped', 'true');
                    item.style.transition = '';
                    item.style.transform = `translateX(${dx > 0 ? 150 : -150}%)`;
                    item.style.opacity = '0';

                    swipe.dotnetObj.invokeMethodAsync('SwipeDismissed', itemId);
                    return;
                }

                SnackBars.resetSwipe(item);
            };

            const onCancel = () => {
                document.removeEventListener('pointermove', onMove);
                document.removeEventListener('pointerup', onUp);
                document.removeEventListener('pointercancel', onCancel);

                if (dragging) SnackBars.resetSwipe(item);
            };

            // The listeners are on the document rather than on the item: a pointer that leaves the item mid-drag
            // has to keep being followed, and the item can be taken out of the DOM under it at any moment.
            document.addEventListener('pointermove', onMove);
            document.addEventListener('pointerup', onUp);
            document.addEventListener('pointercancel', onCancel);
        }

        private static resetSwipe(item: HTMLElement) {
            item.style.transition = '';
            item.style.transform = '';
            item.style.opacity = '';
        }

        private static init() {
            if (SnackBars._isInitialized) return;

            SnackBars._isInitialized = true;

            // One listener for every host on the page: the map is what tells them apart, so a page with several
            // snack bar hosts does not add a document listener per host.
            document.addEventListener('keydown', (e: KeyboardEvent) => {
                if (SnackBars._hotkeys.size === 0) return;

                for (const [id, keys] of SnackBars._hotkeys) {
                    if (!SnackBars.matches(e, keys)) continue;

                    const element = document.getElementById(id);
                    if (!element) continue;

                    e.preventDefault();
                    element.focus();

                    return;
                }
            });
        }

        private static matches(e: KeyboardEvent, keys: string[]) {
            return keys.every(key => key === 'altKey' ? e.altKey
                : key === 'ctrlKey' ? e.ctrlKey
                : key === 'shiftKey' ? e.shiftKey
                : key === 'metaKey' ? e.metaKey
                : key === e.code);
        }
    }

    interface BitSnackBarSwipe {
        element: HTMLElement;
        threshold: number;
        dotnetObj: DotNetObject;
        onPointerDown: (e: PointerEvent) => void;
    }
}
