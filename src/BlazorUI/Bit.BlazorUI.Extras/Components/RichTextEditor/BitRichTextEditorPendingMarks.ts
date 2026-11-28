namespace BitBlazorUI {
    // bit-trim: on-demand
    // The marks and styles set at a caret for the text typed there next (BitRichTextEditorMarks.ts writes them). A mark
    // toggled with nothing selected changes no markup: it is held here until a character is typed at that caret, which is
    // then written inside the marks turned on and outside the ones turned off. Once the caret moves, they are gone, as
    // they have always been with the browser's own commands. An IME composition started at that caret takes them along
    // and has them applied to what it wrote when it ends.
    //
    // The state lives on the root element the marks work in (_pendingMarks, _composeMarks), so every editable root keeps
    // its own: BitRichTextEditor's surface, and each text block of an editor that has several. The caller owns the
    // selection, the history and what it tells .NET; everything here takes the Range the caller read.

    export interface BitRichTextEditorPending {
        at: number;
        marks: { [mark: string]: boolean };
        styles: { [property: string]: string };
    }

    export class BitRichTextEditorPendingMarks {
        // Drops what is pending and any composition holding it: the content was replaced, or the caret is gone.
        public static clear(root: any) {
            root._pendingMarks = null;
            root._composeMarks = null;
        }

        // What is pending at the caret of a range, or null for a selection or a caret somewhere else.
        public static at(root: any, range: Range | null): BitRichTextEditorPending | null {
            const pending = root._pendingMarks as BitRichTextEditorPending | null;
            return pending && range && range.collapsed && BitRichTextEditorPendingMarks.offset(root, range) === pending.at
                ? pending
                : null;
        }

        // Drops what is pending once the selection is no longer the caret it was set at.
        public static follow(root: any, range: Range) {
            const pending = root._pendingMarks as BitRichTextEditorPending | null;
            if (pending && (!range.collapsed || BitRichTextEditorPendingMarks.offset(root, range) !== pending.at)) {
                root._pendingMarks = null;
            }
        }

        // Whether a mark is on where the selection is: off the markup, or at a caret, as set there for what is typed next.
        public static isActive(root: any, range: Range, mark: BitRichTextEditorMark): boolean {
            const pending = BitRichTextEditorPendingMarks.at(root, range);
            if (pending && mark in pending.marks) return pending.marks[mark];
            return BitRichTextEditorMarks.isActive(root, range, mark);
        }

        // A color or font where the selection is: set at a caret for the text typed there next, else as laid out.
        public static styleAt(root: any, range: Range | null, property: string): string {
            if (!range) return '';
            const pending = BitRichTextEditorPendingMarks.at(root, range);
            if (pending && pending.styles && property in pending.styles) return pending.styles[property];
            return BitRichTextEditorMarks.styleAt(root, range, property);
        }

        // Sets a mark for the text typed next at the caret. A mark set back to what the markup there already says is
        // no longer pending; turning one of sub- and superscript on turns the other off.
        public static set(root: any, range: Range, mark: BitRichTextEditorMark, on: boolean) {
            const pending = BitRichTextEditorPendingMarks.open(root, range);
            const assign = (name: BitRichTextEditorMark, value: boolean) => {
                if (BitRichTextEditorMarks.isActive(root, range, name) === value) delete pending.marks[name];
                else pending.marks[name] = value;
            };
            const other = BitRichTextEditorMarks.EXCLUDES[mark] as BitRichTextEditorMark | undefined;
            if (on && other && BitRichTextEditorPendingMarks.isActive(root, range, other)) assign(other, false);
            assign(mark, on);
            if (Object.keys(pending.marks).length === 0 && Object.keys(pending.styles).length === 0) root._pendingMarks = null;
        }

        // Turns off, for the text typed next at the caret, every mark that is on there.
        public static clearAt(root: any, range: Range) {
            for (const mark of Object.keys(BitRichTextEditorMarks.WRITES) as BitRichTextEditorMark[]) {
                if (BitRichTextEditorPendingMarks.isActive(root, range, mark)) BitRichTextEditorPendingMarks.set(root, range, mark, false);
            }
        }

        // Sets a color or font, already checked by BitRichTextEditorMarks.styleValue, for the text typed next at the caret.
        public static setStyle(root: any, range: Range, property: string, value: string) {
            BitRichTextEditorPendingMarks.open(root, range).styles[property] = value;
        }

        // Writes a character typed where marks are pending, inside the marks turned on and outside the ones turned off;
        // what is typed after it goes on in the same text, which the marks engine takes over. Returns undefined when the
        // input is not one to write (and the browser's default goes ahead), else the caret after what was written, or
        // null when nothing could be. What was pending is used up by any typed text, written or not.
        public static type(root: any, range: Range | null, e: InputEvent): Range | null | undefined {
            const pending = root._pendingMarks as BitRichTextEditorPending | null;
            if (!pending) return undefined;
            if ((e.inputType !== 'insertText' && e.inputType !== 'insertReplacementText') || !e.data) return undefined;
            root._pendingMarks = null;
            if (!range || !range.collapsed || BitRichTextEditorPendingMarks.offset(root, range) !== pending.at) return undefined;

            e.preventDefault();
            // A lone trailing space collapses away unless it is non-breaking, which is also what the engines write.
            return BitRichTextEditorMarks.insertText(root, range, e.data === ' ' ? ' ' : e.data, pending.marks, pending.styles);
        }

        // An IME composition starting at the caret takes what is pending there along.
        public static startComposition(root: any, range: Range | null) {
            root._composeMarks = BitRichTextEditorPendingMarks.at(root, range);
        }

        // Applies what the composition took along to the text it wrote, and returns the caret after it; null when it took
        // nothing along or wrote nothing where it started. Either way nothing is pending any more.
        public static endComposition(root: any, range: Range | null): Range | null {
            const compose = root._composeMarks as BitRichTextEditorPending | null;
            BitRichTextEditorPendingMarks.clear(root);
            let written = compose && range && range.collapsed
                ? BitRichTextEditorMarks.rangeAt(root, compose.at, BitRichTextEditorPendingMarks.offset(root, range))
                : null;
            if (!written) return null;

            for (const mark of Object.keys(compose!.marks)) {
                if (!written || !BitRichTextEditorMarks.isMark(mark)) continue;
                written = BitRichTextEditorMarks.apply(root, written, mark, compose!.marks[mark]) || written;
            }
            for (const property of Object.keys(compose!.styles || {})) {
                written = BitRichTextEditorMarks.applyStyle(root, written!, property, compose!.styles[property]) || written;
            }
            const caret = written!.cloneRange();
            caret.collapse(false);
            return caret;
        }

        // What is pending at the caret, started afresh when the caret is not where the last of it was set.
        private static open(root: any, range: Range): BitRichTextEditorPending {
            const at = BitRichTextEditorPendingMarks.offset(root, range);
            let pending = root._pendingMarks as BitRichTextEditorPending | null;
            if (!pending || pending.at !== at) pending = root._pendingMarks = { at, marks: {}, styles: {} };
            return pending;
        }

        private static offset(root: any, range: Range): number {
            return BitRichTextEditorMarks.textOffset(root, range.startContainer, range.startOffset);
        }
    }
}
