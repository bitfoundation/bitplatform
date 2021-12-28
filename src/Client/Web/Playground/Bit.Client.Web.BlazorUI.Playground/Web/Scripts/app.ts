declare var hljs: any;

var headerBtn = document.getElementById("headerBtn") ?? new HTMLElement();

window.addEventListener('scroll', () => {
    if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
        headerBtn.style.display = "flex";
    } else {
        headerBtn.style.display = "none";
    }
}, true);


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

    if (element instanceof HTMLElement) {
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