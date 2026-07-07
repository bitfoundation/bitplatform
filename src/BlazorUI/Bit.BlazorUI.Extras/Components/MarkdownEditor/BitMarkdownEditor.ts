namespace BitBlazorUI {

    export class MarkdownEditor {
        private static _editors: { [key: string]: MarkdownEditorCore } = {};

        public static init(id: string, textArea: HTMLTextAreaElement, root: HTMLElement, dotnetObj: DotNetObject, defaultValue?: string | null) {
            MarkdownEditor.dispose(id);

            const editor = new MarkdownEditorCore(textArea, root, dotnetObj);

            if (defaultValue) {
                textArea.value = defaultValue;
                editor.resetBaseline();
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

        public static undo(id: string) {
            MarkdownEditor._editors[id]?.undo();
        }

        public static redo(id: string) {
            MarkdownEditor._editors[id]?.redo();
        }

        public static focus(id: string) {
            MarkdownEditor._editors[id]?.focus();
        }

        public static dispose(id: string) {
            if (!MarkdownEditor._editors[id]) return;

            MarkdownEditor._editors[id].dispose();

            delete MarkdownEditor._editors[id];
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

        // Maximum number of states kept per direction.
        private static readonly HISTORY_LIMIT = 200;
        // Consecutive keystrokes within this window are coalesced into one undo step.
        private static readonly TYPING_PAUSE_MS = 100;

        private _undo: MdeSnapshot[] = [];
        private _redo: MdeSnapshot[] = [];
        private _baseline: MdeSnapshot;
        private _typingActive = false;
        private _typingTimer: ReturnType<typeof setTimeout> | null = null;
        private _canUndo = false;
        private _canRedo = false;
        private _commandInFlight = false;
        private _lastSelection: { start: number, end: number };

        private textArea: HTMLTextAreaElement;
        private root: HTMLElement | undefined | null;
        private dotnetObj: DotNetObject | undefined | null;

        constructor(textArea: HTMLTextAreaElement, root: HTMLElement | undefined | null, dotnetObj: DotNetObject) {
            this.textArea = textArea;
            this.root = root;
            this.dotnetObj = dotnetObj;

            this._baseline = this.snapshot();
            this._lastSelection = { start: textArea.selectionStart || 0, end: textArea.selectionEnd || 0 };

            textArea.addEventListener('keydown', this.keyDownHandler);
            textArea.addEventListener('input', this.inputHandler);
            textArea.addEventListener('mouseup', this.saveSelectionHandler);
            textArea.addEventListener('keyup', this.saveSelectionHandler);
            // Capture the selection whenever it changes while the textarea is focused,
            // so commands always know the intended range.
            document.addEventListener('selectionchange', this.selectionChangeHandler);
            // Stop toolbar buttons from stealing focus from the textarea. A native
            // mousedown preventDefault reliably keeps the caret/selection in place.
            root?.addEventListener('mousedown', this.toolbarMouseDownHandler);
        }

        public getValue() {
            return this.textArea.value;
        }

        // Pushes an externally-changed value into the (uncontrolled) textarea without
        // notifying .NET back, so we don't loop the change into Blazor again.
        public setValue(value: string) {
            if (this.textArea.value !== value) {
                this.textArea.value = value;
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

        // Reads selection + value, asks C# to transform it, then writes the result back.
        public async runCommand(command: string) {
            if (!this.dotnetObj || this.textArea.readOnly || this._commandInFlight) return;

            // When a toolbar button takes focus, the textarea's live selection can be
            // lost, so fall back to the last selection captured while it was focused.
            const focused = document.activeElement === this.textArea;
            const start = focused ? this.textArea.selectionStart : this._lastSelection.start;
            const end = focused ? this.textArea.selectionEnd : this._lastSelection.end;
            const value = this.textArea.value;

            let result: MdeEditResult | null = null;
            this._commandInFlight = true;
            try {
                result = await this.dotnetObj.invokeMethodAsync<MdeEditResult>('ApplyCommand', command, start, end, value);
            } finally {
                this._commandInFlight = false;
            }

            if (!result || !result.handled) return;

            // Typing may have changed the value while awaiting .NET; applying the
            // stale result would wipe those newer edits, so drop the command instead.
            if (this.textArea.value !== value) return;

            // Record the state before the command so it can be undone as one step.
            this.endTypingGroup();
            this.pushUndo({ text: value, selStart: start, selEnd: end });
            this._redo = [];

            this.applyResult(result);
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
            if (this._typingTimer) {
                clearTimeout(this._typingTimer);
                this._typingTimer = null;
            }

            this.textArea.removeEventListener('keydown', this.keyDownHandler);
            this.textArea.removeEventListener('input', this.inputHandler);
            this.textArea.removeEventListener('mouseup', this.saveSelectionHandler);
            this.textArea.removeEventListener('keyup', this.saveSelectionHandler);
            document.removeEventListener('selectionchange', this.selectionChangeHandler);
            this.root?.removeEventListener('mousedown', this.toolbarMouseDownHandler);

            this.dotnetObj = undefined;
            this.root = undefined;
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
                }
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
            this.notifyChange();
        };

        private selectionChangeHandler = () => {
            if (document.activeElement === this.textArea) {
                this.saveSelection();
            }
        };

        private saveSelectionHandler = () => {
            this.saveSelection();
        };

        private toolbarMouseDownHandler = (e: MouseEvent) => {
            const target = e.target as HTMLElement;
            if (target?.closest && target.closest('.bit-mde-btn')) {
                e.preventDefault();
            }
        };

        // ==========================================================

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

        private notifyChange() {
            this.dotnetObj?.invokeMethodAsync('OnChange', this.textArea.value);
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

        // Captures undo history for free-form typing, coalescing rapid keystrokes into
        // a single step. The first keystroke of a burst records the state that existed
        // before it; subsequent keystrokes only refresh the baseline.
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
        }

        // Writes a snapshot back to the textarea without feeding the change into the
        // history, while still notifying .NET of the new value.
        private applySnapshot(snap: MdeSnapshot) {
            this.textArea.value = snap.text;
            this.notifyChange();
            this.textArea.focus();
            const max = snap.text.length;
            this.textArea.setSelectionRange(Math.min(snap.selStart, max), Math.min(snap.selEnd, max));
            this.saveSelection();
            this._baseline = this.snapshot();
        }

        private applyResult(result: MdeEditResult) {
            this.textArea.value = result.text;
            this.notifyChange();
            this.textArea.focus();
            this.textArea.setSelectionRange(result.selectionStart, result.selectionEnd);
            this.saveSelection();
            this._baseline = this.snapshot();
            this.notifyHistory();
        }
    }
}
