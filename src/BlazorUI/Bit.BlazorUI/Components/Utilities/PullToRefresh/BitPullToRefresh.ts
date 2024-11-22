namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: BitPullRefresher[] = [];

        public static setup(anchor: string, threshold: number, dotnetObj: DotNetObject) {
            PullToRefresh._refreshers.push(new BitPullRefresher(anchor, threshold, dotnetObj));
        }

    }

    class BitPullRefresher {
        anchor: string;
        threshold: number;
        dotnetObj: DotNetObject;

        constructor(anchor: string, threshold: number, dotnetObj: DotNetObject) {
            this.anchor = anchor;
            this.threshold = threshold;
            this.dotnetObj = dotnetObj;
        }
    }
}