"use strict";
var App = /** @class */ (function () {
    function App() {
    }
    App.setCookie = function (name, value, seconds) {
        var date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = "".concat(name, "=").concat(value, ";expires=").concat(date.toUTCString(), ";path=/");
    };
    App.getCookie = function (name) {
        var _a;
        // https://stackoverflow.com/a/25490531/2720104
        return ((_a = document.cookie.match('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)')) === null || _a === void 0 ? void 0 : _a.pop()) || null;
    };
    App.removeCookie = function (name) {
        document.cookie = "".concat(name, "=; Max-Age=0");
    };
    App.goBack = function () {
        window.history.back();
    };
    App.backToTop = function () {
        window.scrollTo({ top: 0 });
    };
    return App;
}());
