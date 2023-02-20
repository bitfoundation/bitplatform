class BitModal {
    public static toggleScroll(selector: string, isOpen: boolean) {
        const element = document.querySelector(selector) as HTMLElement;

        if (!element) return 0;

        element.style.overflow = isOpen ? "hidden" : "auto";

        return element.scrollTop;

    }
}