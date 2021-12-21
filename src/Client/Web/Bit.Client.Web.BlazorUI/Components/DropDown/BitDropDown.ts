class BitDropDown {
    static toggleDropDownCallout(dotnetObjReference: any,
        dropDownWrapperId: string,
        dropDownId: string,
        dropDownCalloutId: string,
        dropDownOverlayId: string,
        isOpen: boolean) {
        const dropDownWrapper = document.getElementById(dropDownWrapperId) ?? new HTMLElement();
        const dropDown = document.getElementById(dropDownId) ?? new HTMLElement();
        const dropDownCallout = document.getElementById(dropDownCalloutId) ?? new HTMLElement();
        const dropDownOverlay = document.getElementById(dropDownOverlayId) ?? new HTMLElement();

        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropDownCalloutId = "";
        } else {
            Bit.currentDropDownCalloutId = dropDownCalloutId;
            Bit.closeCurrentCalloutIfExists(dropDownCalloutId, dropDownOverlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
            var dropDownCalloutHeight = dropDownCallout.offsetHeight;
            var dropDownCalloutWidth = dropDownCallout.offsetWidth;
            var dropDownHeight = dropDown.offsetHeight;
            var dropDownTop = dropDown.getBoundingClientRect().y;
            var dropDownWrapperHeight = dropDownWrapper.offsetHeight;
            var dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            var dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            var dropDownWrapperY = dropDownWrapper.getBoundingClientRect().y;
            var dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            var dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);

            if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                dropDownCallout.style.left = dropDownWrapperX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            } else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownWrapperX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownWrapperX + dropDownWrapperWidth + 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            } else {
                dropDownCallout.style.left = dropDownWrapperX - dropDownCalloutWidth - 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.right = "unset";
            }
        }
    }
}