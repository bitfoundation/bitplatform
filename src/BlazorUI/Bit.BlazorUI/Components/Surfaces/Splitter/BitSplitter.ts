namespace BitBlazorUI {
    export class Splitter {
        public static handleSplitterDragging(event: TouchEvent) {
            document.body.style.overscrollBehavior = 'none';
        };

        public static handleSplitterDraggingEnd() {
            document.body.style.overscrollBehavior = '';
        };

        public static getSplitterWidth(element: HTMLElement) {
            return element.getBoundingClientRect().width;
        };

        public static setSplitterWidth(element: HTMLElement, width: number) {
            element.style.width = width + 'px';
        };

        public static getSplitterHeight(element: HTMLElement) {
            return element.getBoundingClientRect().height;
        };

        public static setSplitterHeight(element: HTMLElement, height: number) {
            element.style.height = height + 'px';
        };

        public static resetPaneDimensions(element: HTMLElement) {
            element.style.width = '';
            element.style.height = '';
        };
    }
}