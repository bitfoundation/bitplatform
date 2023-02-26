class BitTimePicker {
    static toggleTimePickerCallout(dotnetObjReference: DotNetObject,
        timePickerId: string,
        timePickerCalloutId: string,
        timePickerOverlayId: string,
        isOpen: boolean,
        isResponsive: boolean) {

        const timePicker = document.getElementById(timePickerId);
        if (timePicker == null)
            return;

        const timePickerCallout = document.getElementById(timePickerCalloutId);
        if (timePickerCallout == null)
            return;

        const timePickerOverlay = document.getElementById(timePickerOverlayId);
        if (timePickerOverlay == null)
            return;

        if (isOpen) {
            timePickerCallout.style.display = "none";
            timePickerOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        } else {
            Bit.closeCurrentCalloutIfExists(timePickerCalloutId, timePickerOverlayId, dotnetObjReference);
            timePickerCallout.style.display = "block";
            timePickerOverlay.style.display = "block";
            const timePickerCalloutHeight = timePickerCallout.offsetHeight;
            const timePickerCalloutWidth = timePickerCallout.offsetWidth;
            const timePickerHeight = timePicker.offsetHeight;
            const timePickerWidth = timePicker.offsetWidth;
            const timePickerX = timePicker.getBoundingClientRect().x;
            const timePickerY = timePicker.getBoundingClientRect().y;
            const timePickerTop = timePicker.getBoundingClientRect().y;
            const timePickerWrapperBottom = window.innerHeight - (timePickerHeight + timePickerY);
            const timePickerWrapperRight = window.innerWidth - (timePickerWidth + timePickerX);

            if (isResponsive && window.innerWidth <= 600) {
                timePickerCallout.style.left = "2.5%";
                if (timePickerWrapperBottom >= timePickerCalloutHeight) {
                    timePickerCallout.style.top = timePickerY + timePickerHeight + 1 + "px";
                    timePickerCallout.style.right = "unset";
                    timePickerCallout.style.bottom = "unset";
                } else if (timePickerTop >= timePickerCalloutHeight) {
                    timePickerCallout.style.bottom = timePickerWrapperBottom + timePickerHeight + 1 + "px";;
                    timePickerCallout.style.right = "unset";
                    timePickerCallout.style.top = "unset";
                } else {
                    timePickerCallout.style.top = "2.5%";
                }

                if (window.innerWidth <= 400 && window.innerWidth > 300) {
                    timePickerCallout.style.width = "95%";
                }
                else {
                    timePickerCallout.style.width = "unset";
                }

                return;
            }


            if (timePickerWrapperBottom >= timePickerCalloutHeight) {
                timePickerCallout.style.top = timePickerY + timePickerHeight + 1 + "px";
                timePickerCallout.style.left = timePickerX + "px";
                timePickerCallout.style.right = "unset";
                timePickerCallout.style.bottom = "unset";
            } else if (timePickerTop >= timePickerCalloutHeight) {
                timePickerCallout.style.bottom = timePickerWrapperBottom + timePickerHeight + 1 + "px";;
                timePickerCallout.style.left = timePickerX + "px";
                timePickerCallout.style.right = "unset";
                timePickerCallout.style.top = "unset";
            } else if (timePickerWrapperRight >= timePickerCalloutWidth) {
                timePickerCallout.style.left = timePickerX + timePickerWidth + 1 + "px";
                timePickerCallout.style.bottom = "2px";
                timePickerCallout.style.right = "unset";
                timePickerCallout.style.top = "unset";
            } else {
                timePickerCallout.style.left = timePickerX - timePickerCalloutWidth - 1 + "px";
                timePickerCallout.style.bottom = "2px";
                timePickerCallout.style.right = "unset";
                timePickerCallout.style.top = "unset"
            }
        }
    }
}