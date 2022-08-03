"use strict";
var currentUrlPath = window.location.pathname;
window.onscroll = function () {
    return handleHeaderScroll();
};
var App = /** @class */(function () {
    function App() {}
    App.setCookie = function (name, value, seconds) {
        var date = new Date();
        date.setSeconds(date.getSeconds() + seconds);
        document.cookie = "".concat(name, "=").concat(value, ";expires=").concat(date.toUTCString(), ";path=/");
    };
    App.getCookie = function (name) {
        var cookies = document.cookie.split(';');
        for (var i = 0; i < cookies.length; i++) {
            var cookie = cookies[i].split('=');
            if (App.trim(cookie[0]) == escape(name)) {
                return unescape(App.trim(cookie[1]));
            }
        }
    };
    App.removeCookie = function (name) {
        document.cookie = "".concat(name, "=; Max-Age=0");
    };
    App.goBack = function () {
        window.history.back();
    };
    App.reloadPage = function () {
        location.reload();
    };
    App.trim = function (value) {
        return value.replace(/^\s+|\s+$/g, '');
    };
    App.setBodyStyle = function (value) {
        document.body.setAttribute('style', value);
    };
    return App;
})();
//HeaderShadow======================================================================
function handleHeaderScroll() {
    var mainHeader = document.getElementById("mainHeader");
    if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
        if (mainHeader.className.indexOf("header-shadow") < 0) mainHeader.className += " header-shadow";
    } else {
        mainHeader.className = mainHeader.className.replace(" header-shadow", "");
    }
    if (currentUrlPath == "/process" || currentUrlPath == "/services" || currentUrlPath == "/about") {
        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
            mainHeader.className = mainHeader.className.replace(" transparent-header", "");
        } else {
            if (mainHeader.className.indexOf("transparent-header") < 0) mainHeader.className += " transparent-header";
        }
    }
    if (currentUrlPath == "/") {
        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
            mainHeader.className = mainHeader.className.replace(" home-page-header", "");
        } else {
            if (mainHeader.className.indexOf("home-page-header") < 0) mainHeader.className += " home-page-header";
        }
    }
}

