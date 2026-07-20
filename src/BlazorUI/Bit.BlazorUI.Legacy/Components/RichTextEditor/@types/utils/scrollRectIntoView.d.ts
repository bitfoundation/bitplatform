declare type Rect = {
    top: number;
    right: number;
    bottom: number;
    left: number;
};
declare const scrollRectIntoView: (root: HTMLElement, targetRect: Rect) => void;
