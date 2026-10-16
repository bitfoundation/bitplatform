namespace BitBlazorUI {
    export class MenuButtons {
        private static _handlers = new Map<string, MenuButtonListener[]>();

        // Only the rows of THIS menu, and never the rows of a submenu that is still sitting inside it
        // unopened: a closed submenu is hidden rather than taken out of the page, and focusing a hidden
        // row does nothing at all - which would silently stop the arrow keys on the menu around it.
        private static readonly ITEM_SELECTOR = ':scope > ul > li > .bit-mnb-itm';

        // Attaches keydown listeners that only prevent the default behavior (e.g. page scrolling)
        // of the navigation keys. The actual keyboard logic runs in the Blazor keydown handlers,
        // which cannot conditionally preventDefault per key.
        public static setup(id: string, calloutId: string) {
            MenuButtons.dispose(id);

            const entries: MenuButtonListener[] = [];

            const root = document.getElementById(id);
            if (root) {
                const handler = (e: KeyboardEvent) => {
                    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
                        e.preventDefault();
                    }
                };
                root.addEventListener('keydown', handler);
                entries.push({ id: id, element: root, type: 'keydown', handler: handler as EventListener });
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

                entries[i].element.removeEventListener(entries[i].type, entries[i].handler);
                entries.splice(i, 1);
            }
        }

        public static dispose(id: string) {
            const entries = MenuButtons._handlers.get(id);
            if (!entries) return;

            entries.forEach(e => e.element.removeEventListener(e.type, e.handler));
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

            // A submit or reset item belongs to the form the menu button sits in, but its menu is moved to the
            // body while it is open, out of that form - and a button with no form owner does nothing on a
            // click. So what the browser would have done had the item stayed in place is done here, for the
            // form around the menu button itself: only for an item no form owns (a FormId or a form attribute
            // still reaches its form natively, wherever the item is), only for one that is not unavailable
            // (an aria-disabled item kept focusable runs none of its actions), and only once the click has
            // been dispatched and its default not prevented, which is when the browser runs the submission.
            // A click handled in the same task (WebAssembly) can close the menu - moving the item back into the
            // form - before the browser activates it, and then the browser submits it after all; so it is done
            // here only when the form has seen nothing of the kind since the click.
            const clickHandler = (e: MouseEvent) => {
                const item = (e.target as HTMLElement)?.closest?.('button.bit-mnb-itm') as HTMLButtonElement | null;
                if (!item || item.closest('.bit-mnb-cal') !== callout) return;
                if (item.type !== 'submit' && item.type !== 'reset') return;
                if (item.form || item.disabled || item.getAttribute('aria-disabled') === 'true') return;

                const form = document.getElementById(id)?.closest('form');
                if (!form) return;

                const isReset = item.type === 'reset';
                const eventName = isReset ? 'reset' : 'submit';

                let handled = false;
                const onNative = () => { handled = true; };
                form.addEventListener(eventName, onNative, true);

                setTimeout(() => {
                    form.removeEventListener(eventName, onNative, true);

                    if (handled || e.defaultPrevented || !form.isConnected) return;

                    if (isReset) {
                        form.reset();
                    } else if (typeof form.requestSubmit === 'function') {
                        // requestSubmit rather than submit: only the former runs the constraint validation
                        // and raises the submit event, which is the one an EditForm listens to.
                        form.requestSubmit();
                    }
                });
            };

            callout.addEventListener('keydown', handler);
            callout.addEventListener('click', clickHandler);

            const added: MenuButtonListener[] = [
                { id: calloutId, element: callout, type: 'keydown', handler: handler as EventListener },
                { id: calloutId, element: callout, type: 'click', handler: clickHandler as EventListener }
            ];

            const entries = MenuButtons._handlers.get(id);
            if (entries) {
                entries.push(...added);
            } else {
                MenuButtons._handlers.set(id, added);
            }
        }
    }

    interface MenuButtonListener {
        id: string;
        element: HTMLElement;
        type: string;
        handler: EventListener;
    }
}
