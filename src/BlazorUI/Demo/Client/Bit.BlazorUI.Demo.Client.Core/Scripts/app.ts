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
        title: (element.textContent ?? '').trim(),
        // The tag itself, so the rail can nest an example under the section that hosts it. Anything
        // that is not a heading counts as a leaf.
        level: /^H[1-6]$/.test(element.tagName) ? parseInt(element.tagName[1], 10) : 3
    }));
}

const sideRailScrollSpies: { [key: string]: () => void } = {};

// Each rail entry carries the id of the section it points at in data-rail-item, which is what lets
// the spy move the highlight itself. Both copies of the list - the sticky rail and the responsive
// panel - can be mounted at the same time, so this walks every entry rather than one known id.
function applySideRailActiveItem(activeId: string | null) {
    const activeItems: HTMLElement[] = [];

    document.querySelectorAll<HTMLElement>('[data-rail-item]').forEach((item) => {
        const isActive = item.dataset.railItem === activeId;

        item.classList.toggle('active', isActive);

        if (isActive) {
            activeItems.push(item);
        }

        const link = item.querySelector('button, a');
        if (link == null) return;

        if (isActive) {
            link.setAttribute('aria-current', 'location');
        } else {
            link.removeAttribute('aria-current');
        }
    });

    // After the loop, not inside it: keepSideRailItemInView measures, and a measurement taken between
    // two of the class toggles above would force the layout the browser has just been told is dirty.
    activeItems.forEach((item) => keepSideRailItemInView(item, true));
}

// A page with more sections than the rail is tall makes the rail a scroller of its own, and on such
// a page the entry the spy has just emphasized is regularly one the reader cannot see: the rail
// stays where it was while the page moves on. So the rail follows - it scrolls itself just enough
// to bring the active entry back inside, and only ever itself, which is why this does the arithmetic
// by hand rather than calling scrollIntoView (that one walks up every scrollable ancestor and would
// take the page with it, fighting the very scroll that triggered this).
function keepSideRailItemInView(item: HTMLElement, smooth: boolean) {
    const container = item.closest<HTMLElement>('.side-rail, .side-rail-panel');
    if (container == null) return;

    // Nothing to follow when the whole list fits; the rail is only a scroller on the long pages.
    if (container.scrollHeight - container.clientHeight <= 1) return;

    const itemRect = item.getBoundingClientRect();
    const containerRect = container.getBoundingClientRect();

    // One entry's worth of breathing room on the side it came in from, so the active entry is never
    // flush against the edge with the next one hidden right behind it. Overshooting at the two ends
    // of the list costs nothing - the scroll clamps there anyway.
    const margin = itemRect.height;

    let delta = 0;

    if (itemRect.top < containerRect.top + margin) {
        delta = itemRect.top - containerRect.top - margin;
    } else if (itemRect.bottom > containerRect.bottom - margin) {
        delta = itemRect.bottom - containerRect.bottom + margin;
    }

    if (delta === 0) return;

    // Smooth while the reader scrolls, so the rail reads as following along rather than jumping;
    // instant when a list is being shown for the first time (the panel opening), where there is no
    // previous position for an animation to come from. Reduced motion turns the animation off.
    const behavior: ScrollBehavior = smooth && window.matchMedia('(prefers-reduced-motion: reduce)').matches === false
        ? 'smooth'
        : 'instant';

    container.scrollTo({ top: container.scrollTop + delta, behavior });
}

// The panel copy of the rail is mounted long after the spy last moved the highlight, so it comes up
// scrolled to the top no matter where in the page the reader is. Called once the panel has rendered.
function scrollSideRailToActiveItem() {
    document.querySelectorAll<HTMLElement>('.side-rail-panel [data-rail-item].active')
        .forEach((item) => keepSideRailItemInView(item, false));
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

// The first caller wins: the header's search box registers on every page, and a second copy of the
// control (the gallery's) must not steal the shortcut from it.
let searchShortcutRootId: string | null = null;

function registerSearchShortcut(rootElementId: string) {
    if (searchShortcutRootId != null) return;

    searchShortcutRootId = rootElementId;

    window.addEventListener('keydown', (e: KeyboardEvent) => {
        const isCommandK = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k';

        // A bare "/" is the other conventional docs shortcut, but only while the reader is not
        // already typing somewhere - otherwise it would swallow the character.
        const target = e.target as HTMLElement | null;
        const isTyping = target != null &&
            (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable);
        const isSlash = e.key === '/' && !isTyping && !e.ctrlKey && !e.metaKey && !e.altKey;

        if (!isCommandK && !isSlash) return;

        const input = document.getElementById(searchShortcutRootId!)?.querySelector('input');
        if (input == null) return;

        e.preventDefault();
        input.focus();
        input.select();
    });
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
