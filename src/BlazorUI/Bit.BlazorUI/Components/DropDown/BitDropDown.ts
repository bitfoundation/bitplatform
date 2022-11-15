class BitDropDown {
    static toggleDropDownCallout(dotnetObjReference: DotNetObject,
        dropDownWrapperId: string,
        dropDownId: string,
        dropDownCalloutId: string,
        dropDownOverlayId: string,
        scrollWrapper: HTMLElement,
        dropDirection: BitDropDirection,
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
            if (isResponsiveModeEnabled && dropDownWrapperWidth < 320 && window.innerWidth < 640)
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

            //clear last style
            dropDownCallout.style.top = "";
            dropDownCallout.style.left = "";
            dropDownCallout.style.right = "";
            dropDownCallout.style.bottom = "";
            dropDownCallout.style.height = "";
            dropDownCallout.style.maxHeight = "";
            scrollWrapper.style.height = "";
            scrollWrapper.style.maxHeight = "";

            if (window.innerWidth < minimumWidthForDropDownNormalOpen && isResponsiveModeEnabled) {
                dropDownCallout.style.top = "0";
                dropDownCallout.style.right = "0";
                dropDownCallout.style.maxHeight = window.innerHeight + "px";
                scrollWrapper.style.maxHeight = (window.innerHeight - scrollWrapper.getBoundingClientRect().y) + "px";
                scrollWrapper.style.height = scrollWrapper.style.maxHeight;
            } else if (dropDirection == BitDropDirection.TopAndBottom) {
                dropDownCallout.style.left = dropDownWrapperX + "px";

                if (dropDownCalloutHeight <= dropDownWrapperBottom || dropDownWrapperBottom >= dropDownTop) {
                    dropDownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                    dropDownCallout.style.maxHeight = (dropDownWrapperBottom - 10) + "px";
                }
                else {
                    dropDownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                    dropDownCallout.style.maxHeight = (dropDownTop - 10) + "px";
                }
            } else {
                if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                    dropDownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                    dropDownCallout.style.left = dropDownWrapperX + "px";
                    dropDownCallout.style.maxHeight = (dropDownWrapperBottom - 10) + "px";
                } else if (dropDownTop >= dropDownCalloutHeight) {
                    dropDownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                    dropDownCallout.style.left = dropDownWrapperX + "px";
                    dropDownCallout.style.maxHeight = (dropDownTop - 10) + "px";
                } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                    dropDownCallout.style.left = dropDownWrapperX + dropDownWrapperWidth + 1 + "px";
                    dropDownCallout.style.bottom = "2px";
                    dropDownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                } else {
                    dropDownCallout.style.left = dropDownWrapperX - dropDownCalloutWidth - 1 + "px";
                    dropDownCallout.style.bottom = "2px";
                    dropDownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                }
            }
        }
    }
}

enum BitDropDirection {
    Auto,
    TopAndBottom
}