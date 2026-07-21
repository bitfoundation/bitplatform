namespace BitBlazorUI {
    export class Dropdowns {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }[]>();

        // Attaches keydown listeners that only prevent the default behavior (e.g. page scrolling)
        // of the navigation keys. The actual keyboard logic runs in the Blazor keydown handlers,
        // which cannot conditionally preventDefault per key.
        public static setup(id: string, calloutId: string) {
            Dropdowns.dispose(id);

            const entries: { element: HTMLElement, handler: (e: KeyboardEvent) => void }[] = [];

            const isTextInput = (e: KeyboardEvent) => (e.target as HTMLElement)?.tagName === 'INPUT';

            const root = document.getElementById(id);
            if (root) {
                const handler = (e: KeyboardEvent) => {
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || (e.key === ' ' && !isTextInput(e))) {
                        e.preventDefault();
                    }
                };
                root.addEventListener('keydown', handler);
                entries.push({ element: root, handler });
            }

            const callout = document.getElementById(calloutId);
            if (callout) {
                const handler = (e: KeyboardEvent) => {
                    // Home/End must keep their caret behavior while typing in the search/combo inputs.
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp' || e.key === 'PageDown' || e.key === 'PageUp' ||
                        ((e.key === 'Home' || e.key === 'End') && !isTextInput(e))) {
                        e.preventDefault();
                    }
                };
                callout.addEventListener('keydown', handler);
                entries.push({ element: callout, handler });
            }

            Dropdowns._handlers.set(id, entries);
        }

        public static dispose(id: string) {
            const entries = Dropdowns._handlers.get(id);
            if (!entries) return;

            entries.forEach(e => e.element.removeEventListener('keydown', e.handler));
            Dropdowns._handlers.delete(id);
        }

        public static focusItem(calloutId: string, mode: string, char: string | null) {
            const callout = document.getElementById(calloutId);
            if (!callout) return;

            const items = (Array.from(callout.querySelectorAll('.bit-drp-itm, .bit-drp-mcn')) as HTMLElement[])
                .filter(el => !(el as HTMLButtonElement).disabled &&
                    el.getAttribute('aria-disabled') !== 'true' &&
                    !el.closest('.bit-drp-ids') &&
                    el.offsetParent !== null);
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
            } else if (mode === 'nextPage') {
                index = current < 0 ? 0 : Math.min(current + 10, items.length - 1);
            } else if (mode === 'prevPage') {
                index = current < 0 ? items.length - 1 : Math.max(current - 10, 0);
            } else if (mode === 'selected') {
                // Focus the selected option if there is one, otherwise the first one (APG combobox pattern).
                index = items.findIndex(el => el.classList.contains('bit-drp-sel') || el.getAttribute('aria-selected') === 'true');
                if (index < 0) {
                    index = 0;
                }
            } else if (mode === 'char' && char) {
                // Type-ahead per the APG: a repeated single character cycles through the options starting
                // with it, while a multi-character buffer matches the accumulated string without leaving
                // the current option (so typing a longer prefix refines the match instead of jumping).
                const buffer = char.toLowerCase();
                const sameChar = buffer.split('').every(c => c === buffer[0]);
                const query = sameChar ? buffer[0] : buffer;
                const start = current < 0 ? 0 : (sameChar ? current + 1 : current);
                for (let i = 0; i < items.length; i++) {
                    const candidate = (start + i) % items.length;
                    if ((items[candidate].textContent || '').trim().toLowerCase().indexOf(query) === 0) {
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
