declare var Prism: any;

function scrollToElement(targetElementId: string) {
    const element = document.getElementById(targetElementId);

    if (element != null) {
        element.scrollIntoView({
            behavior: "instant",
            block: "start",
            inline: "nearest"
        });
    }
}

function getSideRailItems() {
    return Array.from(document.querySelectorAll<HTMLElement>('[example-section-title]')).map((element) => ({
        id: element.id,
        title: element.innerText
    }));
}

function copyToClipboard(codeSampleContentForCopy: string) {
    navigator.clipboard.writeText(codeSampleContentForCopy);
}

function highlightSnippet() {
    document.querySelectorAll('pre code').forEach((el) => {
        Prism.highlightElement(el);
    });
}

function getInnerText(element: HTMLElement) {
    return element?.innerText;
}

declare class BitTheme { static init(options: any): void; };

BitTheme.init({
    system: true,
    persist: true,
    onChange: (newTheme: string, oldTheme: string) => {
        if (newTheme === 'dark') {
            document.body.classList.add('bit-blazorui-dark-theme');
            document.body.classList.remove('bit-blazorui-light-theme');
            document.querySelector("meta[name=theme-color]")?.setAttribute('content', '#0d1117');
        } else {
            document.body.classList.add('bit-blazorui-light-theme');
            document.body.classList.remove('bit-blazorui-dark-theme');
            document.querySelector("meta[name=theme-color]")?.setAttribute('content', '#ffffff');
        }
    }
});
