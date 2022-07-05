declare var hljs: any;

const headerBtn = document.getElementById("headerBtn");

if (headerBtn != null) {
    window.addEventListener('scroll', () => {

        if (document.body.scrollTop > 10 || document.documentElement.scrollTop > 10) {
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

function RegisterOnScrollToChangeHeaderStyle(element: any) {
    window.addEventListener('scroll', (event) => {
        if (document.documentElement.scrollTop >= 100) {
            element.classList.add("blue-header-container");
        }
        else {
            element.classList.remove("blue-header-container");
        }
    });
}

function ScrollToGettingStartedChangeSideRailStyle(element: any) {
    window.addEventListener('scroll', (event) => {
        if (document.documentElement.scrollTop >= 500) {
            element.classList.add("fixed-getting-started-side-rail-section");
        }
        else {
            element.classList.remove("fixed-getting-started-side-rail-section");
        }
    });
}