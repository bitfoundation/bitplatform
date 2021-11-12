class BitDropDown {
    static toggleDropDownCallout(dropDownId: string, dropDownCalloutId: string, dropDownOverlayId: string, isOpen: boolean) {
        const dropDown = document.getElementById(dropDownId);
        const dropDownCallout = document.getElementById(dropDownCalloutId);
        const dropDownOverlay = document.getElementById(dropDownOverlayId);

        if (!dropDownCallout || !dropDownOverlay) return;

        if (isOpen) {
            dropDownCallout.style.display = "none";
            dropDownOverlay.style.display = "none";
        } else {
            dropDownCallout.style.display = "block";
            dropDownOverlay.style.display = "block";
        } 
    }
}