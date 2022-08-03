declare var hljs: any;

function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth < 901) {
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