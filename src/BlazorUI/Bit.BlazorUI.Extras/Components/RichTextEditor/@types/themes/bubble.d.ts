declare class BubbleTooltip extends BaseTooltip {
    static TEMPLATE: string;
    constructor(quill: Quill, bounds?: HTMLElement);
    listen(): void;
    cancel(): void;
    position(reference: Bounds): number;
}

declare class BubbleTheme extends BaseTheme {
    tooltip: BubbleTooltip;
    constructor(quill: Quill, options: ThemeOptions);
    extendToolbar(toolbar: Toolbar): void;
}
