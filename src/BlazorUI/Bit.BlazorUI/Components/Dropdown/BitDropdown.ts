class BitDropdown {
    static toggleDropdownCallout(
        dotnetObjReference: DotNetObject,
        dropdownWrapperId: string,
        dropdownId: string,
        dropdownCalloutId: string,
        dropdownOverlayId: string,
        scrollWrapper: HTMLElement,
        dropDirection: BitDropDirection,
        isOpen: boolean,
        isResponsiveModeEnabled: boolean,
        isRtl: boolean,
        showSearchBox: boolean) {

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

            let dropdownWrapperWidth = dropdownWrapper.offsetWidth;
            if (isResponsiveModeEnabled && dropdownWrapperWidth < 320 && window.innerWidth < 640) {
                dropdownWrapperWidth = window.innerWidth > 320 ? 320 : window.innerWidth;
            }

            dropdownCallout.style.width = dropdownWrapperWidth + 'px';

            const dropdownCalloutHeight = dropdownCallout.offsetHeight;
            const dropdownCalloutWidth = dropdownCallout.offsetWidth;
            const dropdownHeight = dropdown.offsetHeight;
            const dropdownTop = dropdown.getBoundingClientRect().y;
            const dropdownWrapperHeight = dropdownWrapper.offsetHeight;
            const dropdownWrapperX = dropdownWrapper.getBoundingClientRect().x;
            const dropdownWrapperY = dropdownWrapper.getBoundingClientRect().y;
            const dropdownWrapperBottom = window.innerHeight - (dropdownWrapperHeight + dropdownWrapperY);
            const dropdownWrapperRight = window.innerWidth - (dropdownWrapperWidth + dropdownWrapperX);
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

            let sbFactor = showSearchBox ? -32 : 0;

            if (window.innerWidth < minimumWidthForDropdownNormalOpen && isResponsiveModeEnabled) {
                dropdownCallout.style.top = "0";
                dropdownCallout.style.maxHeight = window.innerHeight + "px";
                scrollWrapper.style.maxHeight = (window.innerHeight - scrollWrapper.getBoundingClientRect().y) + "px";
                scrollWrapper.style.height = scrollWrapper.style.maxHeight;

                if (isRtl) {
                    dropdownCallout.style.left = "0";
                } else {
                    dropdownCallout.style.right = "0";
                }
            } else if (dropDirection == BitDropDirection.TopAndBottom) {
                dropdownCallout.style.left = dropdownWrapperX + "px";

                if (dropdownCalloutHeight <= dropdownWrapperBottom || dropdownWrapperBottom >= dropdownTop) {
                    dropdownCallout.style.top = dropdownWrapperY + dropdownWrapperHeight + 1 + "px";
                    //dropdownCallout.style.maxHeight = (dropdownWrapperBottom - 10) + "px";
                    scrollWrapper.style.maxHeight = (dropdownWrapperBottom + sbFactor - 10) + "px";
                } else {
                    dropdownCallout.style.bottom = dropdownWrapperBottom + dropdownHeight + 1 + "px";
                    //dropdownCallout.style.maxHeight = (dropdownTop - 10) + "px";
                    scrollWrapper.style.maxHeight = (dropdownTop + sbFactor - 10) + "px";
                }
            } else {
                if (dropdownWrapperBottom >= dropdownCalloutHeight) {
                    dropdownCallout.style.top = dropdownWrapperY + dropdownWrapperHeight + 1 + "px";
                    dropdownCallout.style.left = dropdownWrapperX + "px";
                    //dropdownCallout.style.maxHeight = (dropdownWrapperBottom - 10) + "px";
                    scrollWrapper.style.maxHeight = (dropdownWrapperBottom + sbFactor - 10) + "px";
                } else if (dropdownTop >= dropdownCalloutHeight) {
                    dropdownCallout.style.bottom = dropdownWrapperBottom + dropdownHeight + 1 + "px";
                    dropdownCallout.style.left = dropdownWrapperX + "px";
                    //dropdownCallout.style.maxHeight = (dropdownTop - 10) + "px";
                    scrollWrapper.style.maxHeight = (dropdownTop + sbFactor - 10) + "px";
                } else if (dropdownWrapperRight >= dropdownCalloutWidth) {
                    dropdownCallout.style.left = dropdownWrapperX + dropdownWrapperWidth + 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    //dropdownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                    scrollWrapper.style.maxHeight = (window.innerHeight + sbFactor - 10) + "px";
                } else {
                    dropdownCallout.style.left = dropdownWrapperX - dropdownCalloutWidth - 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    //dropdownCallout.style.maxHeight = (window.innerHeight - 10) + "px";
                    scrollWrapper.style.maxHeight = (window.innerHeight + sbFactor - 10) + "px";
                }
            }
        }
    }
}

enum BitDropDirection {
    Auto,
    TopAndBottom
}