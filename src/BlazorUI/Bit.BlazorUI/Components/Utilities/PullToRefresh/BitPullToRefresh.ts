namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(element: HTMLElement, anchor: string, threshold: number, dotnetObj: DotNetObject) {
            PullToRefresh._refreshers.push(new BitPullRefresher(element, anchor, threshold, dotnetObj));

            //const main = document.querySelector('.main');
            //const ptr = document.querySelector('.pull-to-refresh');
            //let touchstartY = 0;

            //main.addEventListener('touchstart', e => {
            //    touchstartY = e.touches[0].clientY;
            //    const bcr = main.getBoundingClientRect();
            //    ptr.style.top = `${bcr.top}px`;
            //    ptr.style.left = `${bcr.left}px`;
            //    ptr.style.width = `${bcr.width}px`;
            //});

            //main.addEventListener('touchmove', e => {
            //    if (main.scrollTop !== 0) return;

            //    const touchY = e.touches[0].clientY;
            //    let diff = touchY - touchstartY;

            //    diff = diff > 80 ? 80 : diff;

            //    ptr.style.minHeight = `${diff}px`;
            //});
            //main.addEventListener('touchend', e => {
            //    if (parseInt(ptr.style.minHeight) < 80) {
            //        ptr.style.minHeight = 0;
            //    }
            //});
        }

    }

    class BitPullRefresher {
        element: HTMLElement;
        anchor: string;
        threshold: number;
        dotnetObj: DotNetObject;

        constructor(element: HTMLElement, anchor: string, threshold: number, dotnetObj: DotNetObject) {
            this.element = element;
            this.anchor = anchor;
            this.threshold = threshold;
            this.dotnetObj = dotnetObj;
        }
    }
}