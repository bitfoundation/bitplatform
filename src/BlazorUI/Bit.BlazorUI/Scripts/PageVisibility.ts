namespace BitBlazorUI {
    export class PageVisibility {
        private static _isInitialized = false;

        public static init(dotnetObj: DotNetObject) {
            if (PageVisibility._isInitialized) return;

            PageVisibility._isInitialized = true;

            document.addEventListener('visibilitychange', () => dotnetObj.invokeMethodAsync('VisibilityChanged', document.hidden));
        }
    }
}