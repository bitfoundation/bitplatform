class BitDropDown {
    static toggleDropDownCallout(dropDownWrapperId: string,
        dropDownId: string,
        dropDowndropDownCalloutId: string,
        dropDownOverlayId: string,
        isOpen: boolean) {
        const dropDownWrapper = document.getElementById(dropDownWrapperId);
        const dropDown = document.getElementById(dropDownId);
        const dropDownCallout = document.getElementById(dropDowndropDownCalloutId);
        const dropDownOverlay = document.getElementById(dropDownOverlayId);

        if (!dropDownCallout || !dropDownOverlay || !dropDownWrapper || !dropDown) return;

        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
        } else {
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
            var dropDownCalloutHeight = dropDownCallout.offsetHeight;
            var dropDownCalloutWidth = dropDownCallout.offsetWidth;
            var dropDownWrapperHeight = dropDownWrapper.offsetHeight;
            var dropDownHeight = dropDown.offsetHeight;
            var dropDownWrapperWidth = dropDownWrapper.offsetWidth;
            var dropDownWrapperX = dropDownWrapper.getBoundingClientRect().x;
            var dropDownWrapperY = dropDownWrapper.getBoundingClientRect().y;
            var dropDownTop = dropDown.getBoundingClientRect().y;
            var dropDownWrapperBottom = window.innerHeight - (dropDownWrapperHeight + dropDownWrapperY);
            var dropDownWrapperRight = window.innerWidth - (dropDownWrapperWidth + dropDownWrapperX);

            if (dropDownWrapperBottom >= dropDownCalloutHeight) {
                dropDownCallout.style.top = dropDownWrapperHeight + 1 + "px";
                dropDownCallout.style.left = "0px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.bottom = "unset";
            } else if (dropDownTop >= dropDownCalloutHeight) {
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
                dropDownCallout.style.bottom = dropDownHeight + 1 + "px";
                dropDownCallout.style.left = "0px";
            } else if (dropDownWrapperRight >= dropDownCalloutWidth) {
                dropDownCallout.style.left = dropDownWrapperWidth + 1 + "px";
                dropDownCallout.style.bottom = 0 - dropDownWrapperBottom + "px";
                dropDownCallout.style.right = "unset";
                dropDownCallout.style.top = "unset";
            } else {
                dropDownCallout.style.left = "unset";
                dropDownCallout.style.top = "unset"
                dropDownCallout.style.right = dropDownWrapperWidth + "px";
                dropDownCallout.style.bottom = 0 - dropDownWrapperBottom + "px";
            }
        }
    }
}