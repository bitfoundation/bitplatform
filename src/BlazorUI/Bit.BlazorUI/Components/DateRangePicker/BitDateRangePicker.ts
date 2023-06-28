class BitDateRangePicker {
    static toggleDateRangePickerCallout(dotnetObjReference: DotNetObject,
        dateRangePickerId: string,
        dateRangePickerCalloutId: string,
        dateRangePickerOverlayId: string,
        isOpen: boolean) {

        const dateRangePicker = document.getElementById(dateRangePickerId);
        if (dateRangePicker == null) return;

        const dateRangePickerCallout = document.getElementById(dateRangePickerCalloutId);
        if (dateRangePickerCallout == null) return;

        const dateRangePickerOverlay = document.getElementById(dateRangePickerOverlayId);
        if (dateRangePickerOverlay == null) return;

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

    static checkMonthPickerWidth(dateRangePickerId: string, dateRangePickerCalloutId: string, responsive: boolean) {

        const dateRangePickerCallout = document.getElementById(dateRangePickerCalloutId);

        if (dateRangePickerCallout == null) return;

        if (responsive && window.innerWidth <= 600) {
            dateRangePickerCallout.style.width = "95%";
            dateRangePickerCallout.style.left = "2.5%";
            return true;
        }

        const dateRangePickerCalloutWidth = dateRangePickerCallout.offsetWidth;
        const bodyWidth = document.body.offsetWidth;

        if (dateRangePickerCalloutWidth > bodyWidth) return true;

        const dateRangePicker = document.getElementById(dateRangePickerId);

        if (dateRangePicker == null) return;

        const { x: calloutLeft } = dateRangePickerCallout.getBoundingClientRect();

        if (dateRangePickerCalloutWidth + calloutLeft > bodyWidth) {
            const dateRangePickerOffsetRight = bodyWidth - (dateRangePicker.getBoundingClientRect().x + dateRangePicker.offsetWidth);
            const rightPositon = bodyWidth - (dateRangePickerCalloutWidth + dateRangePickerOffsetRight);
            const leftPositon = rightPositon + dateRangePickerCalloutWidth;
            const spaceFromRight = bodyWidth - rightPositon;
            const spaceFromLeft = bodyWidth - leftPositon;

            if (rightPositon > 0 && spaceFromRight > spaceFromLeft) {
                dateRangePickerCallout.style.left = rightPositon + "px";
            }
            else if (leftPositon > 0 && spaceFromRight < spaceFromLeft) {
                dateRangePickerCallout.style.left = leftPositon + "px";
            }
            else {
                dateRangePickerCallout.style.left = ((bodyWidth - dateRangePickerCalloutWidth) / 2) + "px";
            }
            dateRangePickerCallout.style.right = "unset";
            dateRangePickerCallout.style.margin = "auto";
            dateRangePickerCallout.style.width = "fit-content";
        }

        return false;
    }
}
