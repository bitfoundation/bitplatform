declare class Cursor extends EmbedBlot {
    static blotName: string;
    static className: string;
    static tagName: string;
    static CONTENTS: string;
    static value(): undefined;
    selection: Selection;
    textNode: QuillText;
    savedLength: number;
    constructor(scroll: ScrollBlot, domNode: HTMLElement, selection: Selection);
    detach(): void;
    format(name: string, value: unknown): void;
    index(node: Node, offset: number): number;
    length(): number;
    position(): [Text, number];
    remove(): void;
    restore(): EmbedContextRange | null;
    update(mutations: MutationRecord[], context: Record<string, unknown>): void;
    optimize(context?: unknown): void;
    value(): string;
}
