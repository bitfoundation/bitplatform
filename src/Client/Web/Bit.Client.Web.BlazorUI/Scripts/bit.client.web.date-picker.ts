class BitDatePicker {
    static registerOnDocumentClickEvent(dotnetHelper: any, componentId: string, callback: string): void {
        document.addEventListener('click', e => {
            var element = e.target as Element;
            var datePickerRoot = element.parentElement?.parentElement?.parentElement?.parentElement as Element;
            if (element.classList.contains('bit-dtp-cal') ||
                element.parentElement?.classList.contains('bit-dtp-cal') ||
                (datePickerRoot != null &&
                datePickerRoot.id == componentId)) {
                e.stopPropagation();
            } else {
                dotnetHelper.invokeMethodAsync(callback);
            }
        });
    }
}
