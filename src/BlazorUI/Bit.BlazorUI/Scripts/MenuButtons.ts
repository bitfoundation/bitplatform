namespace BitBlazorUI {
    export class MenuButtons {
        private static _handlers = new Map<string, { id: string, element: HTMLElement, handler: (e: KeyboardEvent) => void }[]>();

        // Only the rows of THIS menu, and never the rows of a submenu that is still sitting inside it
        // unopened: a closed submenu is hidden rather than taken out of the page, and focusing a hidden
        // row does nothing at all - which would silently stop the arrow keys on the menu around it.
        private static readonly ITEM_SELECTOR = ':scope > ul > li > .bit-mnb-itm';

        // Attaches keydown listeners that only prevent the default behavior (e.g. page scrolling)
        // of the navigation keys. The actual keyboard logic runs in the Blazor keydown handlers,
        // which cannot conditionally preventDefault per key.
        public static setup(id: string, calloutId: string) {
            MenuButtons.dispose(id);

            const entries: { id: string, element: HTMLElement, handler: (e: KeyboardEvent) => void }[] = [];

            const root = document.getElementById(id);
            if (root) {
                const handler = (e: KeyboardEvent) => {
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                        e.preventDefault();
                    }
                };
                root.addEventListener('keydown', handler);
                entries.push({ id: id, element: root, handler });
            }

            MenuButtons._handlers.set(id, entries);

            MenuButtons.attachMenu(id, calloutId);
        }

        // The same listener on a submenu, which is a menu of its own: it is moved to the body while it is
        // open, so the keys pressed in it never reach the menu it was opened from. It is registered under
        // the menu button's id so that disposing the component takes every menu of it away at once.
        public static setupSubmenu(id: string, calloutId: string) {
            MenuButtons.attachMenu(id, calloutId);
        }

        public static disposeSubmenu(id: string, calloutId: string) {
            const entries = MenuButtons._handlers.get(id);
            if (!entries) return;

            for (let i = entries.length - 1; i >= 0; i--) {
                if (entries[i].id !== calloutId) continue;

                entries[i].element.removeEventListener('keydown', entries[i].handler);
                entries.splice(i, 1);
            }
        }

        public static dispose(id: string) {
            const entries = MenuButtons._handlers.get(id);
            if (!entries) return;

            entries.forEach(e => e.element.removeEventListener('keydown', e.handler));
            MenuButtons._handlers.delete(id);
        }

        public static focusItem(calloutId: string, mode: string, char: string | null, includeDisabled?: boolean, fromCurrent?: boolean) {
            Utils.focusItem(calloutId, MenuButtons.ITEM_SELECTOR, mode, char, includeDisabled === true, fromCurrent === true);
        }

        private static attachMenu(id: string, calloutId: string) {
            const callout = document.getElementById(calloutId);
            if (!callout) return;

            MenuButtons.disposeSubmenu(id, calloutId);

            const handler = (e: KeyboardEvent) => {
                // Only the menu the key was pressed in answers it: a submenu is moved to the body while it
                // is open, so nothing bubbles from it into the menu it was opened from, but a submenu that
                // is still closed is a hidden part of this one and must not speak for it either.
                const menu = (e.target as HTMLElement)?.closest?.('.bit-mnb-cal');
                if (menu !== callout) return;

                if (['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'Tab'].indexOf(e.key) !== -1) {
                    e.preventDefault();
                    return;
                }

                // Space activates the focused menu item (the APG menu pattern). A button item already does
                // that natively, but an anchor one does not - it only answers to Enter - so the key is
                // turned into the click the anchor would have got from the pointer, which is also what
                // carries out the navigation its href asks for. Prevented either way so the callout does
                // not scroll underneath the item that was just activated.
                if (e.key === ' ') {
                    const anchor = (e.target as HTMLElement)?.closest?.('a.bit-mnb-itm') as HTMLElement | null;
                    if (anchor) {
                        e.preventDefault();
                        anchor.click();
                    }
                }
            };

            callout.addEventListener('keydown', handler);

            const entries = MenuButtons._handlers.get(id);
            if (entries) {
                entries.push({ id: calloutId, element: callout, handler });
            } else {
                MenuButtons._handlers.set(id, [{ id: calloutId, element: callout, handler }]);
            }
        }
    }
}
