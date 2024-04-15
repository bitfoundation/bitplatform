namespace BitBlazorUI {
    export class SearchBox {
        public static moveCursorToEnd(inputElement: HTMLInputElement) {
            inputElement.selectionStart = inputElement.selectionEnd = inputElement.value.length;
        }
    }
}