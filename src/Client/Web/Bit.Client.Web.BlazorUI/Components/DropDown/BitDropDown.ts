class BitDropDown {
    static toggleDropDownCallout(dotnetObjReference: DotNetObject,
        dropDownWrapperId: string,
        dropDownId: string,
        dropDownCalloutId: string,
        dropDownOverlayId: string,
        isOpen: boolean) {

        const dropDownWrapper = document.getElementById(dropDownWrapperId);
        if (!(dropDownWrapper instanceof HTMLElement))
            return;

        const dropDown = document.getElementById(dropDownId);
        if (!(dropDown instanceof HTMLElement))
            return;

        const dropDownCallout = document.getElementById(dropDownCalloutId);
        if (!(dropDownCallout instanceof HTMLElement))
            return;

        const dropDownOverlay = document.getElementById(dropDownOverlayId);
        if (!(dropDownOverlay instanceof HTMLElement))
            return;

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

            const dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            dropDownCallout.style.width = dropDownWrapperWidth + 'px';

            const dropDownCalloutHeight = dropDownCallout.offsetHeight;
            const dropDownCalloutWidth = dropDownCallout.offsetWidth;
            const dropDownHeight = dropDown.offsetHeight;
            const dropDownTop = dropDown.getBoundingClientRect().y;
            const dropDownWrapperHeight = dropDownWrapper.offsetHeight;
            const dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            const dropDownWrapperY = dropDownWrapper.getBoundingClientRect().y;
            const dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            const dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);

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