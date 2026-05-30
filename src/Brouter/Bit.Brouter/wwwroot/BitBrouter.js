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
