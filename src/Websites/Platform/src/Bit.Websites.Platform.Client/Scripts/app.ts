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

async function jumpToDemos(e: Event) {
    const video = document.querySelector('video');
    if (!video) return;

    video.currentTime = 107; // 01:47
    await video.play();

    (e.target as HTMLElement).style.display = 'none';
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
            document.querySelector("meta[name=theme-color]")?.setAttribute('content', '#060E2D');
        } else {
            document.body.classList.add('bit-platform-light-theme');
            document.body.classList.remove('bit-platform-dark-theme');
            document.querySelector("meta[name=theme-color]")?.setAttribute('content', '#FFFFFF');
        }
    }
});