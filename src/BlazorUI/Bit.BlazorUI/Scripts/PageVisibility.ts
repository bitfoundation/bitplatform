namespace BitBlazorUI {
    export class PageVisibility {
        private static _isInitialized = false;
        private static _visibilityHandler?: () => void;
        private static _blurHandler?: () => void;
        private static _focusHandler?: () => void;

        public static init(dotnetObj: DotNetObject) {
            if (PageVisibility._isInitialized) return;

            PageVisibility._isInitialized = true;

            PageVisibility._visibilityHandler = () => dotnetObj.invokeMethodAsync('VisibilityChanged', document.hidden);

            document.addEventListener('visibilitychange', PageVisibility._visibilityHandler);

            // A window that lost the focus is not hidden - another window is simply covering it, or the focus went
            // to the dev tools or an iframe - so visibilitychange never fires for it. It is reported separately
            // because "the page is not being looked at" and "the page is not being typed into" are different
            // questions, and a consumer that only cares about one of them should not have to hear about the other.
            PageVisibility._blurHandler = () => dotnetObj.invokeMethodAsync('WindowFocusChanged', true);
            PageVisibility._focusHandler = () => dotnetObj.invokeMethodAsync('WindowFocusChanged', false);

            window.addEventListener('blur', PageVisibility._blurHandler);
            window.addEventListener('focus', PageVisibility._focusHandler);
        }

        public static dispose() {
            if (!PageVisibility._isInitialized) return;

            if (PageVisibility._visibilityHandler) {
                document.removeEventListener('visibilitychange', PageVisibility._visibilityHandler);
                PageVisibility._visibilityHandler = undefined;
            }

            if (PageVisibility._blurHandler) {
                window.removeEventListener('blur', PageVisibility._blurHandler);
                PageVisibility._blurHandler = undefined;
            }

            if (PageVisibility._focusHandler) {
                window.removeEventListener('focus', PageVisibility._focusHandler);
                PageVisibility._focusHandler = undefined;
            }

            PageVisibility._isInitialized = false;
        }
    }
}
