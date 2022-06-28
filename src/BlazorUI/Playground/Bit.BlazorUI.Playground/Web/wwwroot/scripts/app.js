"use strict";
var headerBtn = document.getElementById("headerBtn");
if (headerBtn != null) {
    window.addEventListener('scroll', function () {
        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
            headerBtn.style.display = "flex";
        }
        else {
            headerBtn.style.display = "none";
        }
    }, true);
}
function toggleBodyOverflow(isMenuOpen) {
    if (window.innerWidth < 1024) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        }
        else {
            document.body.style.overflow = "auto";
        }
    }
}
function scrollToElement(targetElementId) {
    var element = document.getElementById(targetElementId);
    if (element != null) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest"
        });
    }
}
function copyToClipboard(codeSampleContentForCopy) {
    navigator.clipboard.writeText(codeSampleContentForCopy);
}
function highlightSnippet() {
    document.querySelectorAll('pre code').forEach(function (el) {
        hljs.highlightBlock(el);
    });
}
