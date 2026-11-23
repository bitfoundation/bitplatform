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
        // An instant reveal skips the fade: a button that is only being put back as it was (one re-created by an
        // interactive render, or one whose mark a DOM sync took away) must not blink out and fade back in.
        public static reveal(button: HTMLElement, instant = false) {
            try {
                const offset = Math.max(0, parseFloat(button.getAttribute('data-bit-stt-offset') || '0') || 0);

                const shown = ScrollToTop.scrollTop(ScrollToTop.scroller(button)) > offset || document.activeElement === button;

                if (shown === button.hasAttribute(ScrollToTop.SHOWN)) return;

                if (shown === false) {
                    button.removeAttribute(ScrollToTop.SHOWN);
                    return;
                }

                if (instant === false) {
                    button.setAttribute(ScrollToTop.SHOWN, '');
                    return;
                }

                // Reading the scroller above already computed the hidden style of the button, so the change is made with
                // no transition and flushed before the transition of the button is given back.
                const transition = button.style.transition;
                button.style.transition = 'none';
                button.setAttribute(ScrollToTop.SHOWN, '');
                void getComputedStyle(button).opacity;
                button.style.transition = transition;
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
                // A scrollable overflow that has nothing to scroll is not the scroller of the button: walking past it lands
                // on the box that really scrolls.
                if (ScrollToTop.scrolls(node)) return node;

                node = node.parentElement;
            }

            // An app shell can make the body the scroller (html { overflow: hidden } body { overflow-y: auto }), where the
            // window never scrolls. The overflow of the body only belongs to the body itself while the root's is not
            // visible: otherwise the browser hands it to the viewport, which the window covers.
            const body = document.body;

            if (body && getComputedStyle(document.documentElement).overflowY !== 'visible' && ScrollToTop.scrolls(body)) return body;

            return window;
        }

        private static scrolls(node: HTMLElement) {
            const overflowY = getComputedStyle(node).overflowY;

            return (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') && node.scrollHeight > node.clientHeight;
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

            let restore: (() => void) | null = null;

            if (element.tabIndex < 0 && !element.hasAttribute('tabindex')) {
                const outline = element.style.outline;

                element.setAttribute('tabindex', '-1');
                element.style.outline = 'none';

                restore = () => {
                    element.removeAttribute('tabindex');
                    element.style.outline = outline;
                };

                element.addEventListener('blur', restore, { once: true });
            }

            // The scroll above is already taking the scroller to its top; letting the focus scroll as well would cut a
            // smooth scroll short.
            element.focus({ preventScroll: true });

            // A box that still cannot take the focus (display: contents, inert, hidden) never fires the blur that would
            // give back its tabindex and its outline, so they are given back at once.
            if (restore && document.activeElement !== element) {
                element.removeEventListener('blur', restore);
                restore();
            }
        }

        // A button added to the page, or one whose offset or scroller changed, is read at once rather than at the next
        // scroll, which may never come (a page restored scrolled, a button rendered into a pane the reader is deep into).
        // An added button, and one whose shown mark was taken away (Blazor's DOM sync of an enhanced navigation, a
        // streaming update or an enhanced form post drops the attributes the server did not render), is put back here,
        // before the next paint and with no fade, since the reader never saw it go.
        // A scroller named by data-bit-stt-target that is rendered, re-rendered or taken away after its button (a page
        // whose content streams in, an interactive render replacing the pane) changes what the button reads, though no
        // button is among the nodes, so a button naming one is read again with the rest.
        public static mutated(records: MutationRecord[]) {
            let changed = false;
            let targeted: NodeListOf<HTMLElement> | null = null;

            for (const record of records) {
                if (record.type === 'attributes') {
                    const target = record.target as HTMLElement;

                    if (target.matches(ScrollToTop.SELECTOR) === false) continue;

                    if (record.attributeName === ScrollToTop.SHOWN) {
                        if (target.matches(ScrollToTop.REVEAL_SELECTOR)) ScrollToTop.reveal(target, true);
                    } else {
                        changed = true;
                    }

                    continue;
                }

                for (let i = 0; i < record.addedNodes.length; i++) {
                    const node = record.addedNodes[i];

                    if (!(node instanceof HTMLElement)) continue;

                    if (node.matches(ScrollToTop.REVEAL_SELECTOR)) ScrollToTop.reveal(node, true);

                    const buttons = node.querySelectorAll<HTMLElement>(ScrollToTop.REVEAL_SELECTOR);

                    for (let j = 0; j < buttons.length; j++) {
                        ScrollToTop.reveal(buttons[j], true);
                    }
                }

                if (changed) continue;

                targeted = targeted || document.querySelectorAll<HTMLElement>('[data-bit-stt-offset][data-bit-stt-target]');

                if (targeted.length === 0) continue;

                changed = ScrollToTop.holdsTarget(record.addedNodes, targeted) || ScrollToTop.holdsTarget(record.removedNodes, targeted);
            }

            if (changed) ScrollToTop.queue();
        }

        private static holdsTarget(nodes: NodeList, buttons: NodeListOf<HTMLElement>) {
            for (let i = 0; i < nodes.length; i++) {
                const node = nodes[i];

                if (!(node instanceof HTMLElement)) continue;

                for (let j = 0; j < buttons.length; j++) {
                    const selector = buttons[j].getAttribute('data-bit-stt-target');

                    if (!selector) continue;

                    // A selector that is not valid CSS names nothing, as in scroller.
                    try {
                        if (node.matches(selector) || node.querySelector(selector)) return true;
                    } catch { }
                }
            }

            return false;
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
        .observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-bit-stt-offset', 'data-bit-stt-target', ScrollToTop.SHOWN] });

    ScrollToTop.queue();
}
