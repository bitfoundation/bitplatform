namespace BitBlazorUI {
    export namespace Utils {

        // Whether the key was pressed while an input method editor (Pinyin, Kana, Hangul, ...) was composing, where
        // Escape cancels the candidate rather than meaning anything to the page: such a press is nobody's, and every
        // listener that closes something on Escape leaves it alone. Most engines report those keydowns as the
        // "Process" key, which no listener is looking for anyway; the ones that report the real key set isComposing,
        // and the ones that predate it report the legacy 229 key code instead.
        export function isComposingKey(e: KeyboardEvent | InputEvent) {
            return e.isComposing || (e as KeyboardEvent).keyCode === 229;
        }

        // Keeps an Escape that cancels an IME composition (see isComposingKey) away from .NET. KeyboardEventArgs cannot
        // tell such a press from any other - it carries no key code, and no composition state before .NET 9 - and the
        // engines that report the real key for it (WebKit) would have every .NET handler that acts on Escape - a
        // dropdown's search box, a picker's field, a callout around a field of the consumer's own - act on the press
        // the user made to put a candidate back. Utils.Keys.ts adds it on the document element in the bubble phase, so
        // every listener of the page's own elements has heard the key by then - the field's, the surfaces', which
        // leave it alone themselves - and only Blazor's delegation on the document, and the listeners after it, do
        // not: such a press then reaches .NET in no engine, where the others report it as the "Process" key anyway.
        export function stopComposingEscape(e: KeyboardEvent) {
            if (e.key !== 'Escape' || !Utils.isComposingKey(e)) return;

            e.stopPropagation();
        }

        // Thrown by isRtl to an arrow key the user has moved on from; BitComponentBase.IsRtlArrowAsync reads it.
        export const ARROW_KEY_OVERTAKEN = 'BitBlazorUI: the arrow key was overtaken';

        export let _arrowKeyTarget: EventTarget | null = null;
        export let _arrowKeyOvertaken = false;

        // Follows the horizontal arrows and what takes the focus away from where one was pressed (see isRtl).
        // Utils.Keys.ts adds it on the document element in the capture phase, for every keydown and every pointerdown, so
        // it has seen a key before any listener of the page's elements and before Blazor sends it to .NET. A Tab and a pointer press
        // are what move the focus without a component asking for it; every other key is a component's own to act
        // on, in the order .NET handles them.
        export function trackArrowKey(e: KeyboardEvent | PointerEvent) {
            if (e.type === 'pointerdown') {
                Utils._arrowKeyOvertaken = true;
                return;
            }

            const key = e as KeyboardEvent;
            if (Utils.isComposingKey(key)) return;

            if (key.key === 'ArrowLeft' || key.key === 'ArrowRight' || key.code === 'ArrowLeft' || key.code === 'ArrowRight') {
                Utils._arrowKeyTarget = key.target;
                Utils._arrowKeyOvertaken = false;
                return;
            }

            if (key.key === 'Tab') {
                Utils._arrowKeyOvertaken = true;
            }
        }

        // Whether the element is laid out right to left. A component whose Dir is unset inherits the direction of
        // the page and one whose Dir is Auto takes it from its content, so neither is known to .NET; the computed
        // style resolves both, the dir attribute of every ancestor included.
        // Asked for a horizontal arrow (arrow), it is the round trip a key handler waits on before it moves the
        // focus, so a Tab or a click that took the focus elsewhere while it was out would have that focus stolen back
        // by a key the user has already moved on from. Such a key is answered with ARROW_KEY_OVERTAKEN instead of a
        // direction, which .NET turns into the cancellation of the handler (BitComponentBase.IsRtlArrowAsync).
        export function isRtl(element: HTMLElement, arrow?: boolean): boolean {
            if (arrow
                && Utils._arrowKeyOvertaken
                && Utils._arrowKeyTarget
                && document.activeElement !== Utils._arrowKeyTarget) {
                throw new Error(Utils.ARROW_KEY_OVERTAKEN);
            }

            if (!element) return false;

            try {
                return getComputedStyle(element).direction === 'rtl';
            } catch (e) {
                console.error("BitBlazorUI.Utils.isRtl:", e);
                return false;
            }
        }
    }
}

// bit-trim: always
// Kept by every bundle: every component acting on Escape or on a horizontal arrow in .NET depends on these two, and
// what they keep from .NET or drop is decided on the document as the page runs, by no interop call.

// An Escape that cancels an IME composition never reaches .NET (see Utils.stopComposingEscape): on the document element
// in the bubble phase, after every listener of the page's elements and right ahead of Blazor's delegation on the document.
document.documentElement.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.stopComposingEscape(e));

// A horizontal arrow whose direction a component reads in a round trip is dropped once the user has taken the focus
// elsewhere (see Utils.isRtl), so the keys and the presses that do that are followed in the capture phase, ahead of
// every listener of the page's elements and of Blazor's delegation. On the document element rather than the window,
// so that the window's own keydown listener - the claim's, Utils.Escape.ts - keeps its place at the start of the dispatch.
document.documentElement.addEventListener('keydown', (e: KeyboardEvent) => BitBlazorUI.Utils.trackArrowKey(e), true);
document.documentElement.addEventListener('pointerdown', (e: PointerEvent) => BitBlazorUI.Utils.trackArrowKey(e), true);
