namespace BitBlazorUI {
    export class PageVisibility {
        private static _isInitialized = false;
        private static _handler?: () => void;

        public static init(dotnetObj: DotNetObject) {
            if (PageVisibility._isInitialized) return;

            PageVisibility._isInitialized = true;

            PageVisibility._handler = () => dotnetObj.invokeMethodAsync('VisibilityChanged', document.hidden);

            document.addEventListener('visibilitychange', PageVisibility._handler);
        }

        public static dispose() {
            if (!PageVisibility._isInitialized) return;

            if (PageVisibility._handler) {
                document.removeEventListener('visibilitychange', PageVisibility._handler);
                PageVisibility._handler = undefined;
            }

            PageVisibility._isInitialized = false;
        }
    }
}
