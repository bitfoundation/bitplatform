class BitLink {
    static scrollToFragmentOnClickEvent(targetElementId: string) {
        const element = document.getElementById(targetElementId);

        if (element != null) {
            element.scrollIntoView({
                behavior: "smooth",
                block: "start",
                inline: "nearest"
            });
        }
    }
}