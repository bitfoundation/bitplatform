class BitDropdown {
    static toggleDropdownCallout(dotnetObjReference: DotNetObject,
        dropdownWrapperId: string,
        dropdownId: string,
        dropdownCalloutId: string,
        dropdownOverlayId: string,
        scrollWrapper: HTMLElement,
        dropDirection: BitDropDirection,
        isOpen: boolean,
        isResponsiveModeEnabled: boolean,
        isRtl: boolean) {

        const dropdownWrapper = document.getElementById(dropdownWrapperId);
        if (dropdownWrapper == null) return;

        const dropdown = document.getElementById(dropdownId);
        if (dropdown == null) return;

        const dropdownCallout = document.getElementById(dropdownCalloutId);
        if (dropdownCallout == null) return;

        const dropdownOverlay = document.getElementById(dropdownOverlayId);
        if (dropdownOverlay == null) return;

        if (isOpen) {
            dropdownCallout.style.display = "none";
            dropdownOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
            Bit.currentDropdownCalloutId = "";
        } else {
            Bit.currentDropdownCalloutId = dropdownCalloutId;
            Bit.currentDropdownCalloutResponsiveModeIsEnabled = isResponsiveModeEnabled;
            Bit.closeCurrentCalloutIfExists(dropdownCalloutId, dropdownOverlayId, dotnetObjReference);
            dropdownCallout.style.display = "block";
            dropdownOverlay.style.display = "block";

            let dropDownWrapperWidth = dropdownWrapper.offsetWidth;
            if (isResponsiveModeEnabled && dropDownWrapperWidth < 320 && window.innerWidth < 640)
                dropDownWrapperWidth = window.innerWidth > 320 ? 320 : window.innerWidth;

            dropdownCallout.style.width = dropDownWrapperWidth + 'px';

            const dropDownCalloutHeight = dropdownCallout.offsetHeight;
            const dropDownCalloutWidth = dropdownCallout.offsetWidth;
            const dropDownHeight = dropdown.offsetHeight;
            const dropDownTop = dropdown.getBoundingClientRect().y;
            const dropDownWrapperHeight = dropdownWrapper.offsetHeight;
            const dropDownWrapperX = dropdownWrapper.getBoundingClientRect().x;
            const dropDownWrapperY = dropdownWrapper.getBoundingClientRect().y;
            const dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            const dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);
            const minimumWidthForDropdownNormalOpen = 640;

            //clear last style
            dropdownCallout.style.top = "";
            dropdownCallout.style.left = "";
            dropdownCallout.style.right = "";
            dropdownCallout.style.bottom = "";
            dropdownCallout.style.height = "";
            dropdownCallout.style.maxHeight = "";
            scrollWrapper.style.height = "";
            scrollWrapper.style.maxHeight = "";

            if (window.innerWidth < minimumWidthForDropdownNormalOpen && isResponsiveModeEnabled) {
                dropdownCallout.style.top = "0";
                dropdownCallout.style.maxHeight = window.innerHeight + "px";
                scrollWrapper.style.maxHeight = (window.innerHeight - scrollWrapper.getBoundingClientRect().y) + "px";
                scrollWrapper.style.height = scrollWrapper.style.maxHeight;

                if (isRtl) {
                    dropdownCallout.style.left = "0";
                }
                else {
                    dropdownCallout.style.right = "0";
                }
            } else if (dropDirection == BitDropDirection.TopAndBottom) {
                dropdownCallout.style.left = dropDownWrapperX + "px";

                if (dropDownCalloutHeight <= dropDownWrapperBottom || dropDownWrapperBottom >= dropDownTop) {
                    dropdownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                    dropdownCallout.style.maxHeight = (dropDownWrapperBottom - 10) + "px";
                }
                else {
                    dropdownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                    dropdownCallout.style.maxHeight = (dropDownTop - 10) + "px";
                }
            } else {
                if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                    dropdownCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                    dropdownCallout.style.left = dropDownWrapperX + "px";
                    dropdownCallout.style.maxHeight = (dropDownWrapperBottom - 10) + "px";
                } else if (dropDownTop >= dropDownCalloutHeight) {
                    dropdownCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                    dropdownCallout.style.left = dropDownWrapperX + "px";
                    dropdownCallout.style.maxHeight = (dropDownTop - 10) + "px";
                } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                    dropdownCallout.style.left = dropDownWrapperX + dropDownWrapperWidth + 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    dropdownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                } else {
                    dropdownCallout.style.left = dropDownWrapperX - dropDownCalloutWidth - 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    dropdownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                }
            }
        }
    }
}

enum BitDropDirection {
    Auto,
    TopAndBottom
}