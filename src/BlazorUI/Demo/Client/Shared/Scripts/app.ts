declare var Prism: any;

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
        Prism.highlightElement(el);
    });
}

function goToTop() {
    window.scrollTo({ top: 0 });
}

function toggleBitTheme(isDark: boolean) {
    document.documentElement.setAttribute('bit-theme', isDark ? 'dark' : 'light');
}

function applyBodyElementStyles(cssClasses: string[], cssVariables: any) {
    cssClasses?.forEach(c => document.body.classList.add(c));
    Object.keys(cssVariables)
        .forEach(key => document.body.style.setProperty(key, cssVariables[key]));
}

function isSystemThemeDark() {
    return matchMedia('(prefers-color-scheme: dark)').matches;
}

(function () {
    toggleBitTheme(isSystemThemeDark());
}());