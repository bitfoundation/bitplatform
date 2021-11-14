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
        } else {
            Bit.closeCurrentCalloutIfExists(dropDownCalloutId, dropDownOverlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
            var dropDownCalloutHeight = dropDownCallout.offsetHeight;
            var dropDownCalloutWidth = dropDownCallout.offsetWidth;
            var dropDownWrapperHeight = dropDownWrapper.offsetHeight;
            var dropDownHeight = dropDown.offsetHeight;
            var dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            var dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            var dropDownWrapperY = dropDownWrapper.getBoundingClientRect().y;
            var dropDownTop = dropDown.getBoundingClientRect().y;
            var dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            var dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);

            if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownWrapperHeight + 1 + "px";
                dropDownCallout.style.left = "0px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            } else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.bottom = dropDownHeight + 1 + "px";
                dropDownCallout.style.left = "0px";
            } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownWrapperWidth + 1 + "px";
                dropDownCallout.style.bottom = 0 - dropDownWrapperBottom + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            } else {
                dropDownCallout.style.left = "unset";
                dropDownCallout.style.top = "unset"
                dropDownCallout.style.right = dropDownWrapperWidth + "px";
                dropDownCallout.style.bottom = 0 - dropDownWrapperBottom + "px";
            }
        }
    }
}