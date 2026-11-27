namespace BitBlazorUI {
    // bit-trim: on-demand
    // The undo history of BitRichTextEditor (BitRichTextEditor.ts), which replaces the browser's own: once a command
    // changes the DOM itself rather than through document.execCommand, the native stack no longer matches the content.
    // Nothing here reads or writes the DOM - a state is the surface's markup and an opaque selection the editor script
    // takes and puts back - so the Node tests of the component (Tests/.../RichTextEditor/Scripts) run it as it is.
    //
    // The model is the one BitMarkdownEditor's history follows. `baseline` is the last state committed as a step, and
    // `latest` the last state the editor captured: typing moves `latest` on every input and commits only when a run
    // ends (a word boundary, a pause, another kind of input, a command), so one Ctrl+Z takes back a word rather than a
    // character.

    export interface BitRichTextEditorHistoryState {
        html: string;
        // What the editor script needs to put the selection back; never read here.
        selection: any;
    }

    // What an input does to the text, as far as grouping it into steps goes.
    export type BitRichTextEditorInputKind = 'insert' | 'delete' | 'compose' | 'other';

    export class BitRichTextEditorHistory {
        // Characters that end an undo "word" (the step closes after them), as in BitMarkdownEditor.
        public static readonly WORD_BOUNDARY = /[\s.,;:!?()\[\]{}'" ]/;
        // A pause longer than this starts a fresh step, even mid-word.
        public static readonly TYPING_PAUSE_MS = 600;

        private _undo: BitRichTextEditorHistoryState[] = [];
        private _redo: BitRichTextEditorHistoryState[] = [];
        private _baseline: BitRichTextEditorHistoryState;
        private _latest: BitRichTextEditorHistoryState;
        // The characters of the committed states (the undo and redo stacks and the baseline); a captured state counts
        // once it is committed.
        private _size = 0;
        private _runKind: BitRichTextEditorInputKind | null = null;
        private _lastInputAt = 0;
        // Whether anything was ever recorded: before that, a state set from outside (the bound value arriving) is the
        // starting point rather than an edit to undo.
        private _touched = false;

        constructor(initial: BitRichTextEditorHistoryState,
            public readonly maxSteps: number = 200,
            public readonly maxChars: number = 25_000_000) {
            this._baseline = this._latest = initial;
            this._size = initial.html.length;
        }

        public get canUndo(): boolean { return this._undo.length > 0 || this.hasPending; }
        public get canRedo(): boolean { return this._redo.length > 0; }
        public get touched(): boolean { return this._touched; }
        public get undoDepth(): number { return this._undo.length; }
        public get redoDepth(): number { return this._redo.length; }

        // Whether the last captured state differs from the last committed one: typing not yet made a step.
        public get hasPending(): boolean { return this._latest.html !== this._baseline.html; }

        // Starts over from a state, with nothing to undo or redo.
        public reset(state: BitRichTextEditorHistoryState) {
            this._undo = [];
            this._redo = [];
            this._baseline = this._latest = state;
            this._size = state.html.length;
            this._runKind = null;
            this._touched = false;
        }

        // Replaces the state the history is at without making a step: what the editor shows once it has tidied a state
        // it put back (an undo, a redo), so that tidying is not taken for an edit.
        public replaceCurrent(state: BitRichTextEditorHistoryState) {
            this._size += state.html.length - this._baseline.html.length;
            this._baseline = this._latest = state;
        }

        // Records the state the editor now shows, without making it a step yet (typing in progress).
        public capture(state: BitRichTextEditorHistoryState) {
            if (state.html !== this._latest.html) this._touched = true;
            // The content moved on from what an undo left it at, so the undone steps no longer follow from it: they go
            // at once, not when the typing is committed, so Redo is not offered for a step that can no longer apply.
            if (state.html !== this._baseline.html) this.clearRedo();
            this._latest = state;
        }

        // Makes what was captured since the last step a step of its own. Returns whether a step was made.
        public commit(): boolean {
            this._runKind = null;
            if (this.hasPending === false) {
                // Same content: keep the newer selection, so undoing back to this state puts the caret where it was last.
                this._baseline = this._latest;
                return false;
            }

            this._undo.push(this._baseline);
            this._size += this._latest.html.length;
            this._baseline = this._latest;
            this.clearRedo();
            this.trim();
            this._touched = true;
            return true;
        }

        // Captures a state and commits it at once: what a command, a paste or an input rule does.
        public record(state: BitRichTextEditorHistoryState): boolean {
            this.commit();
            this.capture(state);
            return this.commit();
        }

        // Called before an input changes the text. Returns whether the run in progress must be committed first: the
        // kind of input changed, it is not a kind that groups, or the typing paused.
        public beforeInput(kind: BitRichTextEditorInputKind, now: number): boolean {
            const paused = now - this._lastInputAt > BitRichTextEditorHistory.TYPING_PAUSE_MS;
            const breaks = this._runKind !== null && (kind !== this._runKind || kind === 'other' || (paused && kind !== 'compose'));
            this._lastInputAt = now;
            if (breaks) return true;
            this._runKind = kind;
            return false;
        }

        // Called after an input changed the text and its state was captured. Returns whether the step closes here: an
        // input that does not group, or a typed word boundary.
        public afterInput(kind: BitRichTextEditorInputKind, data: string | null): boolean {
            if (kind === 'other') return true;
            if (kind === 'insert' && data && BitRichTextEditorHistory.WORD_BOUNDARY.test(data[data.length - 1])) return true;
            this._runKind = kind;
            return false;
        }

        // Steps back: returns the state to show, or null when there is nothing to undo.
        public undo(): BitRichTextEditorHistoryState | null {
            this.commit();
            const previous = this._undo.pop();
            if (!previous) return null;
            // The states only move between the stacks, so the size holds.
            this._redo.push(this._baseline);
            this._baseline = this._latest = previous;
            return previous;
        }

        // Steps forward again: returns the state to show, or null when there is nothing to redo.
        public redo(): BitRichTextEditorHistoryState | null {
            this.commit();
            const next = this._redo.pop();
            if (!next) return null;
            this._undo.push(this._baseline);
            this._baseline = this._latest = next;
            return next;
        }

        // How many characters the history holds, for its size limit.
        public get size(): number { return this._size; }

        public static kindOf(inputType: string | null | undefined): BitRichTextEditorInputKind {
            switch (inputType) {
                case 'insertText':
                case 'insertReplacementText':
                    return 'insert';
                case 'insertCompositionText':
                    return 'compose';
                default:
                    return inputType && inputType.startsWith('delete') && inputType !== 'deleteByDrag' && inputType !== 'deleteByCut'
                        ? 'delete'
                        : 'other';
            }
        }

        private clearRedo() {
            for (const state of this._redo) this._size -= state.html.length;
            this._redo = [];
        }

        // Drops the oldest steps past either limit. The newest state is always kept, however large.
        private trim() {
            while (this._undo.length > this.maxSteps || (this._size > this.maxChars && this._undo.length > 0)) {
                const oldest = this._undo.shift()!;
                this._size -= oldest.html.length;
            }
        }
    }
}
