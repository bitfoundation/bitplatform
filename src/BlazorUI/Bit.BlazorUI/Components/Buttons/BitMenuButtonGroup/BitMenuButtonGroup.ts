class BitMenuButtonGroup{
    static toggleMenuButtonGroupCallout(dotnetObjReference: DotNetObject,
        splitButtonWrapperId: string,
        splitButtonId: string,
        splitButtonCalloutId: string,
        splitButtonOverlayId: string,
        isOpenMenu: boolean) {

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

            const splitButtonCalloutHeight = splitButtonCallout.offsetHeight;
            const splitButtonCalloutWidth = splitButtonCallout.offsetWidth;

            const splitButtonHeight = splitButton.offsetHeight;
            const splitButtonTop = splitButton.getBoundingClientRect().y;

            const splitButtonWrapperWidth = splitButtonWrapper.offsetWidth;
            const splitButtonWrapperHeight = splitButtonWrapper.offsetHeight;

            const splitButtonWrapperX = splitButtonWrapper.getBoundingClientRect().x;
            const splitButtonWrapperY = splitButtonWrapper.getBoundingClientRect().y;

            const splitButtonWrapperBottom = window.innerHeight - (splitButtonWrapperHeight + splitButtonWrapperY);
            const splitButtonWrapperRight = window.innerWidth - (splitButtonWrapperWidth + splitButtonWrapperX);

            if (splitButtonWrapperBottom >= splitButtonCalloutHeight) {
                splitButtonCallout.style.top = splitButtonWrapperY + splitButtonWrapperHeight + 1 + "px";
                splitButtonCallout.style.left = splitButtonWrapperX + "px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.bottom = "unset";
            } else if (splitButtonTop >= splitButtonCalloutHeight) {
                splitButtonCallout.style.bottom = splitButtonWrapperBottom + splitButtonHeight + 1 + "px";
                splitButtonCallout.style.left = splitButtonWrapperX + "px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.top = "unset";
            } else if (splitButtonWrapperRight >= splitButtonCalloutWidth) {
                splitButtonCallout.style.left = splitButtonWrapperX + splitButtonWrapperWidth + 1 + "px";
                splitButtonCallout.style.bottom = "2px";
                splitButtonCallout.style.right = "unset";
                splitButtonCallout.style.top = "unset";
            } else {
                splitButtonCallout.style.left = splitButtonWrapperX - splitButtonCalloutWidth - 1 + "px";
                splitButtonCallout.style.bottom = "2px";
                splitButtonCallout.style.top = "unset";
                splitButtonCallout.style.right = "unset";
            }
        }
    }
}