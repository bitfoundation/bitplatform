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

const sideRailScrollSpies: { [key: string]: () => void } = {};

function registerSideRailScrollSpy(id: string, dotnetObj: any, methodName: string, sectionIds: string[]) {
    unregisterSideRailScrollSpy(id);

    let activeId: string | null = null;
    let frame = 0;

    const update = () => {
        frame = 0;

        // A section scrolled to via the rail lands with its top at its own scroll-margin-top, and
        // that margin varies per page (7rem on plain pages, 11rem under a pivot's extra sticky bar)
        // and per element (the chrome cards use 90px), so each section's activation line is derived
        // from its computed scroll-margin-top plus a little slack for rounding. The active section
        // is then the last one in document order whose top has passed its line; before the first
        // one arrives there (page top), the first entry stands in.
        let current: string | null = null;

        for (const sectionId of sectionIds) {
            const element = document.getElementById(sectionId);
            if (element == null) continue;

            const line = (parseFloat(getComputedStyle(element).scrollMarginTop) || 0) + 18;
            if (element.getBoundingClientRect().top <= line) {
                current = sectionId;
            }
        }

        current = current ?? sectionIds[0] ?? null;

        if (current !== activeId) {
            activeId = current;
            dotnetObj.invokeMethodAsync(methodName, current);
        }
    };

    // Capturing on window keeps the spy agnostic about which element actually scrolls the page
    // (scroll events do not bubble, but they do capture); the rAF gate collapses the bursts a
    // scroll produces into one measurement per frame.
    const listener = () => {
        if (frame !== 0) return;
        frame = requestAnimationFrame(update);
    };

    sideRailScrollSpies[id] = () => {
        window.removeEventListener('scroll', listener, true);
        window.removeEventListener('resize', listener);
        if (frame !== 0) cancelAnimationFrame(frame);
    };
    window.addEventListener('scroll', listener, true);
    window.addEventListener('resize', listener);

    listener();
}

function unregisterSideRailScrollSpy(id: string) {
    const detach = sideRailScrollSpies[id];
    if (detach == null) return;

    detach();
    delete sideRailScrollSpies[id];
}

function copyToClipboard(codeSampleContentForCopy: string) {
    navigator.clipboard.writeText(codeSampleContentForCopy);
}

function highlightSnippet(id: string | undefined) {
    const el = (id && document.getElementById(id)) || document;

    el.querySelectorAll('pre code').forEach((el) => {
        Prism.highlightElement(el);
    });
}

function getInnerText(element: HTMLElement) {
    return element?.innerText;
}

function removeElementById(id: string) {
    document.getElementById(id)?.remove();
}

const windowResizeListeners: { [key: string]: () => void } = {};

function registerWindowResizeListener(id: string, dotnetObj: any, methodName: string) {
    unregisterWindowResizeListener(id);

    const listener = () => dotnetObj.invokeMethodAsync(methodName);
    windowResizeListeners[id] = listener;
    window.addEventListener('resize', listener);
}

function unregisterWindowResizeListener(id: string) {
    const listener = windowResizeListeners[id];
    if (listener == null) return;

    window.removeEventListener('resize', listener);
    delete windowResizeListeners[id];
}

// Cookies are how a client-side preference reaches the server, which needs it to prerender the page
// the way the visitor left it - localStorage is unreachable from there. Lax + the 400-day cap
// browsers clamp to, mirroring what the library writes for its own theme-preference cookie.
function setCookie(name: string, value: string, maxAgeSeconds: number) {
    const secure = location.protocol === 'https:' ? '; Secure' : '';
    document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=${maxAgeSeconds}; SameSite=Lax${secure}`;
}

function getCookie(name: string): string | null {
    const prefix = `${name}=`;
    const match = document.cookie.split(';').map(c => c.trim()).find(c => c.startsWith(prefix));
    if (match == null) return null;

    try {
        return decodeURIComponent(match.substring(prefix.length));
    } catch {
        return match.substring(prefix.length);
    }
}

declare namespace BitBlazorUI {
    class Theme { static init(options: any): void; }
}

// Theme-dependent styling in the app keys off the bit-theme attribute the library script keeps on
// the document element, so this callback only has to maintain what CSS cannot reach: the browser
// chrome color.
BitBlazorUI.Theme.init({
    system: true,
    persist: true,
    // Mirror every theme change into the bit-theme-preference cookie so the server can paint the
    // right theme into the prerendered markup (see App.razor). Without it the server would fall back
    // to following the OS and the app would flash the wrong theme for visitors who picked one.
    persistCookie: true,
    onChange: (newTheme: string, oldTheme: string) => {
        const name = (newTheme ?? '').toLowerCase();
        const isDark = name === 'dark' || name.endsWith('-dark');
        document.querySelector("meta[name=theme-color]")?.setAttribute('content', isDark ? '#0d1117' : '#ffffff');
    }
});
