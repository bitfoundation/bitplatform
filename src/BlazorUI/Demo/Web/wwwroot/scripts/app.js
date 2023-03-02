"use strict";
function toggleBodyOverflow(isMenuOpen) {
    if (window.innerWidth < 901) {
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
        Prism.highlightElement(el);
    });
}
function goToTop() {
    window.scrollTo({ top: 0 });
}
