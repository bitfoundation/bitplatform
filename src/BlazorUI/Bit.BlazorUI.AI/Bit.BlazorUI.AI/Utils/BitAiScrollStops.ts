namespace BitBlazorUI {
    // The boxes of the AI components that scroll what they hold - a code block, a table, a reasoning panel, the detail of
    // an error - are marked data-bit-ai-scroll in their markup. Such a box holds nothing that takes the focus, so it takes
    // the focus itself while it has something to scroll, which is what lets a keyboard scroll it in an engine that does
    // not make a scrolling box focusable on its own (WebKit). A box its content fits in is left out of the tab order,
    // where it would only be a stop that does nothing.
    export class AiScrollStops {
        public static readonly SELECTOR = '[data-bit-ai-scroll]';

        // The boxes whose tabindex this script wrote, as opposed to one the markup wrote itself (a data table handed a
        // tabindex), which is left alone.
        private static _owned = new WeakSet<Element>();
        private static _pending = new Set<HTMLElement>();
        private static _frame = 0;
        private static _resizes: ResizeObserver | null = null;

        // Measures a box once the frame it changed in is laid out, along with every other box that changed in it.
        public static queue(box: HTMLElement) {
            AiScrollStops._pending.add(box);

            if (AiScrollStops._frame) return;

            AiScrollStops._frame = requestAnimationFrame(() => {
                AiScrollStops._frame = 0;

                const boxes = Array.from(AiScrollStops._pending);
                AiScrollStops._pending.clear();

                for (const b of boxes) AiScrollStops.sync(b);
            });
        }

        public static sync(box: HTMLElement) {
            if (box.isConnected === false) return;

            const owned = AiScrollStops._owned.has(box);
            if (owned === false && box.hasAttribute('tabindex')) return;

            const scrolls = box.scrollWidth > box.clientWidth + 1 || box.scrollHeight > box.clientHeight + 1;

            if (scrolls) {
                if (owned) return;

                box.setAttribute('tabindex', '0');
                AiScrollStops._owned.add(box);
            } else if (owned && box !== document.activeElement) {
                // A box that stopped scrolling while it held the focus keeps its stop until the focus leaves it (see the
                // focusout below): taking the tabindex away would drop the focus to the page.
                box.removeAttribute('tabindex');
                AiScrollStops._owned.delete(box);
            }
        }

        // A box changes what it can scroll as its own size changes (the window, a panel opening) and as its content does,
        // which the page-level watch below reports.
        public static watch(box: HTMLElement) {
            AiScrollStops._resizes ??= new ResizeObserver(entries => {
                for (const entry of entries) AiScrollStops.queue(entry.target as HTMLElement);
            });

            AiScrollStops._resizes.observe(box);
            AiScrollStops.queue(box);
        }

        public static unwatch(box: HTMLElement) {
            AiScrollStops._resizes?.unobserve(box);
            AiScrollStops._pending.delete(box);
        }

        // The boxes in a part of the page that was added or taken away, the part itself included.
        public static boxesIn(node: Node): HTMLElement[] {
            if (node instanceof HTMLElement === false) return [];

            const boxes = Array.from(node.querySelectorAll<HTMLElement>(AiScrollStops.SELECTOR));

            if (node.matches(AiScrollStops.SELECTOR)) boxes.push(node);

            return boxes;
        }
    }
}

// bit-trim: markup data-bit-ai-scroll
// Kept for every component that marks a scrolling box in its markup. The boxes come and go with the renders, and what they
// hold changes with them - a streaming code block grows wider than its box without the box changing size - so the page is
// watched for both, and every box a change landed in is measured again.
namespace BitBlazorUI {
    const scrollStops = new MutationObserver(records => {
        for (const record of records) {
            for (const node of Array.from(record.removedNodes)) {
                for (const box of AiScrollStops.boxesIn(node)) AiScrollStops.unwatch(box);
            }

            for (const node of Array.from(record.addedNodes)) {
                for (const box of AiScrollStops.boxesIn(node)) AiScrollStops.watch(box);
            }

            const target = record.target instanceof Element ? record.target : record.target.parentElement;
            const box = target?.closest<HTMLElement>(AiScrollStops.SELECTOR);
            if (box) AiScrollStops.queue(box);
        }
    });

    scrollStops.observe(document.documentElement, { childList: true, subtree: true, characterData: true });

    for (const box of AiScrollStops.boxesIn(document.documentElement)) AiScrollStops.watch(box);

    document.addEventListener('focusout', e => {
        if (e.target instanceof HTMLElement && e.target.matches(AiScrollStops.SELECTOR)) AiScrollStops.queue(e.target);
    }, true);
}
