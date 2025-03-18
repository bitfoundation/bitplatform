namespace BitBlazorUI {
    type Content = {
        value: string;
        type: "inline" | "block" | "wrap";
    };

    export class MarkdownEditor {
        private static _editors: { [key: string]: MarkdownEditor } = {};

        public static init(id: string, textArea: HTMLTextAreaElement) {
            const editor = new MarkdownEditor(textArea);

            MarkdownEditor._editors[id] = editor;
        }

        public static getValue(id: string) {
            const editor = MarkdownEditor._editors[id];
            if (!editor) return;

            return editor.value;
        }

        #opens: string[] = [];

        #pairs: { [key: string]: string } = {
            "(": ")",
            "{": "}",
            "[": "]",
            "<": ">",
            '"': '"',
            "`": "`",
        };

        #textArea: HTMLTextAreaElement;

        constructor(textArea: HTMLTextAreaElement) {
            this.#textArea = textArea;

            this.#init();
        }

        get value() {
            return this.#textArea.value;
        }

        set value(value) {
            this.#textArea.value = value;
        }

        get #block() {
            const codeBlocks = this.value.split("```");
            let total = 0;
            for (const [i, b] of codeBlocks.entries()) {
                total += b.length + 3;
                if (this.#start < total) {
                    return i;
                }
            }
            return 0;
        }

        get #end() {
            return this.#textArea.selectionEnd;
        }

        get #start() {
            return this.#textArea.selectionStart;
        }

        #set(start: number, end: number) {
            this.#textArea.setSelectionRange(start, end);
        }

        #getLine() {
            const total = this.value.split("\n");
            let count = 0;
            for (let i = 0; i < total.length; i++) {
                const length = total.at(i)?.length ?? 0;
                count++;
                count += length;
                if (count > this.#end) {
                    return {
                        total,
                        num: i,
                        col: this.#end - (count - length - 1),
                    };
                }
            }
            return { total, num: 0, col: 0 };
        }

        #insert(
            element: Content,
            start: number,
            end: number,
        ) {
            if (element.type === "inline") {
                this.value = `${this.value.slice(0, end)}${element.value}${this.value.slice(end)}`;
            } else if (element.type === "wrap") {
                this.value = insert(this.value, element.value, start);
                this.value = insert(
                    this.value,
                    this.#pairs[element.value] as string,
                    end + element.value.length,
                );
                if (element.value.length < 2) this.#opens.push(element.value);
            } else if (element.type === "block") {
                const { total, num } = this.#getLine();
                const first = element.value.at(0);
                if (first && total[num]?.startsWith(first)) {
                    total[num] = element.value.trim() + total[num];
                } else {
                    total[num] = element.value + total[num];
                }
                this.value = total.join("\n");
            }
        }

        #setCaret(
            text: string,
            start: number,
            end: number,
        ) {
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

            this.#set(startPos, endPos);
            this.#textArea.focus();
        }

        #add(element: Content) {
            const end = this.#end;
            const start = this.#start;

            this.#insert(element, start, end);
            this.#setCaret(element.value, start, end);
        }

        #getLists(str: string | undefined) {
            if (!str) return;

            if (startsWithDash(str)) {
                return '- ';
            }

            const listNum = startsWithNumber(str);
            if (listNum) return `${listNum}. `;
        }

        #correct(cur: number, isDec = false) {
            const { total } = this.#getLine();
            for (let i = cur + 1; i < total.length; i++) {
                const l = total[i];
                if (!l) continue;

                if (startsWithDash(l)) {
                    if (l.length > 2) {
                        total[i] = l;
                    } else {
                        continue;
                    }
                } else {
                    const number = startsWithNumber(l);
                    if (!number) {
                        break;
                    } else {
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
                }
            }
            this.value = total.join("\n");
        }

        #init() {
            this.#textArea.addEventListener("keydown", async (e) => {
                const reseters = ["Delete", "ArrowUp", "ArrowDown"];
                const next = this.value[this.#end] ?? "";
                if (reseters.includes(e.key)) {
                    this.#opens = [];
                } else if (e.key === "Backspace") {
                    const prev = this.value[this.#start - 1];
                    if (
                        prev &&
                        prev in this.#pairs &&
                        next === this.#pairs[prev]
                    ) {
                        e.preventDefault();
                        const start = this.#start - 1;
                        const end = this.#end - 1;
                        this.value = remove(this.value, start);
                        this.value = remove(this.value, end);
                        setTimeout(() => {
                            this.#set(start, end);
                        }, 0);
                        this.#opens.pop();
                    }
                    if (prev === "\n" && this.#start === this.#end) {
                        e.preventDefault();
                        const pos = this.#start - 1;
                        const { num } = this.#getLine();
                        this.#correct(num, true);
                        this.value = remove(this.value, pos);
                        setTimeout(async () => {
                            this.#set(pos, pos);
                        }, 0);
                    }
                } else if (e.key === "Tab") {
                    if (this.#block % 2 !== 0) {
                        e.preventDefault();
                        this.#add({
                            type: "inline",
                            value: "\t",
                        });
                    }
                } else if (e.key === "Enter") {
                    const { total, num, col } = this.#getLine();
                    const line = total.at(num);
                    let rep = this.#getLists(line);
                    const orig = rep;

                    const n = startsWithNumber(rep);
                    if (n) rep = `${n + 1}. `;

                    if (rep && (orig && orig.length < col)) {
                        e.preventDefault();
                        if (n) this.#correct(num);
                        this.#add({
                            type: "inline",
                            value: `\n${rep}`,
                        });
                    } else if (rep && (orig && orig.length === col)) {
                        e.preventDefault();

                        const origEnd = this.#end;
                        const pos = origEnd - orig.length;

                        for (let i = 0; i < orig.length; i++) {
                            this.value = remove(this.value, origEnd - (i + 1));
                        }

                        setTimeout(async () => {
                            this.#set(pos, pos);
                            this.#textArea.focus();
                            this.#add({
                                type: "inline",
                                value: `\n`,
                            });
                        }, 0);
                    }
                } else {
                    const nextIsPaired = Object.values(this.#pairs).includes(next);
                    const isSelected = this.#start !== this.#end;
                    if (e.ctrlKey || e.metaKey) {
                        if (this.#start === this.#end) {
                            if (e.key === "c" || e.key === "x") {
                                e.preventDefault();
                                const { total, num, col } = this.#getLine();

                                await navigator.clipboard.writeText(`${num === 0 && e.key === "x" ? "" : "\n"}${total[num]}`);

                                if (e.key === "x") {
                                    const pos = this.#start - col;
                                    total.splice(num, 1);
                                    this.value = total.join("\n");
                                    setTimeout(() => this.#set(pos, pos), 0);
                                }
                            }
                        }
                    }

                    if ((e.ctrlKey || e.metaKey) && e.key) {
                        // TODO: handle shortkeys for example
                    } else if (
                        nextIsPaired &&
                        (next === e.key || e.key === "ArrowRight") &&
                        this.#opens.length &&
                        !isSelected
                    ) {
                        e.preventDefault();
                        this.#set(this.#start + 1, this.#end + 1);
                        this.#opens.pop();
                    } else if (e.key in this.#pairs) {
                        e.preventDefault();
                        this.#add({
                            type: "wrap",
                            value: e.key,
                        });
                        this.#opens.push(e.key);
                    }
                }
            });

            this.#textArea.addEventListener("dblclick", () => {
                if (this.#start !== this.#end) {
                    if (this.value[this.#start] === " ") {
                        this.#set(this.#start + 1, this.#end);
                    }
                    if (this.value[this.#end - 1] === " ") {
                        this.#set(this.#start, this.#end - 1);
                    }
                }
            });

            this.#textArea.addEventListener("click", () => (this.#opens = []));
        }
    }

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