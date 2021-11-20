class BitLink {
    static scrollToFragmentOnClickEvent(targetElementId: string) {
        const element = document.getElementById(targetElementId);

        if (element instanceof HTMLElement) {
            element.scrollIntoView({
                behavior: "smooth",
                block: "start",
                inline: "nearest"
            });
        }
    }
}