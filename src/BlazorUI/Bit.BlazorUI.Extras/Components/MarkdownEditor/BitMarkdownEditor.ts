namespace BitBlazorUI {

    type MarkdownEditorConfig = {
        imageUpload: boolean;
        syncScroll: boolean;
        autoPair: boolean;
        autoSaveKey?: string | null;
        changeDebounceMs: number;
        maxLength: number;
        autoFocus: boolean;
        reportSelection: boolean;
        tabIndents: boolean;
        maxImageSize: number;
        imageAccept?: string | null;
        uploadingText: string;
    };

    type MdeFindResult = {
        count: number;
        index: number;
    };

    type MdeSelection = {
        start: number;
        end: number;
        text: string;
    };

    export class MarkdownEditor {
        private static _editors: { [key: string]: MarkdownEditorCore } = {};

        public static init(id: string, textArea: HTMLTextAreaElement, root: HTMLElement, dotnetObj: DotNetObject, defaultValue?: string | null, config?: MarkdownEditorConfig) {
            MarkdownEditor.dispose(id);

            const editor = new MarkdownEditorCore(textArea, root, dotnetObj, config);

            // Restore an autosaved draft only when no explicit value was provided.
            let initial = defaultValue;
            if (!initial && config?.autoSaveKey) {
                const draft = MarkdownEditor.readDraft(config.autoSaveKey);
                if (draft) initial = draft;
            }

            if (initial) {
                textArea.value = initial;
                editor.resetBaseline();
                editor.notifyChangeNow();
            }

            MarkdownEditor._editors[id] = editor;

            if (config?.autoFocus) editor.focus();
        }

        public static setConfig(id: string, config: MarkdownEditorConfig) {
            MarkdownEditor._editors[id]?.setConfig(config);
        }

        public static getValue(id: string) {
            const editor = MarkdownEditor._editors[id];
            if (!editor) return '';

            return editor.getValue();
        }

        public static setValue(id: string, value?: string | null) {
            MarkdownEditor._editors[id]?.setValue(value ?? '');
        }

        public static run(id: string, command: string) {
            return MarkdownEditor._editors[id]?.runCommand(command);
        }

        public static insert(id: string, text: string) {
            MarkdownEditor._editors[id]?.insertText(text);
        }

        public static replaceAll(id: string, search: string, replacement: string, all: boolean, matchCase: boolean) {
            return MarkdownEditor._editors[id]?.replaceAll(search, replacement, all, matchCase) ?? 0;
        }

        public static replaceOne(id: string, search: string, replacement: string, matchCase: boolean): MdeFindResult {
            return MarkdownEditor._editors[id]?.replaceOne(search, replacement, matchCase) ?? { count: 0, index: 0 };
        }

        public static find(id: string, search: string, matchCase: boolean, backwards: boolean): MdeFindResult {
            return MarkdownEditor._editors[id]?.find(search, matchCase, backwards) ?? { count: 0, index: 0 };
        }

        public static getSelection(id: string): MdeSelection {
            return MarkdownEditor._editors[id]?.getSelection() ?? { start: 0, end: 0, text: '' };
        }

        public static setSelection(id: string, start: number, end: number) {
            MarkdownEditor._editors[id]?.setSelection(start, end);
        }

        public static undo(id: string) {
            MarkdownEditor._editors[id]?.undo();
        }

        public static redo(id: string) {
            MarkdownEditor._editors[id]?.redo();
        }

        public static focus(id: string) {
            MarkdownEditor._editors[id]?.focus();
        }

        public static blur(id: string) {
            MarkdownEditor._editors[id]?.blur();
        }

        public static clearDraft(id: string) {
            MarkdownEditor._editors[id]?.clearDraft();
        }

        public static dispose(id: string) {
            if (!MarkdownEditor._editors[id]) return;

            MarkdownEditor._editors[id].dispose();

            delete MarkdownEditor._editors[id];
        }

        private static readDraft(key: string): string | null {
            try {
                return window.localStorage.getItem(key);
            } catch {
                return null;
            }
        }
    }

    type MdeSnapshot = {
        text: string;
        selStart: number;
        selEnd: number;
    };

    type MdeEditResult = {
        handled: boolean;
        text: string;
        selectionStart: number;
        selectionEnd: number;
    };

    // The textarea is uncontrolled (this script owns its value to preserve the caret).
    // Toolbar commands and external updates assign `textarea.value` directly, which
    // wipes the browser's native undo stack and would otherwise make Ctrl+Z behave
    // erratically. Owning the history here keeps undo/redo consistent across typing,
    // toolbar commands and keyboard shortcuts. All markdown transformations happen in C#.
    class MarkdownEditorCore {
        private static readonly LIST_LINE = /^(\s*)([-*+] (\[[ xX]\] )?|\d+[.)] )/;
        private static readonly QUOTE_LINE = /^\s*> /;
        // Characters that end an undo "word", so history breaks at word boundaries
        // instead of collapsing a whole paragraph into a single step.
        private static readonly WORD_BOUNDARY = /[\s.,;:!?()\[\]{}'"]/;
        // Selection-wrapping pairs typed to surround the current selection.
        private static readonly PAIRS: { [key: string]: string } = {
            '*': '*', '_': '_', '`': '`', '~': '~', '(': ')', '[': ']', '{': '}', '"': '"', '<': '>'
        };
        // Ctrl/Cmd+Alt+<digit> heading shortcuts, keyed by physical code so they survive
        // keyboard layouts where the combination does not produce the digit itself.
        private static readonly HEADING_CODES: { [key: string]: string } = {
            Digit1: 'Heading1', Digit2: 'Heading2', Digit3: 'Heading3',
            Digit4: 'Heading4', Digit5: 'Heading5', Digit6: 'Heading6'
        };

        // Maximum number of states kept per direction.
        private static readonly HISTORY_LIMIT = 200;
        // A pause longer than this starts a fresh undo step even mid-word.
        private static readonly TYPING_PAUSE_MS = 600;
        private static readonly SELECTION_DEBOUNCE_MS = 120;

        private _undo: MdeSnapshot[] = [];
        private _redo: MdeSnapshot[] = [];
        private _baseline: MdeSnapshot;
        private _typingActive = false;
        private _typingTimer: ReturnType<typeof setTimeout> | null = null;
        private _changeTimer: ReturnType<typeof setTimeout> | null = null;
        private _selectionTimer: ReturnType<typeof setTimeout> | null = null;
        private _canUndo = false;
        private _canRedo = false;
        private _commandInFlight = false;
        private _lastSelection: { start: number, end: number };
        private _syncingScroll = false;
        private _uploadSeq = 0;
        // Set by Escape so the next Tab moves the focus out instead of indenting, which
        // is the escape hatch that keeps the editor from becoming a keyboard trap.
        private _tabEscape = false;
        private _openDropdown: HTMLElement | null = null;
        private _scrollSyncBound = false;
        private _toolbarObserver: MutationObserver | null = null;

        private textArea: HTMLTextAreaElement;
        private root: HTMLElement | undefined | null;
        private dotnetObj: DotNetObject | undefined | null;
        private config: MarkdownEditorConfig;
        private editorPane: HTMLElement | null = null;
        private previewPane: HTMLElement | null = null;
        private toolbar: HTMLElement | null = null;

        constructor(textArea: HTMLTextAreaElement, root: HTMLElement | undefined | null, dotnetObj: DotNetObject, config?: MarkdownEditorConfig) {
            this.textArea = textArea;
            this.root = root;
            this.dotnetObj = dotnetObj;
            this.config = config ?? {
                imageUpload: false, syncScroll: true, autoPair: true, autoSaveKey: null,
                changeDebounceMs: 0, maxLength: 0, autoFocus: false, reportSelection: true, tabIndents: true,
                maxImageSize: 0, imageAccept: null, uploadingText: 'uploading'
            };

            this._baseline = this.snapshot();
            this._lastSelection = { start: textArea.selectionStart || 0, end: textArea.selectionEnd || 0 };

            textArea.addEventListener('keydown', this.keyDownHandler);
            textArea.addEventListener('input', this.inputHandler);
            textArea.addEventListener('blur', this.blurHandler);
            textArea.addEventListener('paste', this.pasteHandler);
            textArea.addEventListener('mouseup', this.saveSelectionHandler);
            textArea.addEventListener('keyup', this.saveSelectionHandler);
            // Capture the selection whenever it changes while the textarea is focused,
            // so commands always know the intended range.
            document.addEventListener('selectionchange', this.selectionChangeHandler);
            // Stop toolbar buttons from stealing focus from the textarea. Only mousedown
            // is cancelled: cancelling touchstart would also suppress the click the browser
            // synthesizes from it, leaving the toolbar dead on touch devices.
            root?.addEventListener('mousedown', this.toolbarPointerDownHandler);
            // Dropping an image anywhere on the editor (the preview pane included) uploads it,
            // instead of only over the few pixels the textarea happens to occupy.
            root?.addEventListener('drop', this.dropHandler);
            root?.addEventListener('dragover', this.dragOverHandler);
            root?.addEventListener('dragleave', this.dragLeaveHandler);
            root?.addEventListener('dragend', this.dragLeaveHandler);

            this.applyScrollSync();

            this.toolbar = root?.querySelector('.bit-mde-tlb') ?? null;
            if (this.toolbar) {
                this.toolbar.addEventListener('keydown', this.toolbarKeydownHandler);
                this.toolbar.addEventListener('focusin', this.toolbarFocusInHandler);
                this.toolbar.addEventListener('click', this.toolbarClickHandler);
                this.toolbar.addEventListener('pointerover', this.toolbarPointerOverHandler);
                this.toolbar.addEventListener('pointerleave', this.toolbarPointerLeaveHandler);
                document.addEventListener('pointerdown', this.documentPointerDownHandler, true);
                this.refreshToolbarRoving();
                this.observeToolbar();
            }
        }

        // Blazor re-renders the toolbar whenever a button's disabled state flips (undo and
        // redo do it constantly). Watching for that keeps the single tab stop on a button
        // that can actually take focus, and re-applies it to freshly created buttons.
        private observeToolbar() {
            if (!this.toolbar || typeof MutationObserver === 'undefined') return;

            this._toolbarObserver = new MutationObserver(() => this.refreshToolbarRoving());
            this._toolbarObserver.observe(this.toolbar, { attributes: true, attributeFilter: ['disabled'], subtree: true, childList: true });
        }

        // Implements the WAI-ARIA toolbar pattern: a single tab stop, arrow keys move focus
        // between buttons. Managed here so Blazor's diffing is untouched. The tab stop must
        // land on an enabled button - the first item of the default toolbar is Undo, which
        // starts out disabled and would otherwise make the whole toolbar unreachable.
        private refreshToolbarRoving() {
            const buttons = this.toolbarButtons();
            if (!buttons.length) return;

            const enabled = buttons.filter(b => !b.disabled);
            const active = enabled.find(b => b.getAttribute('tabindex') === '0') ?? enabled[0];
            for (const b of buttons) b.setAttribute('tabindex', b === active ? '0' : '-1');
        }

        private toolbarButtons(): HTMLButtonElement[] {
            if (!this.toolbar) return [];
            // Top-level buttons and dropdown triggers only, not the buttons inside menus.
            return Array.from(this.toolbar.querySelectorAll<HTMLButtonElement>(':scope > .bit-mde-btn, :scope > .bit-mde-dd > .bit-mde-btn'));
        }

        private menuItems(dd: HTMLElement): HTMLButtonElement[] {
            return Array.from(dd.querySelectorAll<HTMLButtonElement>('.bit-mde-mi')).filter(b => !b.disabled);
        }

        // The menus are opened from script rather than from a :hover / :focus-within rule,
        // so pointer, click and keyboard all go through one place and aria-expanded always
        // describes what is actually on screen (a :hover rule also sticks on touch).
        private setDropdownOpen(dd: HTMLElement, open: boolean) {
            if (open && this._openDropdown && this._openDropdown !== dd) {
                this.setDropdownOpen(this._openDropdown, false);
            }

            dd.classList.toggle('bit-mde-ddo', open);
            dd.querySelector<HTMLButtonElement>(':scope > .bit-mde-btn')?.setAttribute('aria-expanded', open ? 'true' : 'false');

            if (open) {
                this._openDropdown = dd;
            } else if (this._openDropdown === dd) {
                this._openDropdown = null;
            }
        }

        private closeDropdown(focusTrigger: boolean) {
            const dd = this._openDropdown;
            if (!dd) return;

            const trigger = dd.querySelector<HTMLButtonElement>(':scope > .bit-mde-btn');
            this.setDropdownOpen(dd, false);
            if (focusTrigger) trigger?.focus();
        }

        private toolbarFocusInHandler = (e: FocusEvent) => {
            const btn = (e.target as HTMLElement)?.closest('.bit-mde-btn') as HTMLButtonElement | null;
            if (!btn || btn.classList.contains('bit-mde-mi')) return;

            for (const b of this.toolbarButtons()) b.setAttribute('tabindex', b === btn ? '0' : '-1');

            // Focus reached a button outside the open menu: the menu has been left behind.
            if (this._openDropdown && this._openDropdown.contains(btn) === false) {
                this.setDropdownOpen(this._openDropdown, false);
            }
        };

        private toolbarClickHandler = (e: MouseEvent) => {
            const target = e.target as HTMLElement;
            if (!target || !target.closest) return;

            const item = target.closest('.bit-mde-mi');
            if (item) {
                // The item ran its command; the menu has served its purpose. An item reached by
                // keyboard still holds the focus, which the menu is about to take off the screen,
                // so the focus goes back to the trigger it came from.
                this.closeDropdown(document.activeElement === item);
                return;
            }

            const trigger = target.closest('.bit-mde-btn') as HTMLButtonElement | null;
            const dd = this.dropdownOf(trigger);
            if (dd && trigger!.disabled === false) {
                this.setDropdownOpen(dd, dd.classList.contains('bit-mde-ddo') === false);
            } else if (this._openDropdown) {
                this.setDropdownOpen(this._openDropdown, false);
            }
        };

        private dropdownOf(trigger: HTMLElement | null): HTMLElement | null {
            const parent = trigger?.parentElement;
            return parent?.classList.contains('bit-mde-dd') ? parent : null;
        }

        private toolbarPointerOverHandler = (e: PointerEvent) => {
            if (e.pointerType !== 'mouse') return;

            const target = e.target as HTMLElement;
            const dd = target && target.closest ? target.closest('.bit-mde-dd') as HTMLElement | null : null;
            const trigger = dd?.querySelector<HTMLButtonElement>(':scope > .bit-mde-btn');
            if (dd && trigger?.disabled !== true) {
                this.setDropdownOpen(dd, true);
            } else if (this._openDropdown && this._openDropdown.contains(document.activeElement) === false) {
                this.setDropdownOpen(this._openDropdown, false);
            }
        };

        private toolbarPointerLeaveHandler = (e: PointerEvent) => {
            if (e.pointerType !== 'mouse' || !this._openDropdown) return;
            if (this._openDropdown.contains(document.activeElement)) return;

            this.setDropdownOpen(this._openDropdown, false);
        };

        private documentPointerDownHandler = (e: Event) => {
            if (!this._openDropdown) return;
            if (this._openDropdown.contains(e.target as Node)) return;

            this.setDropdownOpen(this._openDropdown, false);
        };

        private toolbarKeydownHandler = (e: KeyboardEvent) => {
            const current = document.activeElement as HTMLElement;

            if (e.key === 'Escape') {
                if (!this._openDropdown) return;
                e.preventDefault();
                this.closeDropdown(true);
                return;
            }

            if (this._openDropdown && current && current.classList.contains('bit-mde-mi')) {
                const items = this.menuItems(this._openDropdown);
                if (!items.length) return;

                if (e.key === 'Tab') { this.closeDropdown(false); return; }
                if (['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(e.key) === false) return;

                e.preventDefault();
                const idx = items.indexOf(current as HTMLButtonElement);
                let next = idx < 0 ? 0 : idx;
                if (e.key === 'ArrowDown') next = (next + 1) % items.length;
                else if (e.key === 'ArrowUp') next = (next - 1 + items.length) % items.length;
                else if (e.key === 'Home') next = 0;
                else next = items.length - 1;
                items[next].focus();
                return;
            }

            const dd = this.dropdownOf(current && current.closest ? current.closest('.bit-mde-btn') as HTMLElement | null : null);
            if (dd && (e.key === 'ArrowDown' || e.key === 'Enter' || e.key === ' ')) {
                e.preventDefault();
                this.setDropdownOpen(dd, true);
                const items = this.menuItems(dd);
                if (items.length) items[0].focus();
                return;
            }

            if (['ArrowRight', 'ArrowLeft', 'Home', 'End'].includes(e.key) === false) return;

            const buttons = this.toolbarButtons().filter(b => !b.disabled);
            if (!buttons.length) return;

            let idx = buttons.findIndex(b => b === current || b.contains(current));
            if (idx < 0) idx = 0;

            e.preventDefault();
            let next = idx;
            if (e.key === 'ArrowRight') next = (idx + 1) % buttons.length;
            else if (e.key === 'ArrowLeft') next = (idx - 1 + buttons.length) % buttons.length;
            else if (e.key === 'Home') next = 0;
            else if (e.key === 'End') next = buttons.length - 1;

            buttons[next].focus();
        };

        // Parameters that reach the script through the init config can change while the
        // component lives, so .NET pushes the whole config again whenever one of them does.
        public setConfig(config: MarkdownEditorConfig) {
            const wasSyncing = this.config.syncScroll;
            this.config = config;

            if (wasSyncing !== config.syncScroll) this.applyScrollSync();

            // A limit lowered below the current length takes effect immediately.
            const limited = this.limit(this.textArea.value);
            if (limited !== this.textArea.value) {
                this.textArea.value = limited;
                this.flushChange();
                this._baseline = this.snapshot();
            }
        }

        public getValue() {
            return this.textArea.value;
        }

        // Pushes an externally-changed value into the (uncontrolled) textarea without
        // notifying .NET back, so we don't loop the change into Blazor again.
        public setValue(value: string) {
            const limited = this.limit(value);
            const truncated = limited !== value;
            value = limited;

            if (this.textArea.value !== value) {
                const sel = { s: this.textArea.selectionStart, e: this.textArea.selectionEnd };
                this.textArea.value = value;
                // Keep the caret close to where it was instead of snapping to the start.
                const max = value.length;
                this.textArea.setSelectionRange(Math.min(sel.s, max), Math.min(sel.e, max));
                this.saveSelection();
            }

            // External assignment becomes the new baseline; in-flight typing groups
            // are closed so the next keystroke starts a fresh undo step.
            this.endTypingGroup();
            this._baseline = this.snapshot();

            // A value longer than MaxLength was cut down here, so .NET has to hear about it
            // or it would go on holding text the editor does not contain.
            if (truncated) this.flushChange();
        }

        public resetBaseline() {
            this._baseline = this.snapshot();
        }

        public focus() {
            this.textArea.focus();
        }

        public blur() {
            this.textArea.blur();
        }

        public getSelection(): MdeSelection {
            const focused = document.activeElement === this.textArea;
            const start = focused ? this.textArea.selectionStart : this._lastSelection.start;
            const end = focused ? this.textArea.selectionEnd : this._lastSelection.end;
            return { start, end, text: this.textArea.value.slice(Math.min(start, end), Math.max(start, end)) };
        }

        public setSelection(start: number, end: number) {
            const max = this.textArea.value.length;
            this.textArea.focus();
            this.textArea.setSelectionRange(Math.min(Math.max(start, 0), max), Math.min(Math.max(end, 0), max));
            this.saveSelection();
            this.scheduleSelectionReport();
        }

        public notifyChangeNow() {
            this.flushChange();
        }

        public clearDraft() {
            if (!this.config.autoSaveKey) return;
            try { window.localStorage.removeItem(this.config.autoSaveKey); } catch { }
        }

        // Reads selection + value, asks C# to transform it, then writes the result back.
        // Retries against the freshest value if typing landed while awaiting .NET, so a
        // toolbar click during rapid typing is never silently dropped.
        public async runCommand(command: string) {
            if (!this.dotnetObj || this.textArea.readOnly || this._commandInFlight) return;

            this._commandInFlight = true;
            try {
                for (let attempt = 0; attempt < 3; attempt++) {
                    // When a toolbar button takes focus, the textarea's live selection can be
                    // lost, so fall back to the last selection captured while it was focused.
                    const focused = document.activeElement === this.textArea;
                    const start = focused ? this.textArea.selectionStart : this._lastSelection.start;
                    const end = focused ? this.textArea.selectionEnd : this._lastSelection.end;
                    const value = this.textArea.value;

                    const result = await this.dotnetObj.invokeMethodAsync<MdeEditResult>('ApplyCommand', command, start, end, value);
                    if (!result || !result.handled) return;

                    // Typing may have changed the value while awaiting .NET; rebase by
                    // recomputing against the newest value rather than clobbering it.
                    if (this.textArea.value !== value) continue;

                    // Record the state before the command so it can be undone as one step.
                    this.endTypingGroup();
                    this.pushUndo({ text: value, selStart: start, selEnd: end });
                    this._redo = [];

                    this.applyResult(result);
                    return;
                }
            } catch {
                // The circuit (or the component) is gone; there is nothing to write back.
            } finally {
                this._commandInFlight = false;
            }
        }

        // Inserts text at the current selection as a single undo step.
        public insertText(text: string) {
            if (this.textArea.readOnly) return;
            const start = this.textArea.selectionStart;
            const end = this.textArea.selectionEnd;
            this.replaceRange(start, end, text, start + text.length, start + text.length);
        }

        // Replaces occurrences of a literal search string; returns the replacement count.
        // With `all` off it replaces the first occurrence at or after the caret (wrapping
        // to the top), so repeated calls walk the document instead of hammering the first
        // match forever when the replacement itself contains the search term.
        public replaceAll(search: string, replacement: string, all: boolean, matchCase: boolean): number {
            if (this.textArea.readOnly || !search) return 0;

            const found = this.matches(search, matchCase);
            if (!found.length) return 0;

            const value = this.textArea.value;

            if (!all) {
                const caret = Math.min(this.textArea.selectionStart, this.textArea.selectionEnd);
                const at = found.find(m => m >= caret) ?? found[0];
                this.replaceRange(at, at + search.length, replacement, at + replacement.length, at + replacement.length);
                return 1;
            }

            let result = '';
            let prev = 0;
            for (const m of found) {
                result += value.slice(prev, m) + replacement;
                prev = m + search.length;
            }
            result += value.slice(prev);

            this.endTypingGroup();
            this.pushUndo(this.snapshot());
            this._redo = [];
            this.textArea.value = this.limit(result);
            const caret = Math.min(this.textArea.selectionStart, this.textArea.value.length);
            this.textArea.setSelectionRange(caret, caret);
            this.saveSelection();
            this.flushChange();
            this._baseline = this.snapshot();
            this.notifyHistory();
            return found.length;
        }

        // Replaces the match the selection is sitting on (if any) and moves to the next
        // one, which is what a find & replace panel's "Replace" button is expected to do.
        public replaceOne(search: string, replacement: string, matchCase: boolean): MdeFindResult {
            if (this.textArea.readOnly || !search) return { count: 0, index: 0 };

            const start = this.textArea.selectionStart;
            const end = this.textArea.selectionEnd;
            const selected = this.textArea.value.slice(start, end);
            const isMatch = selected.length === search.length &&
                (matchCase ? selected === search : selected.toLowerCase() === search.toLowerCase());

            if (isMatch) {
                this.replaceRange(start, end, replacement, start + replacement.length, start + replacement.length);
            }

            return this.find(search, matchCase, false);
        }

        // Selects the next (or previous) occurrence, wrapping around the document ends.
        public find(search: string, matchCase: boolean, backwards: boolean): MdeFindResult {
            if (!search) return { count: 0, index: 0 };

            const found = this.matches(search, matchCase);
            if (!found.length) return { count: 0, index: 0 };

            const selStart = this.textArea.selectionStart;
            const selEnd = this.textArea.selectionEnd;

            let target: number;
            if (backwards) {
                const before = found.filter(m => m < selStart);
                target = before.length ? before[before.length - 1] : found[found.length - 1];
            } else {
                // Step past the current match when one is selected, so "next" advances.
                const from = selEnd > selStart ? selStart + 1 : selStart;
                target = found.find(m => m >= from) ?? found[0];
            }

            this.textArea.focus();
            this.textArea.setSelectionRange(target, target + search.length);
            this.saveSelection();
            this.scheduleSelectionReport();

            return { count: found.length, index: found.indexOf(target) + 1 };
        }

        public undo() {
            if (this.textArea.readOnly || !this._undo.length) return;

            this.endTypingGroup();
            this.pushRedo(this._baseline);

            this.applySnapshot(this._undo.pop()!);
            this.notifyHistory();
        }

        public redo() {
            if (this.textArea.readOnly || !this._redo.length) return;

            this.endTypingGroup();
            this.pushUndo(this._baseline);

            this.applySnapshot(this._redo.pop()!);
            this.notifyHistory();
        }

        public dispose() {
            this.clearTimers();

            this._toolbarObserver?.disconnect();
            this._toolbarObserver = null;

            this.textArea.removeEventListener('keydown', this.keyDownHandler);
            this.textArea.removeEventListener('input', this.inputHandler);
            this.textArea.removeEventListener('blur', this.blurHandler);
            this.textArea.removeEventListener('paste', this.pasteHandler);
            this.textArea.removeEventListener('mouseup', this.saveSelectionHandler);
            this.textArea.removeEventListener('keyup', this.saveSelectionHandler);
            document.removeEventListener('selectionchange', this.selectionChangeHandler);
            document.removeEventListener('pointerdown', this.documentPointerDownHandler, true);
            this.root?.removeEventListener('mousedown', this.toolbarPointerDownHandler);
            this.root?.removeEventListener('drop', this.dropHandler);
            this.root?.removeEventListener('dragover', this.dragOverHandler);
            this.root?.removeEventListener('dragleave', this.dragLeaveHandler);
            this.root?.removeEventListener('dragend', this.dragLeaveHandler);
            this.detachScrollSync();
            this.toolbar?.removeEventListener('keydown', this.toolbarKeydownHandler);
            this.toolbar?.removeEventListener('focusin', this.toolbarFocusInHandler);
            this.toolbar?.removeEventListener('click', this.toolbarClickHandler);
            this.toolbar?.removeEventListener('pointerover', this.toolbarPointerOverHandler);
            this.toolbar?.removeEventListener('pointerleave', this.toolbarPointerLeaveHandler);

            this._undo = [];
            this._redo = [];
            this.dotnetObj = undefined;
            this.root = undefined;
            this.editorPane = null;
            this.previewPane = null;
            this.toolbar = null;
            this._openDropdown = null;
        }

        // ==========================================================

        private keyDownHandler = (e: KeyboardEvent) => {
            if (e.isComposing) return;

            // Escape arms the Tab escape hatch below, then lets .NET close the find panel
            // or leave full-screen.
            if (e.key === 'Escape') {
                this._tabEscape = true;
                this.invoke('OnEscape');
                return;
            }

            if (e.key !== 'Tab') this._tabEscape = false;

            const mod = e.ctrlKey || e.metaKey;
            const key = e.key.toLowerCase();

            // Alt + Up/Down moves the current line(s), the way code editors do.
            if (e.altKey && !mod && !e.shiftKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
                e.preventDefault();
                this.runCommand(e.key === 'ArrowUp' ? 'MoveLineUp' : 'MoveLineDown');
                return;
            }

            if (mod && e.altKey) {
                const heading = MarkdownEditorCore.HEADING_CODES[e.code];
                if (heading) { e.preventDefault(); this.runCommand(heading); return; }
                if (key === 'c') { e.preventDefault(); this.runCommand('CodeBlock'); return; }
                return;
            }

            if (mod) {
                // Undo / redo. Ctrl/Cmd+Z, Ctrl/Cmd+Shift+Z and Ctrl/Cmd+Y.
                if (key === 'z' && !e.shiftKey) { e.preventDefault(); this.undo(); return; }
                if ((key === 'z' && e.shiftKey) || (key === 'y' && !e.shiftKey)) { e.preventDefault(); this.redo(); return; }

                if (e.shiftKey) {
                    if (key === 's') { e.preventDefault(); this.runCommand('Strikethrough'); return; }
                    if (key === 'd') { e.preventDefault(); this.runCommand('DeleteLine'); return; }
                    switch (e.code) {
                        case 'Digit7': e.preventDefault(); this.runCommand('OrderedList'); return;
                        case 'Digit8': e.preventDefault(); this.runCommand('UnorderedList'); return;
                        case 'Digit9': e.preventDefault(); this.runCommand('TaskList'); return;
                        case 'Period': e.preventDefault(); this.runCommand('Quote'); return;
                    }
                    return;
                }

                switch (key) {
                    case 'b': e.preventDefault(); this.runCommand('Bold'); return;
                    case 'i': e.preventDefault(); this.runCommand('Italic'); return;
                    case 'e': e.preventDefault(); this.runCommand('InlineCode'); return;
                    case 'd': e.preventDefault(); this.runCommand('DuplicateLine'); return;
                    case 'k': e.preventDefault(); this.runCommand('Link'); return;
                    case 'f': e.preventDefault(); this.invoke('OnShortcut', 'find'); return;
                }
                return;
            }

            if (e.key === 'F9') { e.preventDefault(); this.invoke('OnShortcut', 'mode'); return; }
            if (e.key === 'F11') { e.preventDefault(); this.invoke('OnShortcut', 'fullscreen'); return; }

            // Wrap the selection when a pairing character is typed over it.
            if (this.config.autoPair && !e.altKey && !this.textArea.readOnly &&
                this.textArea.selectionStart !== this.textArea.selectionEnd &&
                Object.prototype.hasOwnProperty.call(MarkdownEditorCore.PAIRS, e.key)) {
                e.preventDefault();
                this.wrapSelection(e.key, MarkdownEditorCore.PAIRS[e.key]);
                return;
            }

            // Only hijack Tab while the editor is writable and Tab indenting is on. Read-only
            // mode, a disabled TabIndents and an Escape pressed right before it all let the
            // browser move the focus on, so the editor can never trap the keyboard.
            if (e.key === 'Tab') {
                const escaped = this._tabEscape;
                this._tabEscape = false;
                if (this.textArea.readOnly || !this.config.tabIndents || escaped) return;
                e.preventDefault();
                this.runCommand(e.shiftKey ? 'Outdent' : 'Indent');
                return;
            }

            // Only hijack Enter when continuing a list/quote, so normal typing keeps
            // its regular flow.
            if (e.key === 'Enter' && !e.shiftKey &&
                this.textArea.selectionStart === this.textArea.selectionEnd) {
                const line = this.currentLine();
                if (MarkdownEditorCore.LIST_LINE.test(line) || MarkdownEditorCore.QUOTE_LINE.test(line)) {
                    e.preventDefault();
                    this.runCommand('NewLine');
                }
            }
        };

        // Programmatic edits (commands, undo/redo, external sets) assign the value
        // directly and never raise input events, so only free-form typing lands here.
        private inputHandler = () => {
            this.recordTyping();
            this.scheduleChange();
        };

        private blurHandler = () => {
            // Make sure the latest value reaches .NET when focus leaves the editor.
            this._tabEscape = false;
            this.flushChange();
        };

        private selectionChangeHandler = () => {
            if (document.activeElement === this.textArea) {
                this.saveSelection();
                this.scheduleSelectionReport();
            }
        };

        private saveSelectionHandler = () => {
            this.saveSelection();
        };

        private toolbarPointerDownHandler = (e: Event) => {
            const target = e.target as HTMLElement;
            if (target?.closest && target.closest('.bit-mde-btn')) {
                e.preventDefault();
            }
        };

        private pasteHandler = (e: ClipboardEvent) => {
            if (this.textArea.readOnly || !e.clipboardData) return;

            // 1) Images from the clipboard -> upload (when a handler is configured).
            if (this.config.imageUpload) {
                const files = this.imageFiles(e.clipboardData.files, e.clipboardData.items);
                if (files.length) {
                    e.preventDefault();
                    this.uploadFiles(files);
                    return;
                }
            }

            const text = e.clipboardData.getData('text/plain');
            const html = e.clipboardData.getData('text/html');

            // 2) An HTML table -> convert to a markdown table.
            if (html && /<table[\s>]/i.test(html)) {
                const md = this.htmlTableToMarkdown(html);
                if (md) {
                    e.preventDefault();
                    this.insertText(md);
                    return;
                }
            }

            // 3) Tab separated rows (spreadsheets that offer nothing but plain text).
            if (text) {
                const md = this.tsvToMarkdown(text);
                if (md) {
                    e.preventDefault();
                    this.insertText(md);
                    return;
                }
            }

            // 4) A URL pasted over a selection -> turn the selection into a link.
            if (text && this.isUrl(text) && this.textArea.selectionStart !== this.textArea.selectionEnd) {
                e.preventDefault();
                const label = this.textArea.value.slice(this.textArea.selectionStart, this.textArea.selectionEnd);
                this.insertText(`[${label}](${text.trim()})`);
                return;
            }

            // Otherwise fall through to the browser's default paste (it fires an input
            // event, so the change is recorded in history normally).
        };

        private dragOverHandler = (e: DragEvent) => {
            if (this.config.imageUpload && e.dataTransfer && Array.from(e.dataTransfer.items || []).some(i => i.kind === 'file')) {
                e.preventDefault();
                this.root?.setAttribute('data-bit-mde-drag', '');
            }
        };

        private dragLeaveHandler = () => {
            this.root?.removeAttribute('data-bit-mde-drag');
        };

        private dropHandler = (e: DragEvent) => {
            this.root?.removeAttribute('data-bit-mde-drag');

            if (this.textArea.readOnly || !this.config.imageUpload || !e.dataTransfer) return;
            const files = this.imageFiles(e.dataTransfer.files, e.dataTransfer.items);
            if (!files.length) return;

            e.preventDefault();
            this.textArea.focus();
            this.uploadFiles(files);
        };

        private editorScrollHandler = () => this.syncScroll(this.editorPane, this.previewPane);
        private previewScrollHandler = () => this.syncScroll(this.previewPane, this.editorPane);

        // ==========================================================

        private applyScrollSync() {
            this.detachScrollSync();

            if (!this.config.syncScroll || !this.root) return;

            // The editor pane's textarea fills the pane (height:100%) and scrolls
            // internally, so the pane wrapper (`.bit-mde-epn`) itself never overflows.
            // Scroll events don't bubble, so listening on the wrapper never fires and
            // writing its scrollTop moves nothing. The textarea is the real scroller on
            // the editor side; the preview pane is the scroller on the preview side.
            this.editorPane = this.textArea;
            this.previewPane = this.root.querySelector('.bit-mde-ppn');
            this.editorPane.addEventListener('scroll', this.editorScrollHandler);
            this.previewPane?.addEventListener('scroll', this.previewScrollHandler);
            this._scrollSyncBound = true;
        }

        private detachScrollSync() {
            if (!this._scrollSyncBound) return;

            this.editorPane?.removeEventListener('scroll', this.editorScrollHandler);
            this.previewPane?.removeEventListener('scroll', this.previewScrollHandler);
            this._scrollSyncBound = false;
        }

        private wrapSelection(open: string, close: string) {
            const start = this.textArea.selectionStart;
            const end = this.textArea.selectionEnd;
            const selected = this.textArea.value.slice(start, end);
            this.replaceRange(start, end, open + selected + close, start + open.length, end + open.length);
        }

        private replaceRange(start: number, end: number, replacement: string, selStart: number, selEnd: number) {
            const value = this.textArea.value;
            this.endTypingGroup();
            this.pushUndo({ text: value, selStart: start, selEnd: end });
            this._redo = [];
            this.textArea.value = this.limit(value.slice(0, start) + replacement + value.slice(end));
            this.textArea.focus();
            const max = this.textArea.value.length;
            this.textArea.setSelectionRange(Math.min(selStart, max), Math.min(selEnd, max));
            this.saveSelection();
            this.flushChange();
            this._baseline = this.snapshot();
            this.notifyHistory();
        }

        // Truncates to the configured MaxLength. The maxlength attribute already covers
        // typing and pasting; this covers everything written programmatically.
        private limit(text: string): string {
            const max = this.config.maxLength;
            return max > 0 && text.length > max ? text.slice(0, max) : text;
        }

        // Indices of every occurrence of `search`. Case-insensitive matching folds both
        // sides, falling back to an exact match when folding would shift the indices.
        private matches(search: string, matchCase: boolean): number[] {
            const value = this.textArea.value;
            if (!search) return [];

            let haystack = value;
            let needle = search;
            if (!matchCase) {
                const foldedText = value.toLowerCase();
                const foldedSearch = search.toLowerCase();
                if (foldedText.length === value.length && foldedSearch.length === search.length) {
                    haystack = foldedText;
                    needle = foldedSearch;
                }
            }

            const result: number[] = [];
            let i = haystack.indexOf(needle);
            while (i >= 0) {
                result.push(i);
                i = haystack.indexOf(needle, i + needle.length);
            }
            return result;
        }

        private imageFiles(fileList: FileList | null, items: DataTransferItemList | null): File[] {
            const files: File[] = [];
            if (fileList) {
                for (let i = 0; i < fileList.length; i++) {
                    if (fileList[i].type.startsWith('image/')) files.push(fileList[i]);
                }
            }
            if (!files.length && items) {
                for (let i = 0; i < items.length; i++) {
                    if (items[i].kind === 'file' && items[i].type.startsWith('image/')) {
                        const f = items[i].getAsFile();
                        if (f) files.push(f);
                    }
                }
            }
            return files.filter(f => this.acceptImage(f));
        }

        // Refuses a file before its bytes are ever read, so an oversized image never becomes a
        // base64 payload on its way to .NET. The rejection is reported so the app can say why.
        private acceptImage(file: File): boolean {
            const accept = this.config.imageAccept;
            if (accept) {
                const allowed = accept.split(',').map(a => a.trim().toLowerCase()).filter(a => a.length > 0);
                const type = (file.type || '').toLowerCase();
                const name = (file.name || '').toLowerCase();
                const ok = allowed.some(a =>
                    a === type ||
                    (a.endsWith('/*') && type.startsWith(a.slice(0, -1))) ||
                    (a.startsWith('.') && name.endsWith(a)));
                if (!ok) {
                    this.invoke('OnImageRejected', file.name || 'image', file.type, file.size, 'Type');
                    return false;
                }
            }

            if (this.config.maxImageSize > 0 && file.size > this.config.maxImageSize) {
                this.invoke('OnImageRejected', file.name || 'image', file.type, file.size, 'Size');
                return false;
            }

            return true;
        }

        private async uploadFiles(files: File[]) {
            for (const file of files) {
                const token = `…${this.config.uploadingText}-${++this._uploadSeq}…`;
                const name = file.name || 'image';
                // Insert a placeholder immediately so the user sees progress.
                this.insertText(`![${token}]()`);

                try {
                    const base64 = await this.fileToBase64(file);
                    const url = await this.dotnetObj?.invokeMethodAsync<string | null>('UploadImage', name, base64, file.type);
                    const replacement = url ? `![${this.escapeAlt(name)}](${url})` : '';
                    this.replaceToken(`![${token}]()`, replacement);
                } catch {
                    this.replaceToken(`![${token}]()`, '');
                }
            }
        }

        private replaceToken(token: string, replacement: string) {
            const value = this.textArea.value;
            const idx = value.indexOf(token);
            if (idx < 0) return;
            // Close any active typing session so undo captures the post-replacement
            // state instead of the stale placeholder baseline.
            this.endTypingGroup();
            this.textArea.value = this.limit(value.slice(0, idx) + replacement + value.slice(idx + token.length));
            const caret = Math.min(idx + replacement.length, this.textArea.value.length);
            if (document.activeElement === this.textArea) this.textArea.setSelectionRange(caret, caret);
            this.saveSelection();
            this.flushChange();
            this._baseline = this.snapshot();
        }

        private fileToBase64(file: File): Promise<string> {
            return new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => {
                    const result = reader.result as string;
                    const comma = result.indexOf(',');
                    resolve(comma >= 0 ? result.slice(comma + 1) : result);
                };
                reader.onerror = () => reject(reader.error);
                reader.readAsDataURL(file);
            });
        }

        private escapeAlt(text: string) {
            return text.replace(/[\[\]]/g, '').trim();
        }

        private isUrl(text: string) {
            const t = text.trim();
            return /^(https?:\/\/|mailto:)\S+$/i.test(t) && !/\s/.test(t);
        }

        private gridToMarkdown(grid: string[][]): string | null {
            const cols = Math.max(...grid.map(r => r.length));
            if (cols === 0) return null;

            const pad = (r: string[]) => { while (r.length < cols) r.push(''); return r; };
            const header = pad(grid[0]);
            const body = grid.slice(1).map(pad);

            let md = '\n| ' + header.join(' | ') + ' |\n';
            md += '| ' + header.map(() => '---').join(' | ') + ' |\n';
            for (const r of body) md += '| ' + r.join(' | ') + ' |\n';
            return md;
        }

        private htmlTableToMarkdown(html: string): string | null {
            try {
                const doc = new DOMParser().parseFromString(html, 'text/html');
                const table = doc.querySelector('table');
                if (!table) return null;

                const rows = Array.from(table.querySelectorAll('tr'));
                if (!rows.length) return null;

                const grid = rows.map(r =>
                    Array.from(r.querySelectorAll('th,td')).map(c => (c.textContent || '').replace(/\s+/g, ' ').trim().replace(/\|/g, '\\|')));

                return this.gridToMarkdown(grid);
            } catch {
                return null;
            }
        }

        // Spreadsheet cells arrive as tab separated rows. Only convert when there is more
        // than one row and every one of them is tabbed, so ordinary text is never mangled.
        private tsvToMarkdown(text: string): string | null {
            const lines = text.replace(/\r\n?/g, '\n').replace(/\n+$/, '').split('\n');
            if (lines.length < 2) return null;
            if (lines.every(l => l.indexOf('\t') >= 0) === false) return null;

            const grid = lines.map(l => l.split('\t').map(c => c.trim().replace(/\|/g, '\\|')));
            if (Math.max(...grid.map(r => r.length)) < 2) return null;

            return this.gridToMarkdown(grid);
        }

        private syncScroll(from: HTMLElement | null, to: HTMLElement | null) {
            if (!from || !to || this._syncingScroll) return;
            // Only meaningful when both panes are visible (split mode).
            if (to.offsetParent === null || from.offsetParent === null) return;

            const fromRange = from.scrollHeight - from.clientHeight;
            const toRange = to.scrollHeight - to.clientHeight;
            if (fromRange <= 0 || toRange <= 0) return;

            this._syncingScroll = true;
            to.scrollTop = (from.scrollTop / fromRange) * toRange;
            // Release on the next frame so the mirrored scroll doesn't echo back.
            requestAnimationFrame(() => { this._syncingScroll = false; });
        }

        private snapshot(): MdeSnapshot {
            return {
                text: this.textArea.value,
                selStart: this.textArea.selectionStart,
                selEnd: this.textArea.selectionEnd
            };
        }

        private currentLine() {
            const value = this.textArea.value;
            const pos = this.textArea.selectionStart;
            const start = value.lastIndexOf('\n', pos - 1) + 1;
            let end = value.indexOf('\n', pos);
            if (end < 0) end = value.length;
            return value.slice(start, end);
        }

        private saveSelection() {
            this._lastSelection = { start: this.textArea.selectionStart, end: this.textArea.selectionEnd };
        }

        // Debounced push of the value to .NET, cutting interop chatter (important on
        // Blazor Server) while a short window keeps two-way binding responsive.
        private scheduleChange() {
            if (this.config.changeDebounceMs <= 0) {
                if (this.config.autoSaveKey) this.saveDraft();
                this.notifyChange();
                return;
            }
            // Debounce the draft save alongside the change notification so we don't hit
            // localStorage synchronously on every keystroke. flushChange() (blur / before
            // notify) is the safety net that still persists the latest value promptly.
            if (this._changeTimer) clearTimeout(this._changeTimer);
            this._changeTimer = setTimeout(() => {
                this._changeTimer = null;
                if (this.config.autoSaveKey) this.saveDraft();
                this.notifyChange();
            }, this.config.changeDebounceMs);
        }

        private flushChange() {
            if (this._changeTimer) {
                clearTimeout(this._changeTimer);
                this._changeTimer = null;
            }
            if (this.config.autoSaveKey) this.saveDraft();
            this.notifyChange();
        }

        private saveDraft() {
            if (!this.config.autoSaveKey) return;
            try { window.localStorage.setItem(this.config.autoSaveKey, this.textArea.value); } catch { }
        }

        // Fire-and-forget interop: a call landing after the circuit (or the component) is
        // gone rejects, and an unhandled rejection surfaces as a console error.
        private invoke(method: string, ...args: unknown[]) {
            this.dotnetObj?.invokeMethodAsync(method, ...args).catch(() => { });
        }

        private notifyChange() {
            this.invoke('OnChange', this.textArea.value);
        }

        private scheduleSelectionReport() {
            if (!this.config.reportSelection) return;

            if (this._selectionTimer) clearTimeout(this._selectionTimer);
            this._selectionTimer = setTimeout(() => {
                this._selectionTimer = null;

                // Active formats are decided line by line, so only the lines the selection
                // touches travel to .NET: sending a whole document on every caret move
                // would be a lot of interop traffic for a document of any size.
                const value = this.textArea.value;
                const start = this.textArea.selectionStart;
                const end = this.textArea.selectionEnd;
                const lineStart = value.lastIndexOf('\n', start - 1) + 1;
                let lineEnd = value.indexOf('\n', end);
                if (lineEnd < 0) lineEnd = value.length;

                this.invoke('OnSelectionChanged', start - lineStart, end - lineStart, value.slice(lineStart, lineEnd));
            }, MarkdownEditorCore.SELECTION_DEBOUNCE_MS);
        }

        private notifyHistory() {
            const canUndo = this._undo.length > 0;
            const canRedo = this._redo.length > 0;
            if (canUndo === this._canUndo && canRedo === this._canRedo) return;

            this._canUndo = canUndo;
            this._canRedo = canRedo;
            this.invoke('OnHistoryChanged', canUndo, canRedo);
        }

        private pushUndo(snap: MdeSnapshot) {
            this._undo.push(snap);
            if (this._undo.length > MarkdownEditorCore.HISTORY_LIMIT) this._undo.shift();
        }

        private pushRedo(snap: MdeSnapshot) {
            this._redo.push(snap);
            if (this._redo.length > MarkdownEditorCore.HISTORY_LIMIT) this._redo.shift();
        }

        private endTypingGroup() {
            this._typingActive = false;
            if (this._typingTimer) {
                clearTimeout(this._typingTimer);
                this._typingTimer = null;
            }
        }

        private clearTimers() {
            this.endTypingGroup();
            if (this._changeTimer) { clearTimeout(this._changeTimer); this._changeTimer = null; }
            if (this._selectionTimer) { clearTimeout(this._selectionTimer); this._selectionTimer = null; }
        }

        // Captures undo history for free-form typing, coalescing rapid keystrokes into
        // a single step per word. The first keystroke of a burst records the state that
        // existed before it; a pause or a word boundary starts a new step.
        private recordTyping() {
            if (!this._typingActive) {
                this.pushUndo(this._baseline);
                this._redo = [];
                this._typingActive = true;
                this.notifyHistory();
            }

            if (this._typingTimer) clearTimeout(this._typingTimer);
            this._typingTimer = setTimeout(() => {
                this._typingActive = false;
                this._typingTimer = null;
            }, MarkdownEditorCore.TYPING_PAUSE_MS);

            this._baseline = this.snapshot();

            // Break the group after a word boundary so undo works word-by-word.
            const caret = this.textArea.selectionStart;
            const prev = caret > 0 ? this.textArea.value[caret - 1] : '';
            if (prev && MarkdownEditorCore.WORD_BOUNDARY.test(prev)) {
                this.endTypingGroup();
            }
        }

        // Writes a snapshot back to the textarea without feeding the change into the
        // history, while still notifying .NET of the new value.
        private applySnapshot(snap: MdeSnapshot) {
            this.textArea.value = snap.text;
            this.flushChange();
            this.textArea.focus();
            const max = snap.text.length;
            this.textArea.setSelectionRange(Math.min(snap.selStart, max), Math.min(snap.selEnd, max));
            this.saveSelection();
            this._baseline = this.snapshot();
        }

        private applyResult(result: MdeEditResult) {
            this.textArea.value = this.limit(result.text);
            this.flushChange();
            this.textArea.focus();
            const max = this.textArea.value.length;
            this.textArea.setSelectionRange(Math.min(result.selectionStart, max), Math.min(result.selectionEnd, max));
            this.saveSelection();
            this.scheduleSelectionReport();
            this._baseline = this.snapshot();
            this.notifyHistory();
        }
    }
}
