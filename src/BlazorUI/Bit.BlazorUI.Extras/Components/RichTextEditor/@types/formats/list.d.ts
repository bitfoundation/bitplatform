declare class ListContainer extends Container {
}

declare class ListItem extends Block {
    static create(value: string): HTMLElement;
    static formats(domNode: HTMLElement): string | undefined;
    static register(): void;
    constructor(scroll: Scroll, domNode: HTMLElement);
    format(name: string, value: string): void;
}
