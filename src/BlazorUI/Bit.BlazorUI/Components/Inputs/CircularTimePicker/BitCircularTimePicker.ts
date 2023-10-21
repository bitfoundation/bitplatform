class BitCircularTimePicker {
    static toggleTimePickerCallout(dotnetObj: DotNetObject,
        timePickerId: string,
        calloutId: string,
        overlayId: string,
        isOpen: boolean,
        isResponsive: boolean) {

        const timePicker = document.getElementById(timePickerId);
        if (timePicker == null)
            return;

        const timePickerCallout = document.getElementById(calloutId);
        if (timePickerCallout == null)
            return;

        const timePickerOverlay = document.getElementById(overlayId);
        if (timePickerOverlay == null)
            return;

        if (isOpen) {
            timePickerCallout.style.display = "none";
            timePickerOverlay.style.display = "none";
            BitCallouts.reset();
        } else {
            BitCallouts.replaceCurrent({ calloutId, overlayId, dotnetObj });
            timePickerCallout.style.display = "block";
            timePickerOverlay.style.display = "block";
            timePickerCallout.style.width = "unset";
            const timePickerCalloutHeight = timePickerCallout.offsetHeight;
            const timePickerCalloutWidth = timePickerCallout.offsetWidth;
            const timePickerHeight = timePicker.offsetHeight;
            const timePickerWidth = timePicker.offsetWidth;
            const timePickerX = timePicker.getBoundingClientRect().x;
            const timePickerY = timePicker.getBoundingClientRect().y;
            const timePickerTop = timePicker.getBoundingClientRect().y;
            const timePickerWrapperBottom = window.innerHeight - (timePickerHeight + timePickerY);
            const timePickerWrapperRight = window.innerWidth - (timePickerWidth + timePickerX);

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

            if (isResponsive && window.innerWidth <= 600) {
                timePickerCallout.style.maxWidth = "400px";
                timePickerCallout.style.width = "95%";
                timePickerCallout.style.left = "2.5%";
            }
        }
    }
}