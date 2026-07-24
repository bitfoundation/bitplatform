declare class Video extends BlockEmbed {
    static blotName: string;
    static className: string;
    static tagName: string;
    static create(value: string): Element;
    static formats(domNode: Element): Record<string, string | null>;
    static sanitize(url: string): string;
    static value(domNode: Element): string | null;
    domNode: HTMLVideoElement;
    format(name: string, value: string): void;
    html(): string;
}
