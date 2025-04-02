declare class Tooltip {
    quill: Quill;
    boundsContainer: HTMLElement;
    root: HTMLDivElement;
    constructor(quill: Quill, boundsContainer?: HTMLElement);
    hide(): void;
    position(reference: Bounds): number;
    show(): void;
}
