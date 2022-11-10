class BitDateRangePicker {
    static toggleDateRangePickerCallout(dotnetObjReference: DotNetObject,
        dateRangePickerId: string,
        dateRangePickerCalloutId: string,
        dateRangePickerOverlayId: string,
        isOpen: boolean) {

        const dateRangePicker = document.getElementById(dateRangePickerId);
        if (dateRangePicker == null)
            return;

        const dateRangePickerCallout = document.getElementById(dateRangePickerCalloutId);
        if (dateRangePickerCallout == null)
            return;

        const dateRangePickerOverlay = document.getElementById(dateRangePickerOverlayId);
        if (dateRangePickerOverlay == null)
            return;

        if (isOpen) {
            dateRangePickerCallout.style.display = "none";
            dateRangePickerOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        } else {
            Bit.closeCurrentCalloutIfExists(dateRangePickerCalloutId, dateRangePickerOverlayId, dotnetObjReference);
            dateRangePickerCallout.style.display = "block";
            dateRangePickerOverlay.style.display = "block";
            const dateRangePickerCalloutHeight = dateRangePickerCallout.offsetHeight;
            const dateRangePickerCalloutWidth = dateRangePickerCallout.offsetWidth;
            const dateRangePickerHeight = dateRangePicker.offsetHeight;
            const dateRangePickerWidth = dateRangePicker.offsetWidth;
            const dateRangePickerX = dateRangePicker.getBoundingClientRect().x;
            const dateRangePickerY = dateRangePicker.getBoundingClientRect().y;
            const dateRangePickerTop = dateRangePicker.getBoundingClientRect().y;
            const dateRangePickerWrapperBottom = window.innerHeight - (dateRangePickerHeight + dateRangePickerY);
            const dateRangePickerWrapperRight = window.innerWidth - (dateRangePickerWidth + dateRangePickerX);

            if (dateRangePickerWrapperBottom >= dateRangePickerCalloutHeight) {
                dateRangePickerCallout.style.top = dateRangePickerY + dateRangePickerHeight + 1 + "px";
                dateRangePickerCallout.style.left = dateRangePickerX + "px";
                dateRangePickerCallout.style.right = "unset";
                dateRangePickerCallout.style.bottom = "unset";
            } else if (dateRangePickerTop >= dateRangePickerCalloutHeight) {
                dateRangePickerCallout.style.bottom = dateRangePickerWrapperBottom + dateRangePickerHeight + 1 + "px";;
                dateRangePickerCallout.style.left = dateRangePickerX + "px";
                dateRangePickerCallout.style.right = "unset";
                dateRangePickerCallout.style.top = "unset";
            } else if (dateRangePickerWrapperRight >= dateRangePickerCalloutWidth) {
                dateRangePickerCallout.style.left = dateRangePickerX + dateRangePickerWidth + 1 + "px";
                dateRangePickerCallout.style.bottom = "2px";
                dateRangePickerCallout.style.right = "unset";
                dateRangePickerCallout.style.top = "unset";
            } else {
                dateRangePickerCallout.style.left = dateRangePickerX - dateRangePickerCalloutWidth - 1 + "px";
                dateRangePickerCallout.style.bottom = "2px";
                dateRangePickerCallout.style.right = "unset";
                dateRangePickerCallout.style.top = "unset"
            }
        }
    }

    static checkMonthPickerWidth(dateRangePickerCalloutId: string) {

        const dateRangePickerCallout = document.getElementById(dateRangePickerCalloutId);
        if (dateRangePickerCallout == null)
            return;

        const dateRangePickerCalloutWidth = dateRangePickerCallout.offsetWidth;
        const bodyWidth = document.body.offsetWidth;
        if (dateRangePickerCalloutWidth > bodyWidth) {
            return true;
        } else {
            const calloutLeft = dateRangePickerCallout.getBoundingClientRect().x;
            if (dateRangePickerCalloutWidth + calloutLeft > bodyWidth) {
                dateRangePickerCallout.style.left = "0";
                dateRangePickerCallout.style.right = "0";
                dateRangePickerCallout.style.margin = "auto";
                dateRangePickerCallout.style.width = "fit-content";
            }

            return false;
        }
    }
}