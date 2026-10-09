namespace BitBlazorUI {
    export class PageVisibility {
        private static _isInitialized = false;
        private static _listeners: Record<string, DotNetObject> = {};

        // The page outlives any one .NET side that listens to it - a Server circuit that is replaced after an
        // enhanced navigation, or the two runtimes of the Auto mode - so every caller is kept and told about each
        // change, not only the first one. The events only report a change, so the state the page is already in is
        // handed back on every call.
        public static init(id: string, dotnetObj: DotNetObject) {
            PageVisibility._listeners[id] = dotnetObj;

            if (PageVisibility._isInitialized === false) {
                PageVisibility._isInitialized = true;

                document.addEventListener('visibilitychange', () => PageVisibility.notify('VisibilityChanged', document.hidden));

                // A window that lost the focus is not hidden - another window is simply covering it, or the focus went
                // to the dev tools or an iframe - so visibilitychange never fires for it. It is reported separately
                // because "the page is not being looked at" and "the page is not being typed into" are different
                // questions, and a consumer that only cares about one of them should not have to hear about the other.
                window.addEventListener('blur', () => PageVisibility.notify('WindowFocusChanged', true));
                window.addEventListener('focus', () => PageVisibility.notify('WindowFocusChanged', false));
            }

            return { hidden: document.hidden, blurred: document.hasFocus() === false };
        }

        public static dispose(id: string) {
            delete PageVisibility._listeners[id];
        }

        // A listener whose .NET side went away without saying so (a circuit that is already gone cannot call
        // dispose) is dropped the first time it fails to answer, so the others are not held up by it.
        private static notify(method: string, value: boolean) {
            Object.keys(PageVisibility._listeners).forEach(id => {
                const dotnetObj = PageVisibility._listeners[id];

                try {
                    dotnetObj.invokeMethodAsync(method, value).catch(() => PageVisibility.drop(id, dotnetObj));
                } catch {
                    PageVisibility.drop(id, dotnetObj);
                }
            });
        }

        private static drop(id: string, dotnetObj: DotNetObject) {
            if (PageVisibility._listeners[id] === dotnetObj) {
                delete PageVisibility._listeners[id];
            }
        }
    }
}
