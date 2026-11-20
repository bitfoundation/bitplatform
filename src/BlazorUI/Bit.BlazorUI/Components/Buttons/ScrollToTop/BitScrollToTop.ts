namespace BitBlazorUI {
    // A BitScrollToTop says everything it needs in its markup - data-bit-stt (the behavior of the scroll), data-bit-stt-offset
    // (how far the scroll has to travel before it is revealed) and data-bit-stt-target (the selector of its scroller) - and
    // nothing in .NET calls into this script: the listeners at the foot of the file act on whatever button the page holds,
    // which is what lets a prerendered page scroll back up before Blazor is interactive.
    export class ScrollToTop {
        public static readonly SELECTOR = '[data-bit-stt]';
        public static readonly REVEAL_SELECTOR = '[data-bit-stt-offset]';
        public static readonly SHOWN = 'data-bit-stt-shown';

        // requestAnimationFrame never hands out a 0 handle, so it doubles as the "no frame pending" mark.
        private static _frame = 0;

        // rAF coalescing keeps a burst of scroll events, or of renders, down to one reading per painted frame.
        public static queue() {
            if (ScrollToTop._frame) return;

            ScrollToTop._frame = requestAnimationFrame(() => {
                ScrollToTop._frame = 0;

                ScrollToTop.revealAll();
            });
        }

        public static revealAll() {
            const buttons = document.querySelectorAll<HTMLElement>(ScrollToTop.REVEAL_SELECTOR);

            for (let i = 0; i < buttons.length; i++) {
                ScrollToTop.reveal(buttons[i]);
            }
        }

        // Shows the button once its scroller has travelled past the offset, and hides it again within it. A button holding
        // the focus stays revealed: hiding it would drop the focus to the page under a keyboard user's feet.
        public static reveal(button: HTMLElement) {
            try {
                const offset = Math.max(0, parseFloat(button.getAttribute('data-bit-stt-offset') || '0') || 0);

                const shown = ScrollToTop.scrollTop(ScrollToTop.scroller(button)) > offset || document.activeElement === button;

                if (shown === button.hasAttribute(ScrollToTop.SHOWN)) return;

                if (shown) {
                    button.setAttribute(ScrollToTop.SHOWN, '');
                } else {
                    button.removeAttribute(ScrollToTop.SHOWN);
                }
            } catch (e) { console.error("BitBlazorUI.ScrollToTop.reveal:", e); }
        }

        // An explicitly named scroller wins over the walk up the tree, which is what a button that does not sit inside the
        // box it scrolls (an app shell whose content is a sibling of it) needs. A selector that matches nothing - or one
        // that is not valid CSS, which makes querySelector throw - falls back to the walk, as BitHeader's ScrollTarget does.
        public static scroller(button: HTMLElement): HTMLElement | Window {
            const selector = button.getAttribute('data-bit-stt-target');

            if (selector) {
                try {
                    const found = document.querySelector(selector);

                    if (found) return found as HTMLElement;
                } catch { }
            }

            let node = button.parentElement;

            while (node && node !== document.body && node !== document.documentElement) {
                const overflowY = getComputedStyle(node).overflowY;

                // A scrollable overflow that has nothing to scroll is not the scroller of the button: walking past it lands
                // on the box that really scrolls.
                if ((overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') &&
                    node.scrollHeight > node.clientHeight) return node;

                node = node.parentElement;
            }

            return window;
        }

        public static scrollTop(target: HTMLElement | Window): number {
            if (target === window) return window.scrollY || document.documentElement.scrollTop || 0;

            return (target as HTMLElement).scrollTop;
        }

        public static handleClick(e: MouseEvent) {
            if (e.defaultPrevented) return;

            const target = e.target as Element | null;
            const button = (target && target.closest ? target.closest(ScrollToTop.SELECTOR) : null) as HTMLButtonElement | null;

            if (!button || button.disabled || button.getAttribute('aria-disabled') === 'true') return;

            e.preventDefault();

            ScrollToTop.activate(button);
        }

        // Scrolls the scroller of the button back to its top - smoothly unless the button says otherwise or the reader has
        // asked for less motion (a button inside ForceAnimation's bit-fam keeps it) - and moves the focus there along with it.
        public static activate(button: HTMLElement) {
            try {
                const target = ScrollToTop.scroller(button);
                const smooth = button.getAttribute('data-bit-stt') !== 'instant' && !Utils.prefersReducedMotion(button);

                target.scrollTo({ top: 0, behavior: smooth ? 'smooth' : 'instant' });

                ScrollToTop.focusTop(target);
            } catch (e) { console.error("BitBlazorUI.ScrollToTop.activate:", e); }
        }

        // The viewport is at the top now, but the keyboard is still at the bottom, on the button: the next Tab would carry on
        // from there. So the focus is moved onto the scroller itself - the body for the page - from where the next Tab
        // reaches the first thing at the top (a skip link, the header). A box that cannot take the focus of its own is given
        // a tab stop only code can reach, and a ring it would draw is held back, both for as long as it holds the focus: a
        // scroller is no control, and the move is only a starting point for the next Tab. One that is focusable already,
        // or was given a tabindex of its own, is left exactly as it is.
        public static focusTop(target: HTMLElement | Window) {
            const element = target === window ? document.body : target as HTMLElement;
            if (!element) return;

            if (element.tabIndex < 0 && !element.hasAttribute('tabindex')) {
                const outline = element.style.outline;

                element.setAttribute('tabindex', '-1');
                element.style.outline = 'none';

                element.addEventListener('blur', () => {
                    element.removeAttribute('tabindex');
                    element.style.outline = outline;
                }, { once: true });
            }

            // The scroll above is already taking the scroller to its top; letting the focus scroll as well would cut a
            // smooth scroll short.
            element.focus({ preventScroll: true });
        }

        // A button added to the page, or one whose offset or scroller changed, is read at once rather than at the next
        // scroll, which may never come (a page restored scrolled, a button rendered into a pane the reader is deep into).
        public static mutated(records: MutationRecord[]) {
            for (const record of records) {
                if (record.type === 'attributes') {
                    ScrollToTop.queue();
                    return;
                }

                for (let i = 0; i < record.addedNodes.length; i++) {
                    const node = record.addedNodes[i];

                    if (node instanceof Element && (node.matches(ScrollToTop.REVEAL_SELECTOR) || node.querySelector(ScrollToTop.REVEAL_SELECTOR))) {
                        ScrollToTop.queue();
                        return;
                    }
                }
            }
        }
    }
}

// bit-trim: markup data-bit-stt
// Kept wherever a BitScrollToTop can be rendered (its markup says data-bit-stt), and loaded as the app starts, so the
// button works on a prerendered page before Blazor is interactive. One delegated click listener serves every button of
// the page, and one capturing scroll listener reveals them - a scroll event does not bubble, but it is captured at the
// document from every box that scrolls, the page's own included.
namespace BitBlazorUI {
    document.addEventListener('click', (e: MouseEvent) => ScrollToTop.handleClick(e));

    document.addEventListener('scroll', () => ScrollToTop.queue(), { capture: true, passive: true });

    window.addEventListener('resize', () => ScrollToTop.queue(), { passive: true });

    // A button that held the focus was kept revealed (see reveal), so it is read again once it lets go of it.
    document.addEventListener('focusout', (e: FocusEvent) => {
        if (e.target instanceof Element && e.target.matches(ScrollToTop.REVEAL_SELECTOR)) ScrollToTop.queue();
    }, true);

    new MutationObserver(records => ScrollToTop.mutated(records))
        .observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-bit-stt-offset', 'data-bit-stt-target'] });

    ScrollToTop.queue();
}
