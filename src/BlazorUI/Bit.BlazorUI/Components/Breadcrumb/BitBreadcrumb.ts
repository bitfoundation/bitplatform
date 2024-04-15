namespace BitBlazorUI {
    export class Breadcrumb {
        public static toggleOverflowCallout(
            dotnetObj: DotNetObject,
            dropdownWrapperId: string,
            dropdownId: string,
            calloutId: string,
            overlayId: string,
            isCalloutOpen: boolean) {

            const dropdownWrapper = document.getElementById(dropdownWrapperId);
            if (dropdownWrapper == null) return;

            const dropdown = document.getElementById(dropdownId);
            if (dropdown == null) return;

            const dropdownCallout = document.getElementById(calloutId);
            if (dropdownCallout == null) return;

            const dropdownOverlay = document.getElementById(overlayId);
            if (dropdownOverlay == null) return;

            if (isCalloutOpen) {
                dropdownCallout.style.display = "none";
                dropdownOverlay.style.display = "none";
                Callouts.reset();
            } else {
                Callouts.replaceCurrent({ calloutId, overlayId, dotnetObj });
                dropdownCallout.style.display = "block";
                dropdownOverlay.style.display = "block";

                const dropdownWrapperWidth = dropdownWrapper.offsetWidth;
                const dropdownWrapperX = dropdownWrapper.getBoundingClientRect().x;
                const dropdownX = dropdown.getBoundingClientRect().x;

                const maxWidth = dropdownWrapperWidth + dropdownWrapperX - dropdownX;
                const minWidth = 150;
                dropdownCallout.style.maxWidth = `${Math.max(maxWidth, minWidth)}px`;

                const dropdownCalloutHeight = dropdownCallout.offsetHeight;
                const dropdownCalloutWidth = dropdownCallout.offsetWidth;
                const dropdownTop = dropdown.getBoundingClientRect().y;
                const dropdownHeight = dropdown.offsetHeight;
                const dropdownWidth = dropdown.offsetWidth;
                const dropdownY = dropdown.getBoundingClientRect().y;
                const dropdownBottom = window.innerHeight - (dropdownHeight + dropdownY);
                const dropdownRight = window.innerWidth - (dropdownWidth + dropdownX);

                if (dropdownBottom >= dropdownCalloutHeight) {
                    dropdownCallout.style.top = dropdownY + dropdownHeight + 1 + "px";
                    dropdownCallout.style.left = dropdownX + "px";
                    dropdownCallout.style.right = "unset";
                    dropdownCallout.style.bottom = "unset";
                }
                else if (dropdownTop >= dropdownCalloutHeight) {
                    dropdownCallout.style.bottom = dropdownBottom + dropdownHeight + 1 + "px";
                    dropdownCallout.style.left = dropdownX + "px";
                    dropdownCallout.style.right = "unset";
                    dropdownCallout.style.top = "unset";
                }
                else if (dropdownRight >= dropdownCalloutWidth) {
                    dropdownCallout.style.left = dropdownX + dropdownWidth + 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    dropdownCallout.style.right = "unset";
                    dropdownCallout.style.top = "unset";
                }
                else {
                    dropdownCallout.style.left = dropdownX - dropdownCalloutWidth - 1 + "px";
                    dropdownCallout.style.bottom = "2px";
                    dropdownCallout.style.top = "unset";
                    dropdownCallout.style.right = "unset";
                }
            }
        }
    }
}