export class EditorToolbar {
    static "__#4@#l10nRemove": null;
    static "__#4@#pointerDown"(e: any): void;
    constructor(editor: any);
    render(): HTMLDivElement;
    get div(): null;
    hide(): void;
    show(): void;
    addAltText(altText: any): Promise<void>;
    addColorPicker(colorPicker: any): void;
    remove(): void;
    #private;
}
export class HighlightToolbar {
    constructor(uiManager: any);
    show(parent: any, boxes: any, isLTR: any): void;
    hide(): void;
    #private;
}
