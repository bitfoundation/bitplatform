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
    // textContent, not innerText: the headings are plain text either way, but innerText is a
    // layout-flushing read, and this runs right after Blazor has written to the DOM.
    return Array.from(document.querySelectorAll<HTMLElement>('[example-section-title]')).map((element) => ({
        id: element.id,
        title: (element.textContent ?? '').trim()
    }));
}

const sideRailScrollSpies: { [key: string]: () => void } = {};

// Each rail entry carries the id of the section it points at in data-rail-item, which is what lets
// the spy move the highlight itself. Both copies of the list - the sticky rail and the responsive
// panel - can be mounted at the same time, so this walks every entry rather than one known id.
function applySideRailActiveItem(activeId: string | null) {
    document.querySelectorAll<HTMLElement>('[data-rail-item]').forEach((item) => {
        const isActive = item.dataset.railItem === activeId;

        item.classList.toggle('active', isActive);

        const link = item.querySelector('button, a');
        if (link == null) return;

        if (isActive) {
            link.setAttribute('aria-current', 'location');
        } else {
            link.removeAttribute('aria-current');
        }
    });
}

function registerSideRailScrollSpy(id: string, dotnetObj: any, activeItemMethodName: string, sectionsChangedMethodName: string, sectionIds: string[]) {
    unregisterSideRailScrollSpy(id);

    let activeId: string | null = null;
    let frame = 0;
    let sections: { id: string, element: HTMLElement, line: number }[] = [];

    // A section scrolled to via the rail lands with its top at its own scroll-margin-top, and that
    // margin varies per page (7rem on plain pages, 11rem under a pivot's extra sticky bar) and per
    // element (the chrome cards use 90px), so each section's activation line is derived from its
    // computed scroll-margin-top plus a little slack for rounding. Resolving that style is the
    // expensive half of the measurement and none of it can change while the page merely scrolls, so
    // it is taken here - once per registration, and again whenever the window resizes.
    const measure = () => {
        sections = [];

        for (const sectionId of sectionIds) {
            const element = document.getElementById(sectionId);
            if (element == null) continue;

            sections.push({ id: sectionId, element, line: (parseFloat(getComputedStyle(element).scrollMarginTop) || 0) + 18 });
        }
    };

    const update = () => {
        frame = 0;

        // Switching a pivot tab swaps the whole set of sections out from under the spy, and nothing
        // announces it - but the elements it measured are no longer in the document. It rebinds to
        // whatever is there now and asks the rail to re-read its list, which is the only place the
        // new titles can come from; measuring again first keeps this from firing every frame.
        if (sections.some(section => section.element.isConnected === false)) {
            measure();
            dotnetObj.invokeMethodAsync(sectionsChangedMethodName);
        }

        // The active section is the last one in document order whose top has passed its line;
        // before the first one arrives there (page top), the first entry stands in.
        let current: string | null = null;

        for (const section of sections) {
            if (section.element.getBoundingClientRect().top <= section.line) {
                current = section.id;
            }
        }

        current = current ?? (sections.length > 0 ? sections[0].id : null);

        if (current === activeId) return;

        activeId = current;

        // The highlight is a class and an aria-current and nothing else, so moving it here keeps
        // scrolling free of both the interop round-trip and the render it used to cost. The call
        // below only tells the rail where the reader is, so that a list rendered later - the panel
        // opening, the sections changing - comes up already pointing at the right entry.
        applySideRailActiveItem(current);
        dotnetObj.invokeMethodAsync(activeItemMethodName, current);
    };

    // Capturing on window keeps the spy agnostic about which element actually scrolls the page
    // (scroll events do not bubble, but they do capture); the rAF gate collapses the bursts a
    // scroll produces into one measurement per frame.
    const listener = () => {
        if (frame !== 0) return;
        frame = requestAnimationFrame(update);
    };

    // A resize can move the activation lines (they are in rem, and the sticky chrome above them
    // changes height across breakpoints), so it re-measures before it re-evaluates.
    const resizeListener = () => {
        measure();
        listener();
    };

    sideRailScrollSpies[id] = () => {
        window.removeEventListener('scroll', listener, true);
        window.removeEventListener('resize', resizeListener);
        if (frame !== 0) cancelAnimationFrame(frame);
    };
    window.addEventListener('scroll', listener, true);
    window.addEventListener('resize', resizeListener);

    measure();
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
