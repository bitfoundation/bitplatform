function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth <= 900) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "auto";
        }
    }
}

function goToTop() {
    window.scrollTo({ top: 0 });
}

declare class BitTheme { static init(options: any): void; };

BitTheme.init({
    //system: true,
    default: 'dark',
    persist: true,
    onChange: (newTheme: string, oldThem: string) => {
        if (newTheme === 'dark') {
            document.body.classList.add('bit-platform-dark-theme');
            document.body.classList.remove('bit-platform-light-theme');
        } else {
            document.body.classList.add('bit-platform-light-theme');
            document.body.classList.remove('bit-platform-dark-theme');
        }
    }
});