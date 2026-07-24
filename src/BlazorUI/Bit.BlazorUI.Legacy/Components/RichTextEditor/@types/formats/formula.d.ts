declare class Formula extends Embed {
    static blotName: string;
    static className: string;
    static tagName: string;
    static create(value: string): Element;
    static value(domNode: Element): string | null;
    html(): string;
}
