namespace BitBlazorUI {
    export class Dropdowns {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }[]>();

        // The focus of a jump mode in virtualize mode is applied after an await, so a second key press
        // arriving in the meantime would have two invocations scrolling and focusing over each other.
        // Each call stamps its callout with a token and gives up if a newer call has replaced it.
        // Keyed by the element so the entry disappears with the callout instead of having to be cleaned up.
        private static _focusGenerations = new WeakMap<HTMLElement, number>();

        // The number of animation frames to wait for the items revealed by a programmatic scroll in
        // virtualize mode to be rendered, before giving up and using whatever is rendered by then.
        private static readonly VIRTUALIZE_RENDER_FRAMES = 20;

        // The budget for the jump that opens a virtualized list on its selection to hold its position
        // against the list re-measuring itself under it. The list renders for the first time there, and
        // that render is a server round trip under Blazor Server, so the wait is against the clock
        // rather than a frame count; the frames are how long the position then has to stay put to
        // count as settled. Neither is spent once it does.
        private static readonly VIRTUALIZE_SETTLE_TIME = 1000;
        private static readonly VIRTUALIZE_SETTLE_FRAMES = 10;

        // The number of items PageDown/PageUp move the focus by when the whole list is rendered (in
        // virtualize mode the jump is a scroll of one visible window instead, see _scrollFor).
        private static readonly PAGE_STEP = 10;

        // The keys whose default behavior (scrolling the page or the callout) has to be prevented
        // because the dropdown handles them itself. They mirror the keys handled by the
        // HandleOnTriggerKeyDown and HandleOnCalloutKeyDown methods of BitDropdown, which is where a
        // key added to or removed from these lists has to be reflected as well.
        private static readonly TRIGGER_KEYS = ['ArrowDown', 'ArrowUp'];
        // Tab is in the list because the dropdown moves the focus back to the trigger itself when the
        // callout closes (see CloseCalloutAndRestoreFocus): letting the browser tab out of the callout
        // first would move the focus from the end of the document, where the callout is rendered.
        private static readonly CALLOUT_KEYS = ['ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Tab'];

        // These are only handled (on the trigger and in the callout alike) when the focus is not in the
        // search/combo input, where they keep their caret behavior.
        private static readonly CARET_KEYS = ['Home', 'End'];

        // The keys that CHANGE the text of the ComboBox input rather than navigate the list, so they
        // belong to the input even while the focus sits on an option. They are not in CALLOUT_KEYS,
        // so their default is not prevented and they still act on the input once it has the focus.
        private static readonly EDIT_KEYS = ['Backspace', 'Delete'];
        // Those plus the ones that only move the caret, which belong to the ComboBox input for the same
        // reason - the term is edited where it is, not by leaving the list and coming back to it.
        private static readonly TEXT_KEYS = [...Dropdowns.EDIT_KEYS, 'ArrowLeft', 'ArrowRight'];

