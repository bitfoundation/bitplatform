class BitOverlay {
    public static toggleScroll(selector: string, isVisible: boolean) {
        const element = document.querySelector(selector) as HTMLElement;
        if (!element) return;
        element.style.overflow = isVisible ? "hidden" : "auto";
    }
}