class BitDropDown {
    static registerOnDocumentClickEvent(dotnetHelper: any, componentId: string, callback: string): void {
        document.addEventListener('click', e => {
            var element = e.target as Element;
            if (element.id == componentId ||
                element.parentElement?.parentElement?.id == componentId) {
                e.stopPropagation();
            } else {
                dotnetHelper.invokeMethodAsync(callback);
            }
        });
    }
}