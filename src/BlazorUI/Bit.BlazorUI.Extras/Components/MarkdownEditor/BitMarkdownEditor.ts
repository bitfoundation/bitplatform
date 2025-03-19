namespace BitBlazorUI {

    export class MarkdownEditor {
        private static _editors: { [key: string]: Editor } = {};

        public static init(id: string, textArea: HTMLTextAreaElement, dotnetObj?: DotNetObject, defaultValue?: string) {
            const editor = new Editor(textArea, dotnetObj);

            if (defaultValue) {
                editor.value = defaultValue;
            }

            MarkdownEditor._editors[id] = editor;
        }

        public static getValue(id: string) {
            const editor = MarkdownEditor._editors[id];
            if (!editor) return;

            return editor.value;
        }

        public static dispose(id: string) {
            if (!MarkdownEditor._editors[id]) return;

            MarkdownEditor._editors[id].dispose();

            delete MarkdownEditor._editors[id];
        }
    }

    class Editor {
        _opens: string[] = [];
        _pairs: { [key: string]: string } = {
            '(': ')',
            '{': '}',
            '[': ']',
            '<': '>',
            '"': '"',
            '`': '`',
            '*': '*',
            '**': '**',
        };

        private textArea: HTMLTextAreaElement;
        private dotnetObj: DotNetObject | undefined | null;

        private clickHandler: (e: MouseEvent) => void;
        private changeHandler: (e: Event) => void;
        private dblClickHandler: (e: MouseEvent) => void;
        private keydownHandler: (e: KeyboardEvent) => Promise<void>;

        constructor(textArea: HTMLTextAreaElement, dotnetObj?: DotNetObject) {
            this.textArea = textArea;
            this.dotnetObj = dotnetObj;

            this.clickHandler = this.click.bind(this);
            this.changeHandler = this.change.bind(this);
            this.dblClickHandler = this.dblClick.bind(this);
            this.keydownHandler = this.keydown.bind(this);

            this.textArea.addEventListener('click', this.clickHandler);
            this.textArea.addEventListener('change', this.changeHandler);
            this.textArea.addEventListener('input', this.changeHandler);
            this.textArea.addEventListener('dblclick', this.dblClickHandler);
            this.textArea.addEventListener('keydown', this.keydownHandler);
        }

        get value() {
            return this.textArea.value;
        }

        set value(value) {
            this.textArea.value = value;
        }

        get block() {
            const codeBlocks = this.value.split('```');
            let total = 0;
            for (const [i, b] of codeBlocks.entries()) {
                total += b.length + 3;
                if (this.start < total) {
                    return i;
                }
            }
            return 0;
        }

        get end() {
            return this.textArea.selectionEnd;
        }

        get start() {
            return this.textArea.selectionStart;
        }

        set(start: number, end: number) {
            this.textArea.setSelectionRange(start, end);
        }

        getLine() {
            const total = this.value.split('\n');
            let count = 0;
            for (let i = 0; i < total.length; i++) {
                const length = total.at(i)?.length ?? 0;
                count++;
                count += length;
                if (count > this.end) {
                    return {
                        total,
                        num: i,
                        col: this.end - (count - length - 1),
                    };
                }
            }
            return { total, num: 0, col: 0 };
        }

        insert(content: Content, start: number, end: number) {
            if (content.type === 'inline') {
                this.value = `${this.value.slice(0, end)}${content.value}${this.value.slice(end)}`;
            } else if (content.type === 'wrap') {
                this.value = insert(this.value, content.value, start);
                this.value = insert(
                    this.value,
                    this._pairs[content.value] as string,
                    end + content.value.length,
                );
                if (content.value.length < 2) this._opens.push(content.value);
            } else if (content.type === 'block') {
                const { total, num } = this.getLine();
                const first = content.value.at(0);
                if (first && total[num]?.startsWith(first)) {
                    total[num] = content.value.trim() + total[num];
                } else {
                    total[num] = content.value + total[num];
                }
                this.value = total.join('\n');
            } else if (content.type === 'init') { }
        }

        setCaret(text: string, start: number, end: number) {
            let startPos = 0;
            let endPos = 0;
            if (/[a-z]/i.test(text)) {
                for (let i = end; i < this.value.length; i++) {
                    if (this.value[i]?.match(/[a-z]/i)) {
                        if (!startPos) {
                            startPos = i;
                        } else {
                            endPos = i + 1;
                        }
                    } else if (startPos) {
                        break;
                    }
                }
            } else {
                startPos = start + text.length;
                endPos = end + text.length;
            }

            this.set(startPos, endPos);
            this.textArea.focus();
        }

        add(content: Content, s?: number, e?: number) {
            const end = s || this.end;
            const start = e || this.start;

            this.insert(content, start, end);
            this.setCaret(content.value, start, end);
        }

        getRepeat(str: string | undefined) {
            if (!str) return;

            if (startsWithDash(str)) {
                return '- ';
            }

            const n = startsWithNumber(str);
            if (n) return `${n}. `;
        }

        correct(cur: number, isDec = false) {
            const { total } = this.getLine();
            for (let i = cur + 1; i < total.length; i++) {
                const l = total[i];
                if (!l) continue;

                const number = startsWithNumber(l);
                if (!number) break;

                let newNumber: number;
                if (isDec) {
                    if (number > 1) {
                        newNumber = number - 1;
                    } else {
                        break;
                    }
                } else {
                    newNumber = number + 1;
                }
                total[i] = l.slice(String(number).length);
                total[i] = String(newNumber) + total[i];
            }
            this.value = total.join('\n');
        }

        dispose() {
            this.textArea.removeEventListener('click', this.clickHandler);
            this.textArea.removeEventListener('change', this.changeHandler);
            this.textArea.removeEventListener('input', this.changeHandler);
            this.textArea.removeEventListener('dblclick', this.dblClickHandler);
            this.textArea.removeEventListener('keydown', this.keydownHandler);

            this.dotnetObj = undefined;
        }

        // ==========================================================

        async keydown(e: KeyboardEvent) {
            const reseters = ['Delete', 'ArrowUp', 'ArrowDown'];
            const next = this.value[this.end] ?? '';
            if (reseters.includes(e.key)) {
                this._opens = [];
            } else if (e.key === 'Backspace') {
                const prev = this.value[this.start - 1];
                if (
                    prev &&
                    prev in this._pairs &&
                    next === this._pairs[prev]
                ) {
                    e.preventDefault();
                    const start = this.start - 1;
                    const end = this.end - 1;
                    this.value = remove(this.value, start);
                    this.value = remove(this.value, end);
                    setTimeout(() => {
                        this.set(start, end);
                    }, 0);
                    this._opens.pop();
                }
                if (prev === '\n' && this.start === this.end) {
                    e.preventDefault();
                    const pos = this.start - 1;
                    const { num } = this.getLine();
                    this.correct(num, true);
                    this.value = remove(this.value, pos);
                    setTimeout(async () => {
                        this.set(pos, pos);
                    }, 0);
                }
            } else if (e.key === 'Tab') {
                if (this.block % 2 !== 0) {
                    e.preventDefault();
                    this.add({ type: 'inline', value: '\t' });
                }
            } else if (e.key === 'Enter') {
                const { total, num, col } = this.getLine();
                const line = total.at(num);
                let rep = this.getRepeat(line);
                const orig = rep;

                const n = startsWithNumber(rep);
                if (n) rep = `${n + 1}. `;

                if (rep && (orig && orig.length < col)) {
                    e.preventDefault();
                    const ss = this.start;
                    const ee = this.end;

                    if (n) this.correct(num);
                    this.add({ type: 'inline', value: `\n${rep}` }, ss, ee);
                } else if (rep && (orig && orig.length === col)) {
                    e.preventDefault();

                    const origEnd = this.end;
                    const pos = origEnd - orig.length;

                    for (let i = 0; i < orig.length; i++) {
                        this.value = remove(this.value, origEnd - (i + 1));
                    }

                    setTimeout(async () => {
                        this.set(pos, pos);
                        this.textArea.focus();
                        this.add({ type: 'inline', value: `\n` });
                    }, 0);
                }
            } else {
                const nextIsPaired = Object.values(this._pairs).includes(next);
                const isSelected = this.start !== this.end;
                if (e.ctrlKey || e.metaKey) {
                    if (this.start === this.end) {
                        if (e.key === 'c' || e.key === 'x') {
                            e.preventDefault();
                            const { total, num, col } = this.getLine();

                            await navigator.clipboard.writeText(`${num === 0 && e.key === 'x' ? '' : '\n'}${total[num]}`);

                            if (e.key === 'x') {
                                const pos = this.start - col;
                                total.splice(num, 1);
                                this.value = total.join('\n');
                                setTimeout(() => this.set(pos, pos), 0);
                            }
                        }
                    }
                }

                if ((e.ctrlKey || e.metaKey) && e.key) {
                    let content: Content | undefined;

                    if (e.key === 'h') {
                        content = { type: 'block', value: '# ' };
                    }
                    if (e.key === 'b') {
                        content = { type: 'wrap', value: '**' };
                    }
                    if (e.key === 'i') {
                        content = { type: 'wrap', value: '*' };
                    }

                    if (content) {
                        this.add(content);
                        e.preventDefault();
                    }

                } else if (
                    nextIsPaired &&
                    (next === e.key || e.key === 'ArrowRight') &&
                    this._opens.length &&
                    !isSelected
                ) {
                    e.preventDefault();
                    this.set(this.start + 1, this.end + 1);
                    this._opens.pop();
                } else if (e.key in this._pairs) {
                    e.preventDefault();
                    this.add({ type: 'wrap', value: e.key });
                    this._opens.push(e.key);
                }
            }
        }

        dblClick(e: MouseEvent) {
            if (this.start !== this.end) {
                if (this.value[this.start] === ' ') {
                    this.set(this.start + 1, this.end);
                }
                if (this.value[this.end - 1] === ' ') {
                    this.set(this.start, this.end - 1);
                }
            }
        }

        click(e: MouseEvent) {
            this._opens = [];
        }

        change(e: Event) {
            this.dotnetObj?.invokeMethodAsync('OnChange', this.value);
        }
    }

    type Content = {
        value: string;
        type: 'inline' | 'block' | 'wrap' | 'init';
    };

    const startsWithDash = (str: string | undefined) => {
        return !!(str?.startsWith('- '));
    };

    const startsWithNumber = (str: string | undefined) => {
        const result = str?.match(/^(\d+)\./);
        return result ? Number(result[1]) : null;
    };

    const insert = (str: string, char: string, index: number) => {
        return str.slice(0, index) + char + str.slice(index);
    };

    const remove = (str: string, index: number) => {
        return str.slice(0, index) + str.slice(index + 1);
    };
}