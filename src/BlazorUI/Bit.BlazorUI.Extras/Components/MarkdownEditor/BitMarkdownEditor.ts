namespace BitBlazorUI {

    type MarkdownEditorConfig = {
        imageUpload: boolean;
        syncScroll: boolean;
        autoPair: boolean;
        autoSaveKey?: string | null;
        changeDebounceMs: number;
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

        public static replaceAll(id: string, search: string, replacement: string, all: boolean) {
            return MarkdownEditor._editors[id]?.replaceAll(search, replacement, all) ?? 0;
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
            this.config = config ?? { imageUpload: false, syncScroll: true, autoPair: true, autoSaveKey: null, changeDebounceMs: 0 };

            this._baseline = this.snapshot();
            this._lastSelection = { start: textArea.selectionStart || 0, end: textArea.selectionEnd || 0 };

            textArea.addEventListener('keydown', this.keyDownHandler);
            textArea.addEventListener('input', this.inputHandler);
            textArea.addEventListener('blur', this.blurHandler);
            textArea.addEventListener('paste', this.pasteHandler);
            textArea.addEventListener('drop', this.dropHandler);
            textArea.addEventListener('dragover', this.dragOverHandler);
            textArea.addEventListener('mouseup', this.saveSelectionHandler);
            textArea.addEventListener('keyup', this.saveSelectionHandler);
            // Capture the selection whenever it changes while the textarea is focused,
            // so commands always know the intended range.
            document.addEventListener('selectionchange', this.selectionChangeHandler);
            // Stop toolbar buttons from stealing focus from the textarea. A native
            // mousedown/touchstart preventDefault reliably keeps the caret in place.
            root?.addEventListener('mousedown', this.toolbarPointerDownHandler);
            root?.addEventListener('touchstart', this.toolbarPointerDownHandler, { passive: false });

            if (this.config.syncScroll && root) {
                this.editorPane = root.querySelector('.bit-mde-epn');
                this.previewPane = root.querySelector('.bit-mde-ppn');
                this.editorPane?.addEventListener('scroll', this.editorScrollHandler);
                this.previewPane?.addEventListener('scroll', this.previewScrollHandler);
            }

            this.toolbar = root?.querySelector('.bit-mde-tlb') ?? null;
            if (this.toolbar) {
                this.toolbar.addEventListener('keydown', this.toolbarKeydownHandler);
                this.toolbar.addEventListener('focusin', this.toolbarFocusInHandler);
                this.initToolbarRoving();
                this.initToolbarDropdowns();
            }
        }

        // The toolbar dropdowns open on hover / focus-within purely via CSS. Mirror that
        // visible state onto the trigger's aria-expanded so assistive tech knows when the
        // menu is actually open (aria-haspopup alone only says a menu exists).
        private initToolbarDropdowns() {
            const dropdowns = this.toolbar?.querySelectorAll<HTMLElement>('.bit-mde-dd') ?? [];
            dropdowns.forEach(dd => {
                const trigger = dd.querySelector<HTMLButtonElement>(':scope > .bit-mde-btn');
                if (!trigger) return;
                const setExpanded = (open: boolean) => trigger.setAttribute('aria-expanded', open ? 'true' : 'false');
                dd.addEventListener('pointerenter', () => setExpanded(true));
                dd.addEventListener('pointerleave', () => setExpanded(dd.contains(document.activeElement)));
                dd.addEventListener('focusin', () => setExpanded(true));
                dd.addEventListener('focusout', e => setExpanded(dd.contains(e.relatedTarget as Node) || dd.matches(':hover')));
            });
        }

        // Implements the WAI-ARIA toolbar pattern: a single tab stop, arrow keys move
        // focus between buttons. Managed here so Blazor's diffing is untouched.
        private initToolbarRoving() {
            const buttons = this.toolbarButtons();
            buttons.forEach((b, i) => b.setAttribute('tabindex', i === 0 ? '0' : '-1'));
        }

        private toolbarButtons(): HTMLButtonElement[] {
            if (!this.toolbar) return [];
            // Top-level buttons and dropdown triggers only, not the buttons inside menus.
            return Array.from(this.toolbar.querySelectorAll<HTMLButtonElement>(':scope > .bit-mde-btn, :scope > .bit-mde-dd > .bit-mde-btn'));
        }

        private toolbarFocusInHandler = (e: FocusEvent) => {
            const btn = (e.target as HTMLElement)?.closest('.bit-mde-btn') as HTMLButtonElement | null;
            if (!btn) return;
            for (const b of this.toolbarButtons()) b.setAttribute('tabindex', b === btn ? '0' : '-1');
        };

        private toolbarKeydownHandler = (e: KeyboardEvent) => {
            if (!['ArrowRight', 'ArrowLeft', 'Home', 'End'].includes(e.key)) return;

            const buttons = this.toolbarButtons().filter(b => !b.disabled);
            if (!buttons.length) return;

            const current = document.activeElement as HTMLElement;
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

        public getValue() {
            return this.textArea.value;
        }

        // Pushes an externally-changed value into the (uncontrolled) textarea without
        // notifying .NET back, so we don't loop the change into Blazor again.
        public setValue(value: string) {
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
        }

        public resetBaseline() {
            this._baseline = this.snapshot();
        }

        public focus() {
            this.textArea.focus();
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
        public replaceAll(search: string, replacement: string, all: boolean): number {
            if (this.textArea.readOnly || !search) return 0;
            const value = this.textArea.value;
            let count = 0;
            let result: string;
            if (all) {
                result = value.split(search).join(replacement);
                count = value.split(search).length - 1;
            } else {
                const idx = value.indexOf(search);
                if (idx < 0) return 0;
                result = value.slice(0, idx) + replacement + value.slice(idx + search.length);
                count = 1;
            }
            if (count === 0) return 0;

            this.endTypingGroup();
            this.pushUndo(this.snapshot());
            this._redo = [];
            this.textArea.value = result;
            const caret = Math.min(this.textArea.selectionStart, result.length);
            this.textArea.setSelectionRange(caret, caret);
            this.saveSelection();
            this.flushChange();
            this._baseline = this.snapshot();
            this.notifyHistory();
            return count;
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

            this.textArea.removeEventListener('keydown', this.keyDownHandler);
            this.textArea.removeEventListener('input', this.inputHandler);
            this.textArea.removeEventListener('blur', this.blurHandler);
            this.textArea.removeEventListener('paste', this.pasteHandler);
            this.textArea.removeEventListener('drop', this.dropHandler);
            this.textArea.removeEventListener('dragover', this.dragOverHandler);
            this.textArea.removeEventListener('mouseup', this.saveSelectionHandler);
            this.textArea.removeEventListener('keyup', this.saveSelectionHandler);
            document.removeEventListener('selectionchange', this.selectionChangeHandler);
            this.root?.removeEventListener('mousedown', this.toolbarPointerDownHandler);
            this.root?.removeEventListener('touchstart', this.toolbarPointerDownHandler);
            this.editorPane?.removeEventListener('scroll', this.editorScrollHandler);
            this.previewPane?.removeEventListener('scroll', this.previewScrollHandler);
            this.toolbar?.removeEventListener('keydown', this.toolbarKeydownHandler);
            this.toolbar?.removeEventListener('focusin', this.toolbarFocusInHandler);

            this.dotnetObj = undefined;
            this.root = undefined;
            this.editorPane = null;
            this.previewPane = null;
            this.toolbar = null;
        }

        // ==========================================================

        private keyDownHandler = (e: KeyboardEvent) => {
            if (e.isComposing) return;

            const mod = e.ctrlKey || e.metaKey;

            if (mod && !e.altKey) {
                const key = e.key.toLowerCase();
                // Undo / redo. Ctrl/Cmd+Z, Ctrl/Cmd+Shift+Z and Ctrl/Cmd+Y.
                if (key === 'z' && !e.shiftKey) { e.preventDefault(); this.undo(); return; }
                if ((key === 'z' && e.shiftKey) || (key === 'y' && !e.shiftKey)) { e.preventDefault(); this.redo(); return; }
                if (e.shiftKey && key === 's') { e.preventDefault(); this.runCommand('Strikethrough'); return; }
                if (e.shiftKey) return;
                switch (key) {
                    case 'b': e.preventDefault(); this.runCommand('Bold'); return;
                    case 'i': e.preventDefault(); this.runCommand('Italic'); return;
                    case 'k': e.preventDefault(); this.runCommand('Link'); return;
                    case 'f': e.preventDefault(); this.dotnetObj?.invokeMethodAsync('OnFindShortcut'); return;
                }
                return;
            }

            // Escape leaves full-screen mode (the help panel is handled in .NET).
            if (e.key === 'Escape') {
                this.dotnetObj?.invokeMethodAsync('OnEscape');
                return;
            }

            // Wrap the selection when a pairing character is typed over it.
            if (this.config.autoPair && !mod && !this.textArea.readOnly &&
                this.textArea.selectionStart !== this.textArea.selectionEnd &&
                Object.prototype.hasOwnProperty.call(MarkdownEditorCore.PAIRS, e.key)) {
                e.preventDefault();
                this.wrapSelection(e.key, MarkdownEditorCore.PAIRS[e.key]);
                return;
            }

            // Only hijack Tab while the editor is writable; in read-only mode the
            // default behavior must remain so keyboard focus is not trapped.
            if (e.key === 'Tab') {
                if (this.textArea.readOnly) return;
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

            // 3) A URL pasted over a selection -> turn the selection into a link.
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
            }
        };

        private dropHandler = (e: DragEvent) => {
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
            this.textArea.value = value.slice(0, start) + replacement + value.slice(end);
            this.textArea.focus();
            this.textArea.setSelectionRange(selStart, selEnd);
            this.saveSelection();
            this.flushChange();
            this._baseline = this.snapshot();
            this.notifyHistory();
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
            return files;
        }

        private async uploadFiles(files: File[]) {
            for (const file of files) {
                const token = `…uploading-${++this._uploadSeq}…`;
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
            this.textArea.value = value.slice(0, idx) + replacement + value.slice(idx + token.length);
            const caret = idx + replacement.length;
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

        private htmlTableToMarkdown(html: string): string | null {
            try {
                const doc = new DOMParser().parseFromString(html, 'text/html');
                const table = doc.querySelector('table');
                if (!table) return null;

                const rows = Array.from(table.querySelectorAll('tr'));
                if (!rows.length) return null;

                const grid = rows.map(r =>
                    Array.from(r.querySelectorAll('th,td')).map(c => (c.textContent || '').replace(/\s+/g, ' ').trim().replace(/\|/g, '\\|')));

                const cols = Math.max(...grid.map(r => r.length));
                if (cols === 0) return null;

                const pad = (r: string[]) => { while (r.length < cols) r.push(''); return r; };
                const header = pad(grid[0]);
                const body = grid.slice(1).map(pad);

                let md = '\n| ' + header.join(' | ') + ' |\n';
                md += '| ' + header.map(() => '---').join(' | ') + ' |\n';
                for (const r of body) md += '| ' + r.join(' | ') + ' |\n';
                return md;
            } catch {
                return null;
            }
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

        private notifyChange() {
            this.dotnetObj?.invokeMethodAsync('OnChange', this.textArea.value);
        }

        private scheduleSelectionReport() {
            if (this._selectionTimer) clearTimeout(this._selectionTimer);
            this._selectionTimer = setTimeout(() => {
                this._selectionTimer = null;
                this.dotnetObj?.invokeMethodAsync('OnSelectionChanged', this.textArea.selectionStart, this.textArea.selectionEnd, this.textArea.value);
            }, MarkdownEditorCore.SELECTION_DEBOUNCE_MS);
        }

        private notifyHistory() {
            const canUndo = this._undo.length > 0;
            const canRedo = this._redo.length > 0;
            if (canUndo === this._canUndo && canRedo === this._canRedo) return;

            this._canUndo = canUndo;
            this._canRedo = canRedo;
            this.dotnetObj?.invokeMethodAsync('OnHistoryChanged', canUndo, canRedo);
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
            this.textArea.value = result.text;
            this.flushChange();
            this.textArea.focus();
            this.textArea.setSelectionRange(result.selectionStart, result.selectionEnd);
            this.saveSelection();
            this.scheduleSelectionReport();
            this._baseline = this.snapshot();
            this.notifyHistory();
        }
    }
}
