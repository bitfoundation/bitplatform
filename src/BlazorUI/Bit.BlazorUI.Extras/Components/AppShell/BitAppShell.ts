namespace BitBlazorUI {
    export class AppShell {
        private static _currentUrl: string;
        private static _container: HTMLElement;
        private static _scrolls: { [key: string]: number } = {};

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell.navigateScroll(url);
            AppShell.addScroll();
        }

        public static navigateScroll(url: string) {
            AppShell._currentUrl = url;
            AppShell._scrolls[url] = AppShell._scrolls[url] || 0;
            AppShell.removeScroll();
            AppShell._container.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            setTimeout(AppShell.addScroll, 13);
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