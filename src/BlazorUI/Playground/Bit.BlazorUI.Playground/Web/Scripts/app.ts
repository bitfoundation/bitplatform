﻿declare var hljs: any;

const headerBtn = document.getElementById("headerBtn");

if (headerBtn != null) {
    window.addEventListener('scroll', () => {

        if ((document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) && window.innerWidth > 630) {
            headerBtn.style.display = "flex";
        } else {
            headerBtn.style.display = "none";
        }

    }, true);
}

function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth < 1024) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "auto";
        }
    }
}

function scrollToElement(targetElementId: string) {
    const element = document.getElementById(targetElementId);

    if (element != null) {
        element.scrollIntoView({
            behavior: "smooth",
            block: "start",
            inline: "nearest"
        });
    }
}

function copyToClipboard(codeSampleContentForCopy: string) {
    navigator.clipboard.writeText(codeSampleContentForCopy);
}

function highlightSnippet() {
    document.querySelectorAll('pre code').forEach((el) => {
        hljs.highlightBlock(el);
    });
}