declare class CodeBlockContainer extends Container {
    static create(value: string): Element;
    code(index: number, length: number): string;
    html(index: number, length: number): string;
}

declare class CodeBlock extends Block {
    static TAB: string;
    static register(): void;
}

declare class Code extends Inline {
}
