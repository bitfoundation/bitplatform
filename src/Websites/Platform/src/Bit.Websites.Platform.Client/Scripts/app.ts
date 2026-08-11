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

// Closes CSS :hover/:focus-within driven popups (e.g. the header products menu) from C#: after a tap
// or an Escape press the trigger keeps focus, so the popup would stay open until focus moves away.
function blurActiveElement() {
    (document.activeElement as HTMLElement | null)?.blur?.();
}

declare namespace BitBlazorUI {
    class Theme { static init(options: any): void; }
}

// Theme-dependent styling keys off the bit-theme attribute the library script keeps on the document
// element (:root[bit-theme=...] selectors in the stylesheets), so this callback only has to maintain
// what CSS cannot reach: the browser chrome color.
BitBlazorUI.Theme.init({
    system: true,
    persist: true,
    // Mirror every theme change into the bit-theme-preference cookie so the server can paint the
    // right theme into the prerendered markup (see App.razor). Without it the server would fall back
    // to guessing and the page would flash the wrong theme for visitors who picked one.
    persistCookie: true,
    onChange: (newTheme: string, oldTheme: string) => {
        // Same dark-name predicate and hex values (--bit-clr-bg-pri of each palette) as the
        // first-paint bootstrap in the server's App.razor - keep the two in sync.
        const name = (newTheme ?? '').toLowerCase();
        const isDark = name === 'dark' || name.endsWith('-dark');
        document.querySelector("meta[name=theme-color]")?.setAttribute('content', isDark ? '#060E2D' : '#FFFFFF');
    }
});
