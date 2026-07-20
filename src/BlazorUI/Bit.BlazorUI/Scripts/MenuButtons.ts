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

        public static focusItem(calloutId: string, mode: string, char: string | null) {
            const callout = document.getElementById(calloutId);
            if (!callout) return;

            const items = (Array.from(callout.querySelectorAll('.bit-mnb-itm')) as HTMLElement[])
                .filter(el => !(el as HTMLButtonElement).disabled && el.getAttribute('aria-disabled') !== 'true');
            if (items.length === 0) return;

            const current = items.indexOf(document.activeElement as HTMLElement);
            let index = -1;

            if (mode === 'first') {
                index = 0;
            } else if (mode === 'last') {
                index = items.length - 1;
            } else if (mode === 'next') {
                index = current < 0 ? 0 : (current + 1) % items.length;
            } else if (mode === 'prev') {
                index = current < 0 ? items.length - 1 : (current - 1 + items.length) % items.length;
            } else if (mode === 'char' && char) {
                const c = char.toLowerCase();
                const start = current < 0 ? 0 : current + 1;
                for (let i = 0; i < items.length; i++) {
                    const candidate = (start + i) % items.length;
                    if ((items[candidate].textContent || '').trim().toLowerCase().indexOf(c) === 0) {
                        index = candidate;
                        break;
                    }
                }
            }

            if (index > -1) {
                items[index].focus();
            }
        }
    }
}
