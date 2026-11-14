namespace BitBlazorUI {
    // The input of a masked field, marked while the last write to its text is the mask's layout (see setupMaskedChange).
    type MaskedInput = HTMLInputElement & { _bitMaskWrote?: boolean };

    export class TextField {
        // The three concerns of a text field (the multiline behaviors, the ghost text and the IME guard)
        // are wired independently and can be turned on and off at any point in the life of a component,
        // so each of them owns its AbortController instead of sharing a single one.
        private static _abortControllers: { [key: string]: { [feature: string]: AbortController } } = {};
        private static _ghostTexts: { [key: string]: string } = {};
        // The suggestion Escape rejected, kept away until the field is typed into or a different one arrives.
        private static _rejectedGhostTexts: { [key: string]: string } = {};
        private static _maxRows: { [key: string]: number | null } = {};
        private static _inputElements: { [key: string]: HTMLInputElement } = {};
        private static _resizeObservers: { [key: string]: ResizeObserver } = {};

        private static getSignal(id: string, feature: string): AbortSignal {
            const features = TextField._abortControllers[id] ?? (TextField._abortControllers[id] = {});

            // Setting a feature up again replaces its listeners rather than adding a second copy of them,
            // which is what makes every setup call below safe to repeat.
            features[feature]?.abort();

            const ac = new AbortController();
            features[feature] = ac;

            return ac.signal;
        }

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean, maxRows: number | null) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'multiline');
            TextField._maxRows[id] = maxRows ?? null;

            TextField.disconnectResizeObserver(id);

            if (autoHeight) {
                inputElement.addEventListener('input', e => {
                    TextField.resize(inputElement, TextField._maxRows[id]);
                }, { signal });

                // Typing is not the only thing that changes how many lines the content takes: a narrower
                // window, a collapsing side panel or a font that finishes loading all re-wrap the text, and
                // the height measured before any of that would leave the field either clipped or half empty.
                if (typeof ResizeObserver !== 'undefined') {
                    let lastWidth = inputElement.clientWidth;
                    let frame = 0;

                    const observer = new ResizeObserver(() => {
                        // Only the width can re-wrap the content. Measuring again on every height change
                        // would react to the resize this observer itself causes, which is the loop the
                        // browser warns about.
                        const width = inputElement.clientWidth;
                        if (width === lastWidth) return;

                        lastWidth = width;

                        // The height is written on the next frame rather than in here: a size changed inside the
                        // callback is one the observer has to report again within the same frame, which it refuses
                        // with the "loop completed with undelivered notifications" error - an uncaught error of the
                        // page in WebKit - even though the notification it held back would only have been ignored.
                        cancelAnimationFrame(frame);
                        frame = requestAnimationFrame(() => {
                            frame = 0;

                            // The observer may have been let go of since the frame was asked for - the auto growing
                            // turned off, the feature set up again, the field disposed - and a height written now
                            // would be one nothing clears any more.
                            if (signal.aborted) return;

                            TextField.resize(inputElement, TextField._maxRows[id]);

                            // Growing the element can take a scrollbar away, which widens it again. Reading the
                            // width back is what keeps that second notification from being taken for a new
                            // change and measuring all over again.
                            lastWidth = inputElement.clientWidth;
                        });
                    });

                    // Every way the observer is let go of (setting the feature up again, disposeFeature, dispose)
                    // aborts this feature's signal too, which takes a frame still waiting along with it.
                    signal.addEventListener('abort', () => cancelAnimationFrame(frame), { once: true });

                    observer.observe(inputElement);
                    TextField._resizeObservers[id] = observer;
                }
            } else {
                // The heights the auto growing left behind are inline styles, so they would keep the
                // element at the size of its content long after the feature was turned off. Clearing them
                // hands the sizing back to the Rows attribute and to the stylesheet.
                inputElement.style.height = '';
                inputElement.style.overflowY = '';
            }

            if (preventEnter) {
                inputElement.addEventListener('keydown', e => {
                    // Enter commits the candidate of an input method editor, so it must keep its meaning
                    // while a composition session is running. The keyCode check covers the browsers that
                    // report the legacy 229 instead of setting isComposing.
                    if (Utils.isComposingKey(e)) return;

                    if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                    }
                }, { signal });
            }
        }

        public static adjustHeight(id: string, inputElement: HTMLInputElement, maxRows: number | null) {
            if (!inputElement) return;

            TextField._maxRows[id] = maxRows ?? null;

            TextField.resize(inputElement, maxRows ?? null);
        }

        // Measuring collapses the input, which makes every scroll container around it shorter for a moment.
        // Firefox and WebKit clamp the scroll position of those containers to the shorter content and never
        // give it back once the input grows again, so everything below the field moves - a click that is in
        // progress (the one whose blur re-rendered the field) then lands on whatever took its place. The
        // positions are recorded before the collapse and put back once the final height is set.
        private static resize(inputElement: HTMLInputElement, maxRows: number | null) {
            const scrolled: [Element, number][] = [];

            // A container scrolled to its very top has nothing a shorter content could clamp.
            for (let el = inputElement.parentElement; el; el = el.parentElement) {
                if (el.scrollTop > 0) scrolled.push([el, el.scrollTop]);
            }

            TextField.setHeight(inputElement, maxRows);

            for (const [el, top] of scrolled) {
                if (el.scrollTop === top) continue;

                // A plain assignment is animated by a container with scroll-behavior: smooth, and the content
                // would stay displaced while it runs. Safari before 16.4 rejects 'instant' with a TypeError, so
                // it falls back to the assignment rather than leave the remaining containers unrestored.
                try {
                    el.scrollTo({ top, behavior: 'instant' });
                } catch {
                    el.scrollTop = top;
                }
            }
        }

        // Collapses the input first so scrollHeight reports the height the content actually needs, then
        // grows it back to that height. The rows attribute is the floor of that growth and a row ceiling,
        // when one is set, is its top: beyond it the content scrolls inside the input instead of pushing
        // the rest of the page down.
        private static setHeight(inputElement: HTMLInputElement, maxRows: number | null) {
            inputElement.style.height = 'auto';

            const styles = getComputedStyle(inputElement);
            const fontSize = parseFloat(styles.fontSize) || 16;
            const lineHeight = parseFloat(styles.lineHeight) || (fontSize * 1.2);
            const paddings = (parseFloat(styles.paddingTop) || 0) + (parseFloat(styles.paddingBottom) || 0);

            // scrollHeight covers the content and its padding but never the borders, while the height of a
            // border-box element does, so the borders are added back to every height derived from it -
            // otherwise the field ends up exactly one border short of its content and scrolls by a hair.
            const isBorderBox = styles.boxSizing === 'border-box';

            const borders = isBorderBox
                ? (parseFloat(styles.borderTopWidth) || 0) + (parseFloat(styles.borderBottomWidth) || 0)
                : 0;

            // The height of a content-box element is the content alone, so the padding scrollHeight carries
            // is taken back out of it - and left out of the floor and the ceiling below as well, so all
            // three speak the same units the style property is read in.
            const boxPaddings = isBorderBox ? paddings : 0;

            const contentHeight = inputElement.scrollHeight + borders - (isBorderBox ? 0 : paddings);

            // The rows attribute keeps its meaning while the input grows: it is the height the field starts
            // at and never falls below, so a field asking for five rows does not collapse to a single line
            // the moment its value is emptied.
            const rows = (inputElement as unknown as HTMLTextAreaElement).rows;
            const minHeight = rows > 1 ? (lineHeight * rows) + boxPaddings + borders : 0;

            const height = Math.max(contentHeight, minHeight);

            if (!maxRows || maxRows <= 0) {
                inputElement.style.height = height + 'px';
                inputElement.style.overflowY = '';
                return;
            }

            const maxHeight = (lineHeight * maxRows) + boxPaddings + borders;

            if (height > maxHeight) {
                inputElement.style.height = maxHeight + 'px';
                inputElement.style.overflowY = 'auto';
            } else {
                inputElement.style.height = height + 'px';
                inputElement.style.overflowY = 'hidden';
            }
        }

        private static disconnectResizeObserver(id: string) {
            TextField._resizeObservers[id]?.disconnect();

            delete TextField._resizeObservers[id];
        }

        // Keeps an input method editor (Pinyin, Kana, Hangul, ...) from leaking into the component while
        // it is composing. Blazor listens for the events on the document, so stopping them here - on the
        // element itself, before they bubble - is what hides the composition session from it.
        public static setupComposition(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'composition');

            let isComposing = false;

            const composing = (e: KeyboardEvent | InputEvent) =>
                isComposing || Utils.isComposingKey(e);

            inputElement.addEventListener('compositionstart', () => {
                isComposing = true;
            }, { signal });

            inputElement.addEventListener('compositionend', () => {
                isComposing = false;

                // Whether the last input event of a session arrives before or after compositionend is not
                // the same in every engine, so one is dispatched here to make sure the committed text
                // always reaches the binding. A repeated value is a no-op on the C# side.
                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
            }, { signal });

            // The half-composed text never reaches the bound value; the committed text is handed over in
            // one piece when the session ends.
            inputElement.addEventListener('input', e => {
                if (composing(e as InputEvent)) {
                    e.stopPropagation();
                }
            }, { signal });

            // Enter commits the candidate of the editor and Escape cancels it, so neither of them means
            // what it usually does while a session is running. Most engines report those keydowns as the
            // "Process" key, which no handler is looking for anyway, but the ones that report the real
            // key would otherwise submit a form or close a dialog in the middle of typing a word.
            inputElement.addEventListener('keydown', e => {
                if (!composing(e)) return;

                if (e.key === 'Enter' || e.key === 'Escape') {
                    e.stopPropagation();
                }
            }, { signal });
        }

        // Selecting the value from the focus handler alone is not enough: the click that gave the input
        // its focus ends with a mouseup that puts the caret where it landed and drops the selection
        // again, so that one mouseup is swallowed.
        public static setupSelectOnFocus(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'selectOnFocus');

            let justFocused = false;

            const select = () => {
                try {
                    inputElement.select();
                } catch {
                    // Selection APIs are not available on every input type.
                }
            };

            inputElement.addEventListener('focus', () => {
                justFocused = true;
                select();
            }, { signal });

            // An auto-focused field is given its focus on the very render this setup follows, so the focus
            // event is already gone by the time the listener above exists and the value would never be
            // selected. Whatever already holds the focus is selected right away instead.
            if (document.activeElement === inputElement) {
                justFocused = true;
                select();
            }

            // Only the interaction that brings the focus in has its mouseup swallowed. A press on a field
            // that is already focused - the first click after a Tab, or any click after the first one -
            // must place the caret where it landed, so the flag is dropped before that mouseup arrives.
            // The focus of a click is given during the default action of the mousedown, which is after
            // this listener runs, so an unfocused field is still seen as unfocused here.
            inputElement.addEventListener('mousedown', () => {
                if (document.activeElement === inputElement) {
                    justFocused = false;
                }
            }, { signal });

            inputElement.addEventListener('mouseup', e => {
                if (!justFocused) return;

                justFocused = false;
                e.preventDefault();
            }, { signal });

            inputElement.addEventListener('blur', () => {
                justFocused = false;
            }, { signal });
        }

        // The frame of the field shows a text cursor across its whole width, so a press on the chrome inside it -
        // the prefix, the suffix, the decorative icon, the busy indicator - puts the caret in the input instead
        // of doing nothing wherever the input itself does not reach. It is handled here rather than in .NET, where
        // every press would cost a round trip and a render just to move the focus. The press is also kept from
        // moving the focus in the first place, so the input never loses the selection it already had to an
        // element that cannot hold one. The listener sits on the root and looks the input up on every press,
        // since the Multiline mode swaps the element between an input and a textarea.
        public static setupChromeFocus(id: string, rootElement: HTMLElement) {
            if (!rootElement) return;

            const signal = TextField.getSignal(id, 'chromeFocus');

            rootElement.addEventListener('mousedown', e => {
                const target = e.target as Element | null;

                // The buttons inside the frame (clear, reveal, an icon with an action) keep their own press.
                if (!target || target.closest('button')) return;

                if (!target.closest('.bit-tfl-pre, .bit-tfl-suf, .bit-tfl-lod, .bit-tfl-ico')) return;

                const input = rootElement.querySelector<HTMLInputElement | HTMLTextAreaElement>('.bit-tfl-inp');

                if (!input || input.disabled) return;

                e.preventDefault();
                input.focus();
            }, { signal });
        }

        public static setupGhostText(id: string, inputElement: HTMLInputElement, dotnetObj: DotNetObject) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'ghost');
            TextField._inputElements[id] = inputElement;

            const getOverlay = () => inputElement.parentElement?.querySelector<HTMLElement>('.bit-tfl-gho') ?? null;
            const hasGhost = () => (TextField._ghostTexts[id] ?? '').length > 0;
            const getSelection = () => {
                try {
                    const start = inputElement.selectionStart;
                    const end = inputElement.selectionEnd;

                    if (typeof start === 'number' && typeof end === 'number') {
                        return { start, end, supportsSelection: true };
                    }
                } catch {
                    // Some input types (e.g. number) may throw when reading selection APIs.
                }

                const fallback = inputElement.value.length;
                return { start: fallback, end: fallback, supportsSelection: false };
            };

            const syncScroll = () => {
                const overlay = getOverlay();
                if (!overlay) return;
                overlay.scrollTop = inputElement.scrollTop;
                overlay.scrollLeft = inputElement.scrollLeft;
            };

            const clearGhost = () => {
                TextField._ghostTexts[id] = '';
                const overlay = getOverlay();
                if (overlay) overlay.textContent = inputElement.value;
                syncScroll();
            };

            // Accept the stored ghost text at the current caret position.
            const acceptGhost = () => {
                if (inputElement.readOnly || inputElement.disabled) return;

                const ghost = TextField._ghostTexts[id] ?? '';
                if (!ghost) return;

                const { start, end, supportsSelection } = getSelection();

                const before = inputElement.value.substring(0, start);
                const after = inputElement.value.substring(end);

                // A suggestion is written into the element directly, which is not something the maxlength
                // attribute holds back the way it holds back typing, so the limit of the field is honored
                // here instead of being broken by the one thing that does not go through the keyboard.
                const limit = inputElement.maxLength;
                const room = limit >= 0 ? Math.max(0, limit - before.length - after.length) : ghost.length;
                const accepted = room < ghost.length ? ghost.substring(0, room) : ghost;

                // A field that is already full has no room for the suggestion at all, so nothing is written
                // and nothing is reported - only the suggestion goes away, since it can never be taken.
                if (!accepted) {
                    clearGhost();
                    return;
                }

                inputElement.value = before + accepted + after;

                const newPos = start + accepted.length;
                if (supportsSelection) {
                    try {
                        inputElement.setSelectionRange(newPos, newPos);
                    } catch {
                        // Ignore unsupported selection range operations.
                    }
                }

                // Clear ghost immediately after acceptance.
                clearGhost();

                // Both events are dispatched: the input one feeds an Immediate binding, and the change one
                // is what a plain (non-Immediate) binding listens to, so the accepted text reaches the
                // bound value either way instead of waiting for the input to lose focus.
                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
                inputElement.dispatchEvent(new Event('change', { bubbles: true }));

                // What the callback receives is what actually made it into the value rather than what was
                // suggested, so an app appending the accepted text to a log of its own never records more
                // than the field ended up holding.
                dotnetObj.invokeMethodAsync('OnGhostTextAccepted', accepted);
            };

            // On every keystroke: immediately clear the ghost suggestion, and forget the one Escape rejected, which
            // was a rejection of the suggestion for the text as it stood.
            // The overlay is JS-owned; Blazor never touches its content.
            inputElement.addEventListener('input', () => {
                delete TextField._rejectedGhostTexts[id];
                clearGhost();
            }, { signal });

            // Tab/Enter: accept the ghost suggestion.
            inputElement.addEventListener('keydown', e => {
                // Tab and Enter belong to the input method editor while a composition session is running:
                // they move through and commit its candidates, so the suggestion is left alone.
                if (Utils.isComposingKey(e)) return;

                const isAcceptKey = e.key === 'Tab' || e.key === 'Enter';

                if (isAcceptKey && hasGhost()) {
                    if (inputElement.readOnly || inputElement.disabled) return;

                    const { start, end } = getSelection();
                    const atEnd = start === inputElement.value.length && end === start;
                    if (!atEnd) return;

                    e.preventDefault();
                    e.stopPropagation();
                    acceptGhost();
                    return;
                }

                if (!hasGhost()) return;

                // Escape rejects the suggestion and nothing else: the key is kept from the handlers of the
                // component, so it neither clears what was typed (ClearOnEscape) nor closes a dialog around
                // the field, the way an open list closes before anything around it hears the key.
                // The suggestion stays rejected through the renders that follow (the keyup of the very press is one),
                // which push it again for a field typed into (see setGhostText).
                if (e.key === 'Escape') {
                    e.stopPropagation();
                    TextField._rejectedGhostTexts[id] = TextField._ghostTexts[id];
                    clearGhost();
                    return;
                }

                // A modifier on its own (Shift before a capital letter, Ctrl before a shortcut) does not
                // change the value, so the suggestion survives it instead of blinking out of existence.
                if (e.key === 'Shift' || e.key === 'Control' || e.key === 'Alt' || e.key === 'Meta') return;

                // Clear immediately on any other key press so stale ghost text never
                // lingers until the later input event.
                clearGhost();
            }, { signal });

            // Click/touch accept: when there is a ghost suggestion and the caret is at
            // the end of the current value, treat click/touch as accepting the suggestion.
            const acceptGhostOnPointer = () => {
                if (inputElement.readOnly || inputElement.disabled) return;

                if (!hasGhost()) return;

                const { start, end } = getSelection();
                const atEnd = start === inputElement.value.length && end === start;

                if (!atEnd) return;

                acceptGhost();
            };

            inputElement.addEventListener('click', acceptGhostOnPointer, { signal });
            inputElement.addEventListener('touchend', acceptGhostOnPointer, { signal });

            // Sync overlay scroll to input scroll (covers cursor navigation without input events).
            inputElement.addEventListener('scroll', syncScroll, { signal });
        }

        // Called by C# (OnAfterRenderAsync) whenever the GhostText parameter changes.
        // Stores the new ghost text and refreshes the overlay to show value + ghost.
        // A suggestion Escape rejected is not shown again by the renders that keep pushing the same one; a different
        // suggestion, or the same one after the field is typed into, is.
        public static setGhostText(id: string, ghostText: string) {
            const rejected = TextField._rejectedGhostTexts[id];
            if (rejected !== undefined && rejected === (ghostText ?? '')) {
                ghostText = '';
            } else {
                delete TextField._rejectedGhostTexts[id];
            }

            TextField._ghostTexts[id] = ghostText ?? '';

            const inputElement = TextField._inputElements[id];
            if (!inputElement) return;

            const overlay = inputElement.parentElement?.querySelector<HTMLElement>('.bit-tfl-gho');
            if (!overlay) return;

            overlay.textContent = inputElement.value + (ghostText ?? '');
            overlay.scrollTop = inputElement.scrollTop;
            overlay.scrollLeft = inputElement.scrollLeft;
        }

        // Moves the caret, or selects a range of the text, of the input element. A null start selects
        // from the beginning and a null end selects to the very end of the value.
        public static setSelectionRange(inputElement: HTMLInputElement, start: number | null, end: number | null) {
            if (!inputElement) return;

            try {
                const length = inputElement.value.length;
                const from = Math.max(0, Math.min(start ?? 0, length));
                const to = Math.max(from, Math.min(end ?? length, length));

                // A selection of an element that does not have the focus is not painted and the caret is
                // not shown, so setting one without focusing first does nothing the user can see.
                inputElement.focus();
                inputElement.setSelectionRange(from, to);
            } catch (e) {
                // Selection APIs are not available on every input type (a number or an email input
                // throws), which is not an error worth breaking the caller over.
            }
        }

        // WebKit fires no change event when the input loses the focus if the last thing to write its value was a script -
        // and with fast typing the layout setMaskedValue writes lands after the last key - so a field committing on change
        // would never commit what was typed. So the change is raised on the way out, but only where the browser itself
        // owed one and skipped it: the last write was the layout (setMaskedValue, while focused), and the text differs
        // from the one the browser compares with - the text at focus, or at the last change raised since - exactly the
        // rule a browser raises its own change by. That leaves every engine that does raise it alone: it raises it ahead
        // of blur, which moves that text along, and a text the mask put back to what it was raises nothing anywhere. A
        // native change still arriving after this one, for the same text, is kept from the page, so one edit commits once.
        // Set up by .NET for every masked field before it is typed into, so the text at its first focus is known.
        public static setupMaskedChange(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'maskedChange');
            const el = inputElement as MaskedInput;

            let settled = el.value;
            let raised: string | null = null;
            el._bitMaskWrote = false;

            el.addEventListener('focus', () => {
                settled = el.value;
                raised = null;
                el._bitMaskWrote = false;
            }, { signal });

            el.addEventListener('input', e => {
                if (e.isTrusted) el._bitMaskWrote = false;
            }, { signal });

            el.addEventListener('change', e => {
                if (e.isTrusted && raised !== null && raised === el.value) {
                    // The browser's own change for the text the blur below already committed; Blazor listens on the
                    // document, so stopping it on the element is what keeps the commit from running twice.
                    raised = null;
                    e.stopImmediatePropagation();
                    return;
                }

                settled = el.value;
                el._bitMaskWrote = false;
            }, { signal });

            el.addEventListener('blur', () => {
                if (!el._bitMaskWrote) return;
                el._bitMaskWrote = false;

                if (el.value === settled) return;

                raised = el.value;
                el.dispatchEvent(new Event('change', { bubbles: true }));
            }, { signal });
        }

        // Writes the text of a masked field, laid out over its mask, back into the input without throwing the caret to the
        // end: assigning `value` alone parks it after the last character, which would make the middle of the text
        // impossible to edit. The caret is read as "after the nth character of the value" - counted the way BitMask.Strip
        // reads the value out of the typed text, so a literal typed in the place the mask puts it is not counted - and put
        // back after the nth slot of the new text. Only written into an input that still holds the text the input event
        // carried (expected): one typed into since holds a newer text, whose own input event lays it out.
        public static setMaskedValue(inputElement: HTMLInputElement, value: string, expected: string, mask: string) {
            if (!inputElement) return;

            const typed = inputElement.value ?? '';
            if (typed !== expected) return;

            const focused = document.activeElement === inputElement;
            const start = focused ? inputElement.selectionStart : null;

            if (typed !== value) {
                inputElement.value = value;
                // Only a layout written while the user is in the field is one its blur may owe a change for (see
                // setupMaskedChange); one landing after the focus has gone is no edit of the next visit.
                if (focused) (inputElement as MaskedInput)._bitMaskWrote = true;
            }

            if (!focused || start === null) return;

            const units = TextField.parseMask(mask);
            const before = TextField.countValueChars(typed, units, start);

            // After the nth slot of the new text; at its end when it holds fewer, which is where the next one goes.
            let caret = value.length;
            if (before === 0) {
                caret = 0;
            } else {
                let seen = 0;
                for (let i = 0, u = 0; i < value.length && u < units.length; i++, u++) {
                    if (units[u].slot === null) continue;
                    seen++;
                    if (seen === before) {
                        caret = i + 1;
                        break;
                    }
                }
            }

            try {
                inputElement.setSelectionRange(caret, caret);
            } catch (e) { /* an input type that has no text selection to set */ }
        }

        // The tokens of a mask, as BitMask.TextTokens has them: '#' a digit, 'A' a letter, '*' a letter or a digit.
        private static accepts(token: string, c: string) {
            switch (token) {
                case '#': return c >= '0' && c <= '9';
                case 'A': return /\p{L}/u.test(c);
                case '*': return /[\p{L}\p{Nd}]/u.test(c);
                default: return false;
            }
        }

        private static acceptedByAny(c: string) {
            return TextField.accepts('#', c) || TextField.accepts('A', c) || TextField.accepts('*', c);
        }

        // The mask as the run of characters it writes: a slot (its token) or a literal, a backslash making the next one a
        // literal.
        private static parseMask(mask: string) {
            const units: { char: string, slot: string | null }[] = [];
            for (let i = 0; i < mask.length; i++) {
                const c = mask[i];
                if (c === '\\' && i + 1 < mask.length) {
                    units.push({ char: mask[++i], slot: null });
                } else {
                    units.push({ char: c, slot: c === '#' || c === 'A' || c === '*' ? c : null });
                }
            }
            return units;
        }

        // How many characters of the value the text holds before the end index, read the way BitMask.Strip reads them.
        private static countValueChars(text: string, units: { char: string, slot: string | null }[], end: number) {
            let count = 0;
            let maskIndex = 0;
            let textIndex = 0;
            const last = Math.min(end, text.length);

            while (textIndex < last) {
                const c = text[textIndex];

                if (maskIndex >= units.length) {
                    if (TextField.acceptedByAny(c)) count++;
                    textIndex++;
                } else if (units[maskIndex].slot !== null) {
                    if (TextField.accepts(units[maskIndex].slot!, c)) {
                        count++;
                        maskIndex++;
                    }
                    textIndex++;
                } else if (c === units[maskIndex].char) {
                    textIndex++;
                    maskIndex++;
                } else if (TextField.acceptedByAny(c)) {
                    maskIndex++;
                } else {
                    textIndex++;
                }
            }

            return count;
        }

        // Tears a single feature down, which is what happens when a text field stops being multiline or
        // stops being immediate while the rest of it stays alive.
        public static disposeFeature(id: string, feature: string) {
            // The auto growing of the multiline mode watches the element as well as listening to it, and
            // an observer is not something an AbortSignal takes away.
            if (feature === 'multiline') {
                TextField.disconnectResizeObserver(id);
            }

            const features = TextField._abortControllers[id];
            if (!features) return;

            features[feature]?.abort();
            delete features[feature];
        }

        public static dispose(id: string) {
            const features = TextField._abortControllers[id];

            if (features) {
                Object.keys(features).forEach(feature => features[feature].abort());
            }

            TextField.disconnectResizeObserver(id);

            delete TextField._abortControllers[id];
            delete TextField._ghostTexts[id];
            delete TextField._rejectedGhostTexts[id];
            delete TextField._maxRows[id];
            delete TextField._inputElements[id];
        }
    }
}

// bit-lazy: teardown TextField.dispose TextField.disposeFeature
