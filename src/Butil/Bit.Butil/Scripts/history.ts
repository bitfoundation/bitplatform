var BitButil = BitButil || {};

(function (butil: any) {
    butil.history = {
        length,
        scrollRestoration,
        setScrollRestoration,
        state,
        back,
        forward,
        go,
        pushState,
        replaceState
    };

    function length() {
        return window.history.length;
    }

    function scrollRestoration() {
        return window.history.scrollRestoration;
    }

    function setScrollRestoration(value) {
        window.history.scrollRestoration = value;
    }

    function state() {
        return window.history.state;
    }

    function back() {
        window.history.back();
    }

    function forward() {
        window.history.forward();
    }

    function go(delta) {
        window.history.go(delta);
    }

    function pushState(state, unused, url) {
        window.history.pushState(state, unused, url);
    }

    function replaceState(state, unused, url) {
        window.history.replaceState(state, unused, url);
    }
}(BitButil));