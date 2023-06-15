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
    Object.keys(cssVariables).forEach(key => document.body.style.setProperty(key, cssVariables[key]));
}

declare class BitTheme { static initTheme(options: any): void; };

(function () {
    BitTheme.initTheme({
        system: true,
        onChange: (newTheme: string, oldThem: string) => {
            if (newTheme === 'dark') {
                document.body.classList.add('bit-theme-dark');
                document.body.classList.remove('bit-theme-light');
            } else {
                document.body.classList.add('bit-theme-light');
                document.body.classList.remove('bit-theme-dark');
            }
        }
    });
}());