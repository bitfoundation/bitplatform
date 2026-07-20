declare class ColorAttributor extends StyleAttributor {
    value(domNode: HTMLElement): string;
}

declare const ColorClass: ClassAttributor;
declare const ColorStyle: ColorAttributor;
