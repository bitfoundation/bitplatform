var BitButil = BitButil || {};

(function (butil: any) {
    butil.window = {
        addBeforeUnload,
        removeBeforeUnload
    };

    function addBeforeUnload() {
        window.onbeforeunload = e => {
            e.preventDefault();
            e.returnValue = true;
            return true;
        };
    }

    function removeBeforeUnload() {
        window.onbeforeunload = null;
    }
}(BitButil));