        // Attaches keydown listeners that only prevent the default behavior (e.g. page scrolling)
        // of the navigation keys. The actual keyboard logic runs in the Blazor keydown handlers,
        // which cannot conditionally preventDefault per key.
        public static setup(id: string, calloutId: string) {
            Dropdowns.dispose(id);

            const entries: { element: HTMLElement, handler: (e: KeyboardEvent) => void }[] = [];

            const isTextInput = (e: KeyboardEvent) => (e.target as HTMLElement)?.tagName === 'INPUT';

            // Keep this in sync with IsPrintableKey of BitDropdown.
            const isPrintable = (e: KeyboardEvent) => e.key.length === 1 && e.key !== ' ' && !e.ctrlKey && !e.altKey && !e.metaKey;

            const root = document.getElementById(id);
            if (root) {
                const handler = (e: KeyboardEvent) => {
                    // Buttons inside the trigger (the clear button, the chip remove buttons) are
                    // activated by Enter and Space themselves, so those two keys are stopped here -
                    // before Blazor's document-level delegation sees them - to keep the activation
                    // from also reaching the trigger's keydown handler and toggling the callout.
                    // Every other key falls through: the arrows keep operating the dropdown (with
                    // their scrolling default prevented below) even while such a button has the focus.
                    if ((e.target as HTMLElement)?.closest('button') && (e.key === 'Enter' || e.key === ' ')) {
                        e.stopPropagation();
                        return;
                    }

                    if (Dropdowns.TRIGGER_KEYS.indexOf(e.key) > -1 ||
                        (Dropdowns.CARET_KEYS.indexOf(e.key) > -1 && !isTextInput(e)) ||
                        (e.key === ' ' && !isTextInput(e))) {
                        e.preventDefault();
                    }
                };
                root.addEventListener('keydown', handler);
                entries.push({ element: root, handler });
            }

            const callout = document.getElementById(calloutId);
            if (callout) {
                const handler = (e: KeyboardEvent) => {
                    if (Dropdowns.CALLOUT_KEYS.indexOf(e.key) > -1 ||
                        (Dropdowns.CARET_KEYS.indexOf(e.key) > -1 && !isTextInput(e))) {
                        e.preventDefault();
                        return;
                    }

                    // Ctrl/Cmd+A runs the select all of a multi select dropdown (see
                    // HandleOnCalloutKeyDown), so the browser's select-the-page-text default has to be
                    // prevented - except in the search/combo inputs, where it keeps selecting the text.
                    if ((e.ctrlKey || e.metaKey) && (e.key === 'a' || e.key === 'A') && !isTextInput(e) &&
                        callout.querySelector('.bit-drp-scn')?.getAttribute('aria-multiselectable') === 'true') {
                        e.preventDefault();
                        return;
                    }

                    // In ComboBox mode the input is the type-ahead, so the arrow keys having moved the
                    // focus to an option must not stop the editing of the term: the keys that belong to
                    // the text return the focus to the input, and since their default is not prevented
                    // they act on it there - a character is typed, a Backspace deletes, a caret moves.
                    // The space bar is one of those characters: it is only the toggle of the focused
                    // option where there is no text being typed, which the combo lookup below decides.
                    if (!isTextInput(e) && (isPrintable(e) || e.key === ' ' || Dropdowns.TEXT_KEYS.indexOf(e.key) > -1)) {
                        const combo = (callout.querySelector('.bit-drp-icb') ??
                                       root?.querySelector('.bit-drp-inp')) as HTMLElement | null;
                        if (combo) {
                            combo.focus();
                            return;
                        }

                        // A search box filters the same list, so the keys that edit text belong to it for
                        // the same reason - typing after the arrow keys refines the search instead of
                        // starting a type-ahead the search box has already made redundant (see
                        // HandleOnCalloutKeyDown). Two keys are left out where the ComboBox input takes
                        // them: the space bar, which on a dropdown that is not typed into is the toggle of
                        // the focused option and has nothing to replace it, and the caret keys, which have
                        // no caret to move on an option and would only pull the focus off the row the
                        // arrow keys had reached.
                        if (isPrintable(e) || Dropdowns.EDIT_KEYS.indexOf(e.key) > -1) {
                            (callout.querySelector('.bit-drp-sin') as HTMLElement | null)?.focus();
                        }
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

        // Focuses the search box of a callout and puts the caret at the given offset. It is what the
        // character typed on a CLOSED dropdown lands in (see TypeIntoSearchBox): the caret is placed by
        // hand because a browser that focuses an input by script may leave it in front of the value, which
        // would have the next character typed ahead of the one that opened the list.
        public static focusSearchBox(calloutId: string, caret: number) {
            const input = document.getElementById(calloutId)?.querySelector('.bit-drp-sin') as HTMLInputElement | null;
            if (!input) return;

            input.focus();

            try {
                input.setSelectionRange(caret, caret);
            } catch (e) {
                // An input that does not support a text selection cannot be given a caret; it has the focus either way.
            }
        }

        public static async focusItem(calloutId: string, mode: string, char: string | null, virtualize: boolean,
                                      selectedIndex: number = -1, itemSize: number = 0, noWrap: boolean = false) {
            const callout = document.getElementById(calloutId);
            if (!callout) return;

            const scroller = callout.querySelector('.bit-drp-scn') as HTMLElement | null;

            const generation = (Dropdowns._focusGenerations.get(callout) ?? 0) + 1;
            Dropdowns._focusGenerations.set(callout, generation);

            // Only the jump that opens a virtualized list on its selection waits for that list: it is the
            // one whose rows are rendered by the very reveal it follows, and the one that has a row to
            // reach. Everywhere else an empty list is an empty list, and a wait would only hold the focus
            // back - or, on a list still being fetched, put it on a row the next window replaces.
            let items = (virtualize && mode === 'selected' && selectedIndex > -1)
                            ? await Dropdowns._waitForItems(callout)
                            : Dropdowns._getItems(callout);

            // A newer key press took over while the list was being waited for.
            if (Dropdowns._focusGenerations.get(callout) !== generation) return;

            if (items.length === 0) return;

            let current = items.indexOf(document.activeElement as HTMLElement);

            // In virtualize mode only the items around the visible window exist in the DOM, so moving
            // beyond it means scrolling first and continuing with the items rendered afterwards.
            if (virtualize && Dropdowns._isScrollable(scroller)) {
                const scrolled = await Dropdowns._scrollFor(callout, scroller!, mode, items, selectedIndex, itemSize);

                // A newer key press took over while the scroll was being rendered, so this one is
                // working from an outdated window and must not move the focus back to it.
                if (Dropdowns._focusGenerations.get(callout) !== generation) return;

                if (scrolled) {
                    items = scrolled.items;
                    current = scrolled.items.indexOf(document.activeElement as HTMLElement);
                    mode = scrolled.mode;

                    // The new window can come back empty (the scroll landed on a range that renders
                    // nothing), and every mode resolves to an index in a list that has no items.
                    if (items.length === 0) return;
                }
            }

            const index = Dropdowns._resolveIndex(items, current, mode, char, virtualize, noWrap);

            if (index > -1) {
                items[index].focus();
            }
        }

        // The callout is revealed by the call right before this one, and the rows of a virtualized list
        // are rendered only once that reveal has been laid out - so an empty list on the first opening
        // means "not yet" rather than "nothing to focus", and giving up on it is what would leave that
        // first opening on the top of the list with the focus still on the trigger. A list that really
        // is empty costs the frames below once and focuses nothing either way.
        private static async _waitForItems(callout: HTMLElement) {
            let items = Dropdowns._getItems(callout);

            for (let i = 0; items.length === 0 && i < Dropdowns.VIRTUALIZE_RENDER_FRAMES; i++) {
                await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

                items = Dropdowns._getItems(callout);
            }

            return items;
        }

        // The height one row actually takes, measured over two of them so that whatever sits between
        // them is counted as well. ItemSize is what Virtualize is TOLD to expect and what it renders
        // its first window at; the scroll below has to be measured in what the list ends up rendering
        // at instead, or a theme whose rows are a different height lands it hundreds of rows away.
        // Measured off the layout box and never off getBoundingClientRect: the callout scales in as it
        // opens, so the painted rect of a row is short for the length of that animation - which is
        // exactly when the first opening measures it - and the scroll would land short with it.
        private static _measureItemSize(scroller: HTMLElement) {
            const rows = scroller.querySelectorAll('[role="option"]') as NodeListOf<HTMLElement>;
            if (rows.length === 0) return 0;

            if (rows.length > 1 && rows[0].offsetParent === rows[1].offsetParent) {
                const pitch = rows[1].offsetTop - rows[0].offsetTop;
                if (pitch > 0) return pitch;
            }

            return rows[0].offsetHeight;
        }

        // The options plus the select all item, which is not an option (it is a checkbox outside of the
        // listbox) but still has to be reachable with the arrow keys.
        private static _getItems(callout: HTMLElement) {
            return (Array.from(callout.querySelectorAll('[role="option"], .bit-drp-sab')) as HTMLElement[])
                .filter(el => !(el as HTMLButtonElement).disabled &&
                    el.getAttribute('aria-disabled') !== 'true' &&
                    !el.closest('.bit-drp-ids') &&
                    el.offsetParent !== null);
        }

        private static _isScrollable(scroller: HTMLElement | null) {
            return !!scroller && scroller.scrollHeight > scroller.clientHeight + 1;
        }

        // Scrolls the item container for the jump modes and resolves once the items revealed by the
        // scroll have been rendered, mapping the mode to where the focus goes in the new window.
        private static async _scrollFor(callout: HTMLElement, scroller: HTMLElement, mode: string, items: HTMLElement[],
                                       selectedIndex: number = -1, itemSize: number = 0) {
            let max = scroller.scrollHeight - scroller.clientHeight;

            let top: number;
            let nextMode = mode;

            if (mode === 'first') {
                top = 0;
            } else if (mode === 'last') {
                top = max;
            } else if (mode === 'selected') {
                // Opening a long list on a selection that has never been rendered: the element is not in
                // the DOM to be found, so the list is scrolled to where its index says it is first. It is
                // centred in the window rather than pinned to the top, which is what a native select does
                // and what shows the items around the selection instead of only the ones after it.
                if (selectedIndex < 0) return null;

                const size = Dropdowns._measureItemSize(scroller) || itemSize;
                if (size <= 0) return null;

                top = (selectedIndex * size) - ((scroller.clientHeight - size) / 2);

                // A list that is being opened for the first time is still growing to the height of the
                // whole set, so the clamp below would cut the scroll off hundreds of rows short of the
                // selection - which is what the very first opening would otherwise land on. Wait for it
                // to be tall enough to hold the target before scrolling to it.
                for (let i = 0; top > max && i < Dropdowns.VIRTUALIZE_RENDER_FRAMES; i++) {
                    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

                    max = scroller.scrollHeight - scroller.clientHeight;
                }
            } else if (mode === 'prevPage') {
                top = scroller.scrollTop - scroller.clientHeight;
                nextMode = 'first';
            } else if (mode === 'nextPage') {
                top = scroller.scrollTop + scroller.clientHeight;
                nextMode = 'last';
            } else {
                // The arrow keys move one item at a time, which scrolls the focused item into view and
                // makes virtualization render the next ones on its own.
                return null;
            }

            top = Math.max(0, Math.min(max, top));
            if (top === scroller.scrollTop) return null;

            // Which window is rendered, rather than which elements: Blazor updates the reused rows of a
            // Virtualize in place, so the ends of the new window are usually the same elements carrying
            // different items - a comparison by identity alone would never see the re-render and would
            // wait out every frame below before falling back.
            const windowOf = (rendered: HTMLElement[]) => rendered.length === 0 ? '' :
                [rendered[0].id, rendered[0].textContent,
                 rendered[rendered.length - 1].id, rendered[rendered.length - 1].textContent].join(' ');

            const before = windowOf(items);

            scroller.scrollTop = top;

            // The jump that opens a list on its selection is the one that has to hold: the list is
            // rendered for the first time under it, re-measures itself as the window it was scrolled
            // to arrives, and the height it holds open for the rest of the set moves the scroll
            // position with it - which is what would otherwise leave that first opening hundreds of
            // rows above the selection. The position is asked for again until it stays, because the
            // re-render it is racing is a round trip of its own under Blazor Server.
            if (mode === 'selected') {
                const deadline = performance.now() + Dropdowns.VIRTUALIZE_SETTLE_TIME;

                for (let held = 0; held < Dropdowns.VIRTUALIZE_SETTLE_FRAMES && performance.now() < deadline;) {
                    await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

                    if (scroller.scrollTop === top) {
                        held++;
                        continue;
                    }

                    held = 0;

                    // Anything shorter is a list still growing to the height of the whole set, which
                    // there is nothing to ask for yet - the next frames are what it is waited for in.
                    if (top <= scroller.scrollHeight - scroller.clientHeight) {
                        scroller.scrollTop = top;
                    }
                }
            }

            for (let i = 0; i < Dropdowns.VIRTUALIZE_RENDER_FRAMES; i++) {
                await new Promise<void>(resolve => requestAnimationFrame(() => resolve()));

                const rendered = Dropdowns._getItems(callout);
                if (rendered.length > 0 && windowOf(rendered) !== before) {
                    return { items: rendered, mode: nextMode };
                }
            }

            return { items: Dropdowns._getItems(callout), mode: nextMode };
        }

        private static _resolveIndex(items: HTMLElement[], current: number, mode: string, char: string | null,
                                    virtualize: boolean, noWrap: boolean = false) {
            // Wrapping around is only correct when every item is rendered; in virtualize mode the ends
            // of the rendered window are not the ends of the list, so the focus stops there instead -
            // and so it does when the dropdown was asked to stop there (noWrap).
            const wrap = (index: number) => (virtualize || noWrap)
                ? Math.max(0, Math.min(items.length - 1, index))
                : (index + items.length) % items.length;

            if (mode === 'first') return 0;

            if (mode === 'last') return items.length - 1;

            // The very first arrow press has no item to move from, so it starts at the end it is
            // heading away from: ArrowDown at the first item and ArrowUp at the last one - which
            // stopping at the ends does not change, since neither of them steps over one.
            if (mode === 'next') return current < 0 ? 0 : wrap(current + 1);

            if (mode === 'prev') return current < 0 ? items.length - 1 : wrap(current - 1);

            if (mode === 'nextPage') return current < 0 ? 0 : Math.min(current + Dropdowns.PAGE_STEP, items.length - 1);

            if (mode === 'prevPage') return current < 0 ? items.length - 1 : Math.max(current - Dropdowns.PAGE_STEP, 0);

            if (mode === 'selected') {
                // Focus the selected option if there is one, otherwise the first one (APG combobox pattern).
                const index = items.findIndex(el => el.classList.contains('bit-drp-sel') || el.getAttribute('aria-selected') === 'true');
                return index < 0 ? 0 : index;
            }

            if (mode === 'char' && char) {
                // Type-ahead per the APG: a repeated single character cycles through the options starting
                // with it, while a multi-character buffer matches the accumulated string without leaving
                // the current option (so typing a longer prefix refines the match instead of jumping).
                const buffer = char.toLowerCase();
                const sameChar = buffer.split('').every(c => c === buffer[0]);
                const query = sameChar ? buffer[0] : buffer;
                const start = current < 0 ? 0 : (sameChar ? current + 1 : current);
                for (let i = 0; i < items.length; i++) {
                    const candidate = (start + i) % items.length;
                    // The select all item is not an option, so its text must not be matched.
                    if (items[candidate].getAttribute('role') !== 'option') continue;

                    if ((items[candidate].textContent || '').trim().toLowerCase().indexOf(query) === 0) {
                        return candidate;
                    }
                }
            }

            return -1;
        }
    }
}
