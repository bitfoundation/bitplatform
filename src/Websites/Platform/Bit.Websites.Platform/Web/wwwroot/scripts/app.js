"use strict";
//declare var hljs: any;
function toggleBodyOverflow(isMenuOpen) {
    if (window.innerWidth < 1440) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        }
        else {
            document.body.style.overflow = "auto";
        }
    }
}
//function scrollToElement(targetElementId: string) {
//    const element = document.getElementById(targetElementId);
//    if (element != null) {
//        element.scrollIntoView({
//            behavior: "smooth",
//            block: "start",
//            inline: "nearest"
//        });
//    }
//}
//function copyToClipboard(codeSampleContentForCopy: string) {
//    navigator.clipboard.writeText(codeSampleContentForCopy);
//}
//function highlightSnippet() {
//    document.querySelectorAll('pre code').forEach((el) => {
//        hljs.highlightBlock(el);
//    });
//}
function RegisterOnScrollToChangeGettingStartedSideRailStyle(element) {
    window.addEventListener('scroll', function (event) {
        if (document.documentElement.scrollTop >= 500) {
            element.classList.add("fixed-getting-started-side-rail-section");
        }
        else {
            element.classList.remove("fixed-getting-started-side-rail-section");
        }
    });
}
