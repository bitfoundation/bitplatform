var BitButil = BitButil || {};

(function (butil: any) {
    butil.visualViewport = {
        offsetLeft() { return window.visualViewport.offsetLeft; },
        offsetTop() { return window.visualViewport.offsetTop; },
        pageLeft() { return window.visualViewport.pageLeft; },
        pageTop() { return window.visualViewport.pageTop; },
        width() { return window.visualViewport.width; },
        height() { return window.visualViewport.height; },
        scale() { return window.visualViewport.scale; },
    };
}(BitButil));