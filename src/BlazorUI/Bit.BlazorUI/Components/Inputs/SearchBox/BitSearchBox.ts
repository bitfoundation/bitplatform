class BitSearchBox {
    static moveCursorToEnd(inputElement: HTMLInputElement) {
        inputElement.selectionStart = inputElement.selectionEnd = inputElement.value.length;
    }
}
