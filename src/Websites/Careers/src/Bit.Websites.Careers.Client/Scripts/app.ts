//declare var hljs: any;

function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth <= 900) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "auto";
        }
    }
}

function RegisterOnScrollToChangeGettingStartedSideRailStyle(element: any) {
    window.addEventListener('scroll', (event) => {
        if (document.documentElement.scrollTop >= 500) {
            element.classList.add("fixed-getting-started-side-rail-section");
        }
        else {
            element.classList.remove("fixed-getting-started-side-rail-section");
        }
    });
}

function goToTop() {
    window.scrollTo({ top: 0 });
}

declare class BitTheme { static init(options: any): void; };

BitTheme.init({
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