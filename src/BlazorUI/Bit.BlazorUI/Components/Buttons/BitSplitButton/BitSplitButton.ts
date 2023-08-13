class BitSplitButton {
    static toggleSplitButtonCallout(dotnetObjReference: DotNetObject,
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
            BitCallouts.currentCallout.update("", "", null);
        } else {
            BitCallouts.replaceCurrentCallout(splitButtonCalloutId, splitButtonOverlayId, dotnetObjReference);
            splitButtonCallout.style.display = "block";
            splitButtonOverlay.style.display = "block";

            const splitButtonCalloutHeight = splitButtonCallout.offsetHeight;
            const splitButtonHeight = splitButton.offsetHeight;
            const splitButtonWrapperHeight = splitButtonWrapper.offsetHeight;

            const splitButtonWrapperX = splitButtonWrapper.getBoundingClientRect().x;
            const splitButtonWrapperY = splitButtonWrapper.getBoundingClientRect().y;

            const splitButtonWrapperBottom = window.innerHeight - (splitButtonWrapperHeight + splitButtonWrapperY);

            splitButtonCallout.style.left = splitButtonWrapperX + "px";
            splitButtonCallout.style.right = "unset";

            if (splitButtonWrapperBottom >= splitButtonCalloutHeight) {
                splitButtonCallout.style.top = splitButtonWrapperY + splitButtonWrapperHeight + 1 + "px";
                splitButtonCallout.style.bottom = "unset";

            } else {
                splitButtonCallout.style.bottom = splitButtonWrapperBottom + splitButtonHeight + 1 + "px";
                splitButtonCallout.style.top = "unset";
            }
        }
    }
}