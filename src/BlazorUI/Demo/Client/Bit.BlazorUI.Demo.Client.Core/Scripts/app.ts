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
// control (the gallery's) must not steal the shortcut from it. A claim only lives as long as the
// element that made it, though: the home page renders its finder in the hero instead of the header,
// so navigating away from it leaves the claim pointing at an element that is gone, and the next box
// to register takes over. The listener itself is attached once, whoever holds the claim.
let searchShortcutRootId: string | null = null;
let searchShortcutListening = false;

function registerSearchShortcut(rootElementId: string) {
    if (searchShortcutRootId != null && document.getElementById(searchShortcutRootId) != null) return;

    searchShortcutRootId = rootElementId;

    if (searchShortcutListening) return;

    searchShortcutListening = true;

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

const elementWidthObservers: { [key: string]: ResizeObserver } = {};

// Reports the usable (padding-excluded) width of an element to .NET whenever it changes. The
// iconography grid virtualizes by row, so it has to know how many cells fit across before it can
// chunk the icons - and a window resize listener would not see the width change when it is the
// side rail collapsing or a scrollbar appearing rather than the window that moved.
function observeElementWidth(id: string, dotnetObj: any, methodName: string) {
    unobserveElementWidth(id);

    const element = document.getElementById(id);
    if (element == null) return;

    const report = () => {
        const style = getComputedStyle(element);
        const width = element.clientWidth - parseFloat(style.paddingInlineStart) - parseFloat(style.paddingInlineEnd);
        dotnetObj.invokeMethodAsync(methodName, Math.max(0, width));
    };

    const observer = new ResizeObserver(report);
    observer.observe(element);
    elementWidthObservers[id] = observer;

    report();
}

function unobserveElementWidth(id: string) {
    const observer = elementWidthObservers[id];
    if (observer == null) return;

    observer.disconnect();
    delete elementWidthObservers[id];
}

// Keyed by a token the caller owns, not by the element id. Demo pages reuse the same ids from one
// page to the next - every component page has an "example1", and one "api-tables" - and a page being
// navigated away from tears its registrations down asynchronously, well after the page replacing it
// has put its own in. Keyed by id, that teardown disconnects the observers of the page that replaced
// it, and the examples they were watching never mount - not on scroll either, since nothing is
// watching them any more. A per-instance token lets a component only ever unregister itself.
const visibilityObservers: { [key: string]: IntersectionObserver } = {};

// Reports - once, and then never again - that the element with the given id has come within reach of
// the viewport. It is how a demo page mounts an example's live preview, and its API tables, only when
// the reader is actually approaching them: a component page carries dozens of working components and
// a few hundred table rows, and building all of it on every navigation is what makes those pages
// freeze on a phone.
//
// The margin is deliberately asymmetric. Downwards it is one and a half phone screens, so a block is
// mounted well before it can be seen. Upwards it is effectively infinite, which means everything
// ABOVE the reader always counts as reached: a block only ever stays unmounted BELOW where they are,
// so mounting can never change the height of the page above the scroll position - which would slide
// the page under them, and land a restored scroll position or an "#example12" deep link in the wrong
// place.
function observeVisibility(key: string, id: string, dotnetObj: any, methodName: string) {
    unobserveVisibility(key);

    // No element to watch means nothing will ever report - and the caller is holding a block of the
    // page back until something does. Answering straight away is the only safe reading of that: the
    // block gets built, rather than left empty for the life of the page with nothing left to trigger
    // it and scrolling to it doing nothing.
    const element = document.getElementById(id);
    if (element == null) {
        dotnetObj.invokeMethodAsync(methodName);
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        if (entries.some(entry => entry.isIntersecting) === false) return;

        // Before the callback, not after: invokeMethodAsync resolves on a later turn, and a second
        // entry arriving in the meantime would report the same element twice.
        unobserveVisibility(key);

        dotnetObj.invokeMethodAsync(methodName);
    }, { rootMargin: '100000px 0px 1200px 0px' });

    observer.observe(element);
    visibilityObservers[key] = observer;
}

function unobserveVisibility(key: string) {
    const observer = visibilityObservers[key];
    if (observer == null) return;

    observer.disconnect();
    delete visibilityObservers[key];
}

// The height the element with the given id takes in the document right now, or 0 when there is no
// such element.
//
// A component page asks this of its own previews at the one moment their prerendered copies are
// still the ones on screen - while the client is building the render tree that is about to replace
// them, before that batch reaches the DOM. The answer is the room a held-back preview has to keep,
// which is what lets the very first page hold anything back at all: the placeholder stands exactly
// as tall as the markup the server sent, so nothing below it moves.
function getElementHeight(id: string) {
    const element = document.getElementById(id);
    return element == null ? 0 : element.getBoundingClientRect().height;
}

// Keyed by a per-instance token for the same reason the visibility observers are: the page being
// left cancels its idle work asynchronously, and under a key shared by every demo page that
// cancellation lands on the queue of the page that replaced it - which then never fills anything in,
// because the chain that would have rescheduled it is exactly what was cancelled.
const idleWorkHandles: { [key: string]: { handle: number, isIdle: boolean } } = {};

// Calls the named method the next time the browser has nothing better to do. It is how a demo page
// fills in the previews the reader has not scrolled to: the visibility observer guarantees nobody
// ever waits for one, and this guarantees the page becomes whole anyway - so an anchor lands in the
// right place, find-in-page finds everything, and a fast scroll never outruns the observer.
//
// One call mounts one preview and then asks for the next slice, rather than draining the queue in a
// single callback: the point is to leave the main thread between two of them.
function requestIdleWork(key: string, dotnetObj: any, methodName: string) {
    cancelIdleWork(key);

    const run = () => {
        delete idleWorkHandles[key];
        dotnetObj.invokeMethodAsync(methodName);
    };

    // Safari still has no requestIdleCallback; a short timeout is the same shape of promise, minus
    // the browser's opinion about when it is idle.
    const idle = (window as any).requestIdleCallback;
    idleWorkHandles[key] = idle
        ? { handle: idle(run, { timeout: 500 }), isIdle: true }
        : { handle: window.setTimeout(run, 32), isIdle: false };
}

function cancelIdleWork(key: string) {
    const entry = idleWorkHandles[key];
    if (entry == null) return;

    delete idleWorkHandles[key];

    if (entry.isIdle) {
        (window as any).cancelIdleCallback?.(entry.handle);
    } else {
        window.clearTimeout(entry.handle);
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
