class BitSplitButton {
    static toggleSplitButtonCallout(dotnetObjReference: DotNetObject,
        splitButtonWrapperId: string,
        splitButtonId: string,
        splitButtonCalloutId: string,
        splitButtonOverlayId: string,
        isOpenMenu: boolean,
        isResponsiveModeEnabled: boolean) {

        const splitButtonWrapper = document.getElementById(splitButtonWrapperId);
        if (splitButtonWrapper == null)
            return;

        const splitButton = document.getElementById(splitButtonId);
        if (splitButton == null)
            return;

        const splitButtonCallout = document.getElementById(splitButtonCalloutId);
        if (splitButtonCallout == null)
            return;

        const splitButtonOverlay = document.getElementById(splitButtonOverlayId);
        if (splitButtonOverlay == null)
            return;

        if (isOpenMenu) {
            splitButtonCallout.style.display = "none";
            splitButtonOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        } else {
            Bit.closeCurrentCalloutIfExists(splitButtonCalloutId, splitButtonOverlayId, dotnetObjReference);
            splitButtonCallout.style.display = "block";
            splitButtonOverlay.style.display = "block";

            const dropDownWrapperWidth = splitButtonWrapper.offsetWidth;
            splitButtonCallout.style.width = dropDownWrapperWidth + 'px';

            const dropDownCalloutHeight = splitButtonCallout.offsetHeight;
            const dropDownCalloutWidth = splitButtonCallout.offsetWidth;
            const dropDownHeight = splitButton.offsetHeight;
            const dropDownTop = splitButton.getBoundingClientRect().y;
            const dropDownWrapperHeight = splitButtonWrapper.offsetHeight;
            const dropDownWrapperX = splitButtonWrapper.getBoundingClientRect().x;
            const dropDownWrapperY = splitButtonWrapper.getBoundingClientRect().y;
            const dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            const dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);
            const minimumWidthForDropDownNormalOpen = 640;

            if (window.innerWidth < minimumWidthForDropDownNormalOpen && isResponsiveModeEnabled) {
                splitButtonCallout.style.top = "0";
                splitButtonCallout.style.left = "unset";
                splitButtonCallout.style.right = "0";
                splitButtonCallout.style.bottom = "unset";
            } else if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                splitButtonCallout.style.top = dropDownWrapperY + dropDownWrapperHeight + 1 + "px";
                splitButtonCallout.style.left = dropDownWrapperX + "px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.bottom = "unset";
            } else if (dropDownTop >= dropDownCalloutHeight) {
                splitButtonCallout.style.bottom = dropDownWrapperBottom + dropDownHeight + 1 + "px";
                splitButtonCallout.style.left = dropDownWrapperX + "px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.top = "unset";
            } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                splitButtonCallout.style.left = dropDownWrapperX + dropDownWrapperWidth + 1 + "px";
                splitButtonCallout.style.bottom = "2px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.top = "unset";
            } else {
                splitButtonCallout.style.left = dropDownWrapperX - dropDownCalloutWidth - 1 + "px";
                splitButtonCallout.style.bottom = "2px";
                splitButtonCallout.style.top = "unset";
                splitButtonCallout.style.right = "unset";
            }
        }
    }
}