namespace BitBlazorUI {
    export class PageVisibility {
        private static _isInitialized = false;

        // The events only report a change, so the state the page is already in is handed back on every call.
        public static init(dotnetObj: DotNetObject) {
            const state = { hidden: document.hidden, blurred: document.hasFocus() === false };

            if (PageVisibility._isInitialized) return state;

            PageVisibility._isInitialized = true;

            document.addEventListener('visibilitychange', () => dotnetObj.invokeMethodAsync('VisibilityChanged', document.hidden));

            // A window that lost the focus is not hidden - another window is simply covering it, or the focus went
            // to the dev tools or an iframe - so visibilitychange never fires for it. It is reported separately
            // because "the page is not being looked at" and "the page is not being typed into" are different
            // questions, and a consumer that only cares about one of them should not have to hear about the other.
            window.addEventListener('blur', () => dotnetObj.invokeMethodAsync('WindowFocusChanged', true));
            window.addEventListener('focus', () => dotnetObj.invokeMethodAsync('WindowFocusChanged', false));

            return state;
        }
    }
}
