declare class BaseTheme extends Theme {
    pickers: Picker[];
    tooltip?: Tooltip;
    constructor(quill: Quill, options: ThemeOptions);
    addModule(name: 'clipboard'): QuillClipboard;
    addModule(name: 'keyboard'): Keyboard;
    addModule(name: 'uploader'): Uploader;
    addModule(name: 'history'): QuillHistory;
    addModule(name: 'selection'): QuillSelection;
    addModule(name: string): unknown;
    buildButtons(buttons: NodeListOf<HTMLElement>, icons: Record<string, Record<string, string> | string>): void;
    buildPickers(selects: NodeListOf<HTMLSelectElement>, icons: Record<string, string | Record<string, string>>): void;
}

declare class BaseTooltip extends Tooltip {
    textbox: HTMLInputElement | null;
    linkRange?: QuillRange;
    constructor(quill: Quill, boundsContainer?: HTMLElement);
    listen(): void;
    cancel(): void;
    edit(mode?: string, preview?: string | null): void;
    restoreFocus(): void;
    save(): void;
}
