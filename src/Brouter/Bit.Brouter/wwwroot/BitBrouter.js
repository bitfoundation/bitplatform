// Wires a capture-phase click listener on the given anchor that calls preventDefault
// ONLY for unmodified primary clicks. Modified clicks (Ctrl/Cmd/Shift/Alt) and non-
// primary buttons keep their native browser behavior (e.g., "open in new tab").
//
// Blazor's render-time `onclick:preventDefault` attribute can't be toggled per click,
// so it would otherwise suppress the default action even on modified clicks. With
// this listener installed, Blazor's own onclick handler still fires (and the C# side
// applies the same modifier checks before performing the replace navigation), but
// the browser default is left alone for modified clicks.
export function wireConditionalPreventDefault(element) {
    if (!element) return null;

    const handler = (e) => {
        if (e.defaultPrevented) return;
        if (e.button !== 0) return;
        if (e.ctrlKey || e.shiftKey || e.altKey || e.metaKey) return;
        e.preventDefault();
    };

    // Capture phase so we run before Blazor's bubble-phase onclick handler.
    element.addEventListener('click', handler, { capture: true });

    return {
        dispose: () => element.removeEventListener('click', handler, { capture: true })
    };
}

// Applies the post-navigation DOM effects that Blazor's declarative rendering can't express:
// scrolling a URL fragment into view, moving focus for assistive technologies, and scroll-to-top.
// Called once per successful navigation, after the matched route has been committed to the DOM.
// Every step is best-effort: a missing target is silently ignored so navigation never breaks.
//
//   hash          - the URL fragment including its leading '#', or null/empty when the caller
//                   disabled fragment scrolling or the destination has no fragment.
//   focusSelector - a CSS selector for the element to focus (accessibility), or null to skip.
//   scrollToTop   - whether to scroll the window to the top when no fragment claimed the scroll.
export function applyNavigationEffects(hash, focusSelector, scrollToTop) {
    // 1. Fragment scrolling: navigating to /docs#install should land on the #install element,
    //    and (for keyboard/AT users) continue focus from there rather than the top of the page.
    if (hash && hash.length > 1) {
        let id = hash.substring(1);
        try { id = decodeURIComponent(id); } catch { /* keep the raw, still-encoded fragment */ }

        const target = document.getElementById(id)
            || document.querySelector(`a[name="${cssEscape(id)}"]`);

        if (target) {
            target.scrollIntoView();
            // Fragment focus wins over focusSelector: the user asked to jump to this element.
            focusElement(target);
            return;
        }
        // Fragment target not found -> fall through to the scroll-to-top / focus defaults.
    }

    // 2. Scroll to top (only when no fragment claimed the scroll position above).
    if (scrollToTop) {
        window.scrollTo(0, 0);
    }

    // 3. Focus management: move focus to the configured landmark/heading so screen readers
    //    announce the new page instead of leaving focus on the activated link.
    if (focusSelector) {
        const el = document.querySelector(focusSelector);
        if (el) focusElement(el);
    }
}

// Focuses an element, making it programmatically focusable first if it isn't already. Uses
// preventScroll so focusing doesn't fight a scroll position already set by the caller (fragment
// scrollIntoView above, or window.scrollTo(0,0)).
function focusElement(el) {
    // Non-interactive elements (h1, main, div, ...) have tabIndex -1 and no explicit tabindex;
    // they can't receive programmatic focus until one is added. Use -1 so they're script-focusable
    // but stay out of the sequential Tab order, matching Blazor's FocusOnNavigate behavior.
    if (el.tabIndex < 0 && !el.hasAttribute('tabindex')) {
        el.setAttribute('tabindex', '-1');
    }
    try { el.focus({ preventScroll: true }); } catch { /* element detached mid-navigation */ }
}

// CSS.escape isn't available in every host (older WebViews); fall back to a minimal escape so the
// a[name="..."] fragment fallback can't throw on ids containing quotes/backslashes.
function cssEscape(value) {
    if (window.CSS && typeof window.CSS.escape === 'function') return window.CSS.escape(value);
    return value.replace(/["\\]/g, '\\$&');
}
