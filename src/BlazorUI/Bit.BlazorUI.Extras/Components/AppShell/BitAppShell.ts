namespace BitBlazorUI {
    export class AppShell {
        public static PreScroll: number = 0;

        private static _currentUrl: string;
        private static _container: HTMLElement;
        private static _scrolls: { [key: string]: number } = {};

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell._currentUrl = url;
            AppShell._scrolls[url] = AppShell.PreScroll;
            if (AppShell.PreScroll > 0) {
                AppShell._container.scrollTo({ top: AppShell.PreScroll, behavior: 'instant' });
            }
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
            if (AppShell._container != null) {
                AppShell._container.removeEventListener('scroll', AppShell.onScroll);
            }
        }

        private static onScroll() {
            AppShell._scrolls[AppShell._currentUrl] = AppShell._container.scrollTop;
        }
    }
}

(function () {
    const container = document.getElementById('BitAppShell-container');
    if (!container) return;

    container.addEventListener('scroll', e => {
        BitBlazorUI.AppShell.PreScroll = container.scrollTop;
    });
}());