namespace BitBlazorUI {
    export class PageVisibility {
        // One controller owns every listener: registering them with its signal lets dispose() remove them
        // all with a single abort, and "a controller exists" is what says the listeners are installed.
        private static _abort?: AbortController;

        public static init(dotnetObj: DotNetObject) {
            if (PageVisibility._abort) return;

            PageVisibility._abort = new AbortController();
            const signal = PageVisibility._abort.signal;

            document.addEventListener('visibilitychange', () => dotnetObj.invokeMethodAsync('VisibilityChanged', document.hidden), { signal });

            // A window that lost the focus is not hidden - another window is simply covering it, or the focus went
            // to the dev tools or an iframe - so visibilitychange never fires for it. It is reported separately
            // because "the page is not being looked at" and "the page is not being typed into" are different
            // questions, and a consumer that only cares about one of them should not have to hear about the other.
            window.addEventListener('blur', () => dotnetObj.invokeMethodAsync('WindowFocusChanged', true), { signal });
            window.addEventListener('focus', () => dotnetObj.invokeMethodAsync('WindowFocusChanged', false), { signal });
        }

        public static dispose() {
            PageVisibility._abort?.abort();
            PageVisibility._abort = undefined;
        }
    }
}
