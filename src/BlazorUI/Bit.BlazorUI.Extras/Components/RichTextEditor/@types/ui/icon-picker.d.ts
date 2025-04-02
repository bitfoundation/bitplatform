declare class IconPicker extends Picker {
    defaultItem: HTMLElement | null;
    constructor(select: HTMLSelectElement, icons: Record<string, string>);
    selectItem(target: HTMLElement | null, trigger?: boolean): void;
}
