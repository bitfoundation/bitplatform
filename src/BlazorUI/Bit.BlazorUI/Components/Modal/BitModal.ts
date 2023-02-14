class BitModal {
    public static toggleScroll(selector: string, isOpen: boolean) {
        const element = document.querySelector(selector) as HTMLElement;
        if (!element) return;
        element.style.overflow = isOpen ? "hidden" : "auto";
    }
}