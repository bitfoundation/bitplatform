declare class QuillColorPicker extends Picker {
    constructor(select: HTMLSelectElement, label: string);
    buildItem(option: HTMLOptionElement): HTMLSpanElement;
    selectItem(item: HTMLElement | null, trigger?: boolean): void;
}
