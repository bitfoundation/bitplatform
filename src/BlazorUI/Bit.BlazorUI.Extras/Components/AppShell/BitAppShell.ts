namespace BitBlazorUI {
    export class AppShell {
        private static STORE_KEY = 'bit-appshell-scrolls';

        public static PreScroll: number = 0;

        private static _currentUrl: string;
        private static _container: HTMLElement | undefined;
        private static _scrolls: { [key: string]: number | undefined } = {};

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell._currentUrl = url;
            AppShell._scrolls = JSON.parse(sessionStorage.getItem(AppShell.STORE_KEY) || '{}');
            AppShell.storeScroll(url, AppShell.PreScroll > 0 ? AppShell.PreScroll : AppShell._scrolls[url]);
            if (AppShell._scrolls[url]! > 0) {
                AppShell._container.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            }
            AppShell.addScroll();
        }

        public static locationChangedScroll(url: string) {
            AppShell.removeScroll();
        }

        public static afterRenderScroll(url: string) {
            AppShell._currentUrl = url;
            AppShell.storeScroll(url, AppShell._scrolls[url]);
            AppShell._container?.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            AppShell.addScroll();
        }

        public static disposeScroll() {
            AppShell.removeScroll();
        }

        private static addScroll() {
            AppShell._container?.addEventListener('scroll', AppShell.onScroll);
        }

        private static removeScroll() {
            AppShell._container?.removeEventListener('scroll', AppShell.onScroll);
        }

        private static onScroll() {
            AppShell.storeScroll(AppShell._currentUrl, AppShell._container?.scrollTop);
        }

        private static storeScroll(url: string, value: number | undefined) {
            AppShell._scrolls[url] = value || 0;
            window.sessionStorage.setItem(AppShell.STORE_KEY, JSON.stringify(AppShell._scrolls));
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