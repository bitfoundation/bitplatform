class BitMenuButton {
    static toggleMenuButtonCallout(
        dotnetObjReference: DotNetObject,
        uniqueId: string,
        calloutId: string,
        overlayId: string,
        isCalloutOpen: boolean) {

        const menuButton = document.getElementById(uniqueId);
        if (menuButton == null) return;

        const callout = document.getElementById(calloutId);
        if (callout == null) return;

        const overlay = document.getElementById(overlayId);
        if (overlay == null) return;

        if (!isCalloutOpen) {
            callout.style.display = "none";
            overlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        } else {
            Bit.closeCurrentCalloutIfExists(calloutId, overlayId, dotnetObjReference);
            callout.style.display = "block";
            overlay.style.display = "block";

            const calloutHeight = callout.offsetHeight;
            const calloutWidth = callout.offsetWidth;

            const menuButtonWidth = menuButton.offsetWidth;
            const menuButtonHeight = menuButton.offsetHeight;

            const { x: menuButtonX, y: menuButtonY } = menuButton.getBoundingClientRect();

            const menuButtonBottom = window.innerHeight - (menuButtonHeight + menuButtonY);
            const menuButtonRight = window.innerWidth - (menuButtonWidth + menuButtonX);

            if (menuButtonBottom >= calloutHeight) {
                callout.style.top = menuButtonY + menuButtonHeight + 1 + "px";
                callout.style.left = menuButtonX + "px";
                callout.style.right = "unset";
                callout.style.bottom = "unset";
            } else if (menuButtonY >= calloutHeight) {
                callout.style.bottom = menuButtonBottom + menuButtonHeight + 1 + "px";
                callout.style.left = menuButtonX + "px";
                callout.style.right = "unset";
                callout.style.top = "unset";
            } else if (menuButtonRight >= calloutWidth) {
                callout.style.left = menuButtonX + menuButtonWidth + 1 + "px";
                callout.style.bottom = "2px";
                callout.style.right = "unset";
                callout.style.top = "unset";
            } else {
                callout.style.left = menuButtonX - calloutWidth - 1 + "px";
                callout.style.bottom = "2px";
                callout.style.top = "unset";
                callout.style.right = "unset";
            }
        }
    }
}