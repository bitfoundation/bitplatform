class BitDropDown {
    static toggleDropDownCallout(dotnetObjReference: DotNetObject,
        dropDownWrapperId: string,
        dropDownId: string,
        dropDownCalloutId: string,
        dropDownOverlayId: string,
        isOpen: boolean,
        isResponsiveModeEnabled: boolean) {

        const dropDownWrapper = document.getElementById(dropDownWrapperId);
        if (dropDownWrapper == null)
            return;

        const dropDown = document.getElementById(dropDownId);
        if (dropDown == null)
            return;

        const dropDownCallout = document.getElementById(dropDownCalloutId);
        if (dropDownCallout == null)
            return;

        const dropDownOverlay = document.getElementById(dropDownOverlayId);
        if (dropDownOverlay == null)
            return;

        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropDownCalloutId = "";
        } else {
            Bit.currentDropDownCalloutId = dropDownCalloutId;
            Bit.currentDropDownCalloutResponsiveModeIsEnabled = isResponsiveModeEnabled;
            Bit.closeCurrentCalloutIfExists(dropDownCalloutId, dropDownOverlayId, dotnetObjReference);
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";

            let dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            if (isResponsiveModeEnabled && dropDownWrapperWidth < 320)
                dropDownWrapperWidth = window.innerWidth > 320 ? 320 : window.innerWidth;

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
            const minimumWidthForDropDownNormalOpen = 640;

            if (window.innerWidth < minimumWidthForDropDownNormalOpen && isResponsiveModeEnabled) {
                dropDownCallout.style.top = "0";
                dropDownCallout.style.left = "unset";
                dropDownCallout.style.right = "0";
                dropDownCallout.style.bottom = "unset";
            } else if (dropDownWrapperBottom >= dropDownCalloutHeight) {
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