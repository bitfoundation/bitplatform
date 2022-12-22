class BitBreadList {
    static toggleOverflowCallout(
        dotnetObjReference: DotNetObject,
        dropDownWrapperId: string,
        dropDownId: string,
        calloutId: string,
        overlayId: string,
        isCalloutOpen: boolean) {

        const dropDownWrapper = document.getElementById(dropDownWrapperId);
        if (dropDownWrapper == null)
            return;

        const dropDown = document.getElementById(dropDownId);
        if (dropDown == null)
            return;

        const dropDownCallout = document.getElementById(calloutId);
        if (dropDownCallout == null)
            return;

        const dropDownOverlay = document.getElementById(overlayId);
        if (dropDownOverlay == null)
            return;

        if (isCalloutOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropDownCalloutId = "";
        } else {
            Bit.currentDropDownCalloutId = calloutId;
            Bit.closeCurrentCalloutIfExists(calloutId, overlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";

            const dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            const dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            const dropDownX = dropDown.getBoundingClientRect().x;

            dropDownCallout.style.maxWidth = dropDownWrapperWidth + dropDownWrapperX - dropDownX + "px";

            const dropDownCalloutHeight = dropDownCallout.offsetHeight;
            const dropDownCalloutWidth = dropDownCallout.offsetWidth;
            const dropDownTop = dropDown.getBoundingClientRect().y;
            const dropDownHeight = dropDown.offsetHeight;
            const dropDownWidth = dropDown.offsetWidth;
            const dropDownY = dropDown.getBoundingClientRect().y;
            const dropDownBottom = window.innerHeight - (dropDownHeight + dropDownY);
            const dropDownRight = window.innerWidth - (dropDownWidth + dropDownX);

            if (dropDownBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownY + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            }
            else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.bottom = dropDownBottom + dropDownHeight + 1 + "px";
                dropDownCallout.style.left = dropDownX + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else if (dropDownRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownX + dropDownWidth + 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            }
            else {
                dropDownCallout.style.left = dropDownX - dropDownCalloutWidth - 1 + "px";
                dropDownCallout.style.bottom = "2px";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.right = "unset";
            }
        }
    }
}