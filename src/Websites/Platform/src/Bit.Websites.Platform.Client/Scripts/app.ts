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
// element (:root[bit-theme=...] selectors in the stylesheets), so the only thing left to maintain is
// what CSS cannot reach: the browser chrome color (see syncThemeColor below).
BitBlazorUI.Theme.init({
    system: true,
    persist: true,
    // Mirror every theme change into the bit-theme-preference cookie so the server can paint the
    // right theme into the prerendered markup (see App.razor). Without it the server would fall back
    // to guessing and the page would flash the wrong theme for visitors who picked one.
    persistCookie: true,
});

// Paints the browser chrome (an installed PWA's status bar, the mobile address bar) with the page's
// own background, read back from the live styles rather than hardcoded per scheme: Styles/app.scss,
// which declares --bit-clr-bg-pri for both palettes, stays the one place those colors are written.
//
// It cannot be read inside Theme's onChange either - with bit-theme-view-transition the bit-theme
// attribute is only written a frame after the callback runs, so the callback would still see the
// outgoing palette. So it is re-read from <body> (where an applyTheme overlay would land) whenever
// an input to it changes, once per frame at most. The server's App.razor still carries its own
// literals for the first paint, which happens before any stylesheet has loaded.
let themeColorSyncPending = false;
function syncThemeColor() {
    themeColorSyncPending = false;
    const color = getComputedStyle(document.body).getPropertyValue('--bit-clr-bg-pri').trim();
    if (!color) return;
    const meta = document.querySelector('meta[name=theme-color]');
    if (meta && meta.getAttribute('content') !== color) {
        meta.setAttribute('content', color);
    }
}
function scheduleThemeColorSync() {
    if (themeColorSyncPending) return;
    themeColorSyncPending = true;
    requestAnimationFrame(syncThemeColor);
}
const themeColorObserver = new MutationObserver(scheduleThemeColorSync);
themeColorObserver.observe(document.documentElement, { attributes: true, attributeFilter: ['bit-theme'] });
themeColorObserver.observe(document.body, { attributes: true, attributeFilter: ['style', 'class'] });
// A stylesheet still in flight leaves --bit-clr-bg-pri empty, and the read above then keeps whatever
// the tag carries rather than blanking it - so the landing has to be noticed. A <head> child appearing
// covers a stylesheet linked by a script; the one-shot load catch-up covers the rest, since a
// stylesheet that was already in the markup changes no node when it finally applies.
themeColorObserver.observe(document.head, { childList: true });
window.addEventListener('load', scheduleThemeColorSync, { once: true });
scheduleThemeColorSync();
