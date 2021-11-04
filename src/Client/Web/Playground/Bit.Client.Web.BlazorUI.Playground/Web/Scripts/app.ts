function toggleBodyOverflow(isMenuOpen: boolean) {
    if (window.innerWidth < 1024) {
        if (isMenuOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "auto";
        }
    }
}