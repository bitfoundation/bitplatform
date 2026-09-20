namespace BitBlazorUI {
    export class MenuButtons {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }[]>();

        // Attaches keydown listeners that only prevent the default behavior (e.g. page scrolling)
        // of the navigation keys. The actual keyboard logic runs in the Blazor keydown handlers,
        // which cannot conditionally preventDefault per key.
        public static setup(id: string, calloutId: string) {
            MenuButtons.dispose(id);

            const entries: { element: HTMLElement, handler: (e: KeyboardEvent) => void }[] = [];

            const root = document.getElementById(id);
            if (root) {
                const handler = (e: KeyboardEvent) => {
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                        e.preventDefault();
                    }
                };
                root.addEventListener('keydown', handler);
                entries.push({ element: root, handler });
            }

            const callout = document.getElementById(calloutId);
            if (callout) {
                const handler = (e: KeyboardEvent) => {
                    if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Tab'].indexOf(e.key) !== -1) {
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
                entries.push({ element: callout, handler });
            }

            MenuButtons._handlers.set(id, entries);
        }

        public static dispose(id: string) {
            const entries = MenuButtons._handlers.get(id);
            if (!entries) return;

            entries.forEach(e => e.element.removeEventListener('keydown', e.handler));
            MenuButtons._handlers.delete(id);
        }

        public static focusItem(calloutId: string, mode: string, char: string | null, includeDisabled?: boolean, fromCurrent?: boolean) {
            Utils.focusItem(calloutId, '.bit-mnb-itm', mode, char, includeDisabled === true, fromCurrent === true);
        }
    }
}
