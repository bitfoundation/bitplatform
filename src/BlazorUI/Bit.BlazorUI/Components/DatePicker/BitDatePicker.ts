class BitDatePicker {
    static toggleDatePickerCallout(dotnetObjReference: DotNetObject,
        datePickerId: string,
        datePickerCalloutId: string,
        datePickerOverlayId: string,
        isOpen: boolean) {

        const datePicker = document.getElementById(datePickerId);
        if (datePicker == null) return;

        const datePickerCallout = document.getElementById(datePickerCalloutId);
        if (datePickerCallout == null) return;

        const datePickerOverlay = document.getElementById(datePickerOverlayId);
        if (datePickerOverlay == null) return;

        if (isOpen) {
            datePickerCallout.style.display = "none";
            datePickerOverlay.style.display = "none";
            Bit.currentCallout.update("", "", null);
        } else {
            Bit.closeCurrentCalloutIfExists(datePickerCalloutId, datePickerOverlayId, dotnetObjReference);
            datePickerCallout.style.display = "block";
            datePickerOverlay.style.display = "block";
            const datePickerCalloutHeight = datePickerCallout.offsetHeight;
            const datePickerCalloutWidth = datePickerCallout.offsetWidth;
            const datePickerHeight = datePicker.offsetHeight;
            const datePickerWidth = datePicker.offsetWidth;
            const datePickerX = datePicker.getBoundingClientRect().x;
            const datePickerY = datePicker.getBoundingClientRect().y;
            const datePickerTop = datePicker.getBoundingClientRect().top;
            const datePickerWrapperBottom = window.innerHeight - (datePickerHeight + datePickerY);
            const datePickerWrapperRight = window.innerWidth - (datePickerWidth + datePickerX);

            if (datePickerWrapperBottom >= datePickerCalloutHeight) {
                datePickerCallout.style.top = datePickerY + datePickerHeight + 1 + "px";
                datePickerCallout.style.left = datePickerX + "px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.bottom = "unset";
            } else if (datePickerTop >= datePickerCalloutHeight) {
                datePickerCallout.style.bottom = datePickerWrapperBottom + datePickerHeight + 1 + "px";;
                datePickerCallout.style.left = datePickerX + "px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset";
            } else if (datePickerWrapperRight >= datePickerCalloutWidth) {
                datePickerCallout.style.left = datePickerX + datePickerWidth + 1 + "px";
                datePickerCallout.style.bottom = "2px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset";
            } else {
                datePickerCallout.style.left = datePickerX - datePickerCalloutWidth - 1 + "px";
                datePickerCallout.style.bottom = "2px";
                datePickerCallout.style.right = "unset";
                datePickerCallout.style.top = "unset"
            }
        }
    }

    static checkMonthPickerWidth(datePickerId: string, datePickerCalloutId: string, responsive: boolean) {

        const datePickerCallout = document.getElementById(datePickerCalloutId);

        if (datePickerCallout == null) return;

        if (responsive && window.innerWidth <= 600) {
            datePickerCallout.style.width = "95%";
            datePickerCallout.style.left = "2.5%";
            return true;
        }

        const datePickerCalloutWidth = datePickerCallout.offsetWidth;
        const bodyWidth = document.body.offsetWidth;

        if (datePickerCalloutWidth > bodyWidth) return true;

        const datePicker = document.getElementById(datePickerId);

        if (datePicker == null) return;

        const { x: calloutLeft } = datePickerCallout.getBoundingClientRect();

        if (datePickerCalloutWidth + calloutLeft > bodyWidth) {
            const datePickerOffsetRight = bodyWidth - (datePicker.getBoundingClientRect().x + datePicker.offsetWidth);
            const rightPositon = bodyWidth - (datePickerCalloutWidth + datePickerOffsetRight);
            const leftPositon = rightPositon + datePickerCalloutWidth;
            const spaceFromRight = bodyWidth - rightPositon;
            const spaceFromLeft = bodyWidth - leftPositon;

            if (rightPositon > 0 && spaceFromRight > spaceFromLeft) {
                datePickerCallout.style.left = rightPositon + "px";
            }
            else if (leftPositon > 0 && spaceFromRight < spaceFromLeft) {
                datePickerCallout.style.left = leftPositon + "px";
            }
            else {
                datePickerCallout.style.left = ((bodyWidth - datePickerCalloutWidth) / 2) + "px";
            }
            datePickerCallout.style.right = "unset";
            datePickerCallout.style.margin = "auto";
            datePickerCallout.style.width = "fit-content";
        }

        return false;
    }
}