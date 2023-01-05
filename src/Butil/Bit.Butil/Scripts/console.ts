var BitButil = BitButil || {};

(function (butil: any) {
    butil.console = {
        log
    };

    function log(...data: any[]) {
        console.log(...data);
    }
}(BitButil));