class BitDropDown {
    static registerOnDocumentClickEvent(dotnetHelper: any, componentId: string, callback: string): void {

        function checkIfElementOrItsParentsHasTheSpecifiedId(element: Element, id: string): boolean {
            if (!element)
                return false;

            if (element.id === id)
                return true;

            return checkIfElementOrItsParentsHasTheSpecifiedId(element.parentNode as Element, id);
        }
        
        document.addEventListener("click", e => {
            var element = e.target as Element;
            if (checkIfElementOrItsParentsHasTheSpecifiedId(element, componentId)) {
                e.stopPropagation();
            } else {
                dotnetHelper.invokeMethodAsync(callback);
            }
        });
    }
}