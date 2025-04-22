declare class Script extends Inline {
    static blotName: string;
    static tagName: string[];
    static create(value: 'super' | 'sub' | (string & {})): HTMLElement;
    static formats(domNode: HTMLElement): "super" | "sub" | undefined;
}
