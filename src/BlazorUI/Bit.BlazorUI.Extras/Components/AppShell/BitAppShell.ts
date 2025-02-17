namespace BitBlazorUI {
    export class AppShell {
        private static _currentUrl: string;
        private static _container: HTMLElement;
        private static _scrolls: { [key: string]: number } = {};

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell._currentUrl = url;
            AppShell._scrolls[url] = 0;
            AppShell.addScroll();
        }

        public static locationChangedScroll(url: string) {
            AppShell.removeScroll();
        }

        public static afterRenderScroll(url: string) {
            AppShell._currentUrl = url;
            AppShell._scrolls[url] = AppShell._scrolls[url] || 0;
            AppShell._container.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            AppShell.addScroll();
        }

        public static disposeScroll() {
            AppShell.removeScroll();
        }

        private static addScroll() {
            AppShell._container.addEventListener('scroll', AppShell.onScroll);
        }

        private static removeScroll() {
            AppShell._container.removeEventListener('scroll', AppShell.onScroll);
        }

        private static onScroll() {
            AppShell._scrolls[AppShell._currentUrl] = AppShell._container.scrollTop;
        }
    }
}