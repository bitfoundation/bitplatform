namespace BitBlazorUI {
    // bit-trim: on-demand
    // The pure logic of BitVirtualKeyboard (BitVirtualKeyboard.ts): what an edit of a text makes of it, how far
    // Backspace deletes, what a dead key and a character make together, how Shift moves through its states. Nothing here
    // reads or writes the DOM, so the Node tests of the component (Tests/.../VirtualKeyboard/Scripts) run it as it is.

    export interface BitVirtualKeyboardEdit {
        text: string;
        // The selection after the edit (the caret, when they are equal).
        start: number;
        end: number;
        // What the edit typed, after maxlength took its share.
        inserted: string;
    }

    export interface BitVirtualKeyboardShiftState {
        mode: 'off' | 'latched' | 'locked';
        lastTap: number;
    }

    export class BitVirtualKeyboardEngine {
        // Two taps of Shift closer than this lock it.
        public static readonly doubleTapMs = 400;

        // The input types whose selection a script can read and set (selectionStart, setRangeText); on every other type
        // the selection APIs throw.
        public static readonly selectableTypes = ['text', 'search', 'url', 'tel', 'password'];

        // The input types the keyboard types into; date, time, color, range and the rest sanitize partial text away.
        public static readonly qualifyingTypes = ['text', 'search', 'url', 'tel', 'password', 'email', 'number'];

        // Replaces the selection with the text, keeping to maxlength (in UTF-16 code units, as browsers count it) by
        // dropping whole code points off the end of what is typed; null when nothing of it fits.
        public static insert(text: string, start: number, end: number, insert: string, maxLength: number | null): BitVirtualKeyboardEdit | null {
            if (maxLength !== null && maxLength >= 0) {
                const room = maxLength - (text.length - (end - start));
                if (room <= 0) return null;
                insert = BitVirtualKeyboardEngine.truncate(insert, room);
                if (insert.length === 0) return null;
            }

            const caret = start + insert.length;
            return { text: text.slice(0, start) + insert + text.slice(end), start: caret, end: caret, inserted: insert };
        }

        // The longest start of the text, in whole code points, no longer than the given UTF-16 length.
        public static truncate(text: string, length: number): string {
            if (text.length <= length) return text;
            let end = length;
            const c = text.charCodeAt(end - 1);
            if (c >= 0xD800 && c <= 0xDBFF) end--;
            return text.slice(0, Math.max(0, end));
        }

        // Backspace: the selection, else what is before the caret - one code point, as Chromium and Firefox delete it
        // (a combining mark, a ZWNJ, a harakat alone), except a whole emoji sequence.
        public static deleteBackward(text: string, start: number, end: number): BitVirtualKeyboardEdit | null {
            if (start !== end) return { text: text.slice(0, start) + text.slice(end), start, end: start, inserted: '' };
            if (start <= 0) return null;

            const from = BitVirtualKeyboardEngine.backspaceStart(text, start);
            return { text: text.slice(0, from) + text.slice(start), start: from, end: from, inserted: '' };
        }

        // Delete: the selection, else what is after the caret, by the same rule.
        public static deleteForward(text: string, start: number, end: number): BitVirtualKeyboardEdit | null {
            if (start !== end) return { text: text.slice(0, start) + text.slice(end), start, end: start, inserted: '' };
            if (start >= text.length) return null;

            const to = BitVirtualKeyboardEngine.deleteEnd(text, start);
            return { text: text.slice(0, start) + text.slice(to), start, end: start, inserted: '' };
        }

        public static backspaceStart(text: string, index: number): number {
            const cluster = BitVirtualKeyboardEngine.previousGraphemeStart(text, index);
            if (BitVirtualKeyboardEngine.isEmojiSequence(text.slice(cluster, index))) return cluster;

            return BitVirtualKeyboardEngine.previousCodePoint(text, index);
        }

        public static deleteEnd(text: string, index: number): number {
            const cluster = BitVirtualKeyboardEngine.nextGraphemeEnd(text, index);
            if (BitVirtualKeyboardEngine.isEmojiSequence(text.slice(index, cluster))) return cluster;

            return BitVirtualKeyboardEngine.nextCodePoint(text, index);
        }

        public static previousCodePoint(text: string, index: number): number {
            if (index <= 0) return 0;
            const low = text.charCodeAt(index - 1);
            if (low >= 0xDC00 && low <= 0xDFFF && index >= 2) {
                const high = text.charCodeAt(index - 2);
                if (high >= 0xD800 && high <= 0xDBFF) return index - 2;
            }
            return index - 1;
        }

        public static nextCodePoint(text: string, index: number): number {
            if (index >= text.length) return text.length;
            const high = text.charCodeAt(index);
            if (high >= 0xD800 && high <= 0xDBFF && index + 1 < text.length) {
                const low = text.charCodeAt(index + 1);
                if (low >= 0xDC00 && low <= 0xDFFF) return index + 2;
            }
            return index + 1;
        }

        // The grapheme cluster boundaries of the text (Intl.Segmenter), or its code points where there is none.
        public static boundaries(text: string): number[] {
            const result = [0];
            const segmenter = (Intl as any).Segmenter;
            if (typeof segmenter === 'function') {
                for (const segment of new segmenter(undefined, { granularity: 'grapheme' }).segment(text)) {
                    if (segment.index > 0) result.push(segment.index);
                }
            } else {
                for (let i = 0; i < text.length;) {
                    i = BitVirtualKeyboardEngine.nextCodePoint(text, i);
                    if (i < text.length) result.push(i);
                }
            }
            result.push(text.length);
            return result;
        }

        private static segmenter: any;

        // The grapheme segments of the text, which find the cluster around an index without walking the rest of the text;
        // null where there is no Intl.Segmenter.
        private static segments(text: string): any {
            if (BitVirtualKeyboardEngine.segmenter === undefined) {
                const segmenter = (Intl as any).Segmenter;
                BitVirtualKeyboardEngine.segmenter = typeof segmenter === 'function' ? new segmenter(undefined, { granularity: 'grapheme' }) : null;
            }
            return BitVirtualKeyboardEngine.segmenter ? BitVirtualKeyboardEngine.segmenter.segment(text) : null;
        }

        // The last grapheme cluster boundary before the index (the code point before it where there is no Intl.Segmenter).
        public static previousGraphemeStart(text: string, index: number): number {
            if (index <= 0) return 0;
            if (index > text.length) return text.length;

            const segments = BitVirtualKeyboardEngine.segments(text);
            return segments ? segments.containing(index - 1).index : BitVirtualKeyboardEngine.previousCodePoint(text, index);
        }

        // The first grapheme cluster boundary after the index (the code point after it where there is no Intl.Segmenter).
        public static nextGraphemeEnd(text: string, index: number): number {
            if (index >= text.length) return text.length;

            const segments = BitVirtualKeyboardEngine.segments(text);
            if (!segments) return BitVirtualKeyboardEngine.nextCodePoint(text, Math.max(0, index));

            const segment = segments.containing(Math.max(0, index));
            return segment.index + segment.segment.length;
        }

        // The start of the line of a textarea's text the index is on.
        public static lineStart(text: string, index: number): number {
            return index <= 0 ? 0 : text.lastIndexOf('\n', index - 1) + 1;
        }

        public static graphemes(text: string): string[] {
            const bounds = BitVirtualKeyboardEngine.boundaries(text);
            const result: string[] = [];
            for (let i = 1; i < bounds.length; i++) {
                if (bounds[i] > bounds[i - 1]) result.push(text.slice(bounds[i - 1], bounds[i]));
            }
            return result;
        }

        // An emoji sequence: a pictograph (with its modifiers or ZWJ joins), a flag (a pair of regional indicators), or a
        // keycap.
        public static isEmojiSequence(cluster: string): boolean {
            return /\p{Extended_Pictographic}|\p{Regional_Indicator}|\u{20E3}/u.test(cluster);
        }

        // Whether every grapheme of the text matches the character class body (an empty or invalid body allows all).
        public static isAllowed(text: string, body: string | null | undefined): boolean {
            if (!body) return true;

            let pattern: RegExp;
            try {
                pattern = new RegExp('^[' + body + ']+$', 'u');
            } catch {
                return true;
            }

            return BitVirtualKeyboardEngine.graphemes(text).every(g => pattern.test(g));
        }

        // What a character types after a dead key: the table's entry for it, else the dead key's own (spacing) character
        // followed by it, as the operating systems do.
        public static resolveDeadKey(table: Record<string, string> | null | undefined, output: string): string {
            if (table) {
                if (Object.prototype.hasOwnProperty.call(table, output)) return table[output];
                return (table[' '] ?? '') + output;
            }
            return output;
        }

        // What a dead key types on its own (a space after it, or another dead key after it).
        public static deadKeyAlone(table: Record<string, string> | null | undefined): string {
            return table?.[' '] ?? '';
        }

        public static nextMultiTap(count: number, current: number): number {
            return count <= 0 ? 0 : (current + 1) % count;
        }

        // A tap of Shift: off goes to latched, a second tap within doubleTapMs locks, any tap of a locked Shift turns it off.
        public static tapShift(state: BitVirtualKeyboardShiftState, now: number): BitVirtualKeyboardShiftState {
            if (state.mode === 'locked') return { mode: 'off', lastTap: now };
            if (state.mode === 'latched') {
                return now - state.lastTap <= BitVirtualKeyboardEngine.doubleTapMs
                    ? { mode: 'locked', lastTap: now }
                    : { mode: 'off', lastTap: now };
            }
            return { mode: 'latched', lastTap: now };
        }

        // After a character is typed: a latched Shift lets go, a locked one stays.
        public static afterTyping(state: BitVirtualKeyboardShiftState): BitVirtualKeyboardShiftState {
            return state.mode === 'latched' ? { mode: 'off', lastTap: state.lastTap } : state;
        }

        // Whether a type=number input takes the text as its value (anything else, "1." included, it sanitizes to empty).
        public static isNumber(text: string): boolean {
            return /^-?(\d+(\.\d+)?|\.\d+)([eE][-+]?\d+)?$/.test(text);
        }

        // Whether a character can be part of what a type=number input holds.
        public static isNumberCharacter(text: string): boolean {
            return /^[0-9.eE+-]+$/.test(text);
        }
    }
}