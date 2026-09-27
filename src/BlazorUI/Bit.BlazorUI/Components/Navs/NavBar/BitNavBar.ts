namespace BitBlazorUI {
    export class NavBar {
        // The navbar owns the arrow keys, Home and End: they move the focus along the bar. The browser's
        // default for those keys is to scroll the page, which has to be cancelled *before* the event
        // reaches Blazor's .NET handler. @onkeydown:preventDefault cannot do it: Blazor evaluates it at
        // render time, so it cannot know the upcoming key, lags a keystroke behind (the first arrow press
        // still scrolls) and a stale "true" swallows the Tab that follows, trapping the focus inside the
        // bar. A single capture-phase listener decides per key up front instead, and leaves everything
        // else - Tab, Enter, Space, typing - untouched.
        private static readonly NAV_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];

        private static installed = false;

        public static install() {
            if (NavBar.installed || typeof document === 'undefined') return;
            NavBar.installed = true;

            document.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                if (NavBar.NAV_KEYS.indexOf(e.key) < 0) return;

                const target = e.target as HTMLElement | null;
                if (!target?.closest) return;

                // An editable rendered by a custom item template owns its own keys: Home, End and the
                // arrows move the caret there rather than the focus along the bar.
                if (NavBar.isEditable(target)) return;

                // A disabled navbar handles no key at all, so nothing of the browser's own behavior is
                // taken away from it either.
                const item = target.closest('.bit-nbr-itm');
                if (!item || item.closest('.bit-nbr.bit-dis')) return;

                e.preventDefault();
            }, { capture: true });
        }

        // Keeps the selected item inside the visible area of a scrolling navbar. Element.scrollIntoView is
        // deliberately not used here: it scrolls every scrollable ancestor of the item, so bringing a
        // destination into view would drag the page the bar sits on along with it.
        // The item is only handed over when the caller has one in mind (the public ScrollItemIntoView);
        // the selected one is looked up in the DOM instead, so the scroll never depends on an element
        // reference the navbar may not have been handed yet when the selection first lands.
        public static scrollItemIntoView(containerId: string, item?: HTMLElement | null) {
            const container = document.getElementById(containerId);
            if (!container) return;

            const target = item ?? container.querySelector('.bit-nbr-sel') as HTMLElement | null;
            if (!target) return;

            try {
                const containerRect = container.getBoundingClientRect();
                const itemRect = target.getBoundingClientRect();

                // A rail scrolls down its own length and a bar across it, and only one of the two axes ever
                // overflows, so each is corrected on its own and the other one stays where it is.
                if (itemRect.top < containerRect.top) {
                    container.scrollTop -= (containerRect.top - itemRect.top);
                } else if (itemRect.bottom > containerRect.bottom) {
                    container.scrollTop += (itemRect.bottom - containerRect.bottom);
                }

                if (itemRect.left < containerRect.left) {
                    container.scrollLeft -= (containerRect.left - itemRect.left);
                } else if (itemRect.right > containerRect.right) {
                    container.scrollLeft += (itemRect.right - containerRect.right);
                }
            } catch (e) { console.error('BitBlazorUI.NavBar.scrollItemIntoView:', e); }
        }


        // A horizontal Scrollable navbar hides its scrollbar, which leaves a mouse on a desktop with nothing
        // to reach the items past the edge with: a vertical wheel does not scroll a horizontal overflow.
        // The wheel is turned into a horizontal scroll while the bar can still move that way, and handed
        // back to the page at either end so the bar never traps the page scroll. A wheel that already
        // scrolls sideways (a trackpad, Shift+wheel) and a pinch zoom (Ctrl+wheel) are left alone.
        public static setupWheel(containerId: string) {
            const container = document.getElementById(containerId) as (HTMLElement & { bitNbrWheel?: boolean }) | null;
            if (!container || container.bitNbrWheel) return;
            container.bitNbrWheel = true;

            container.addEventListener('wheel', (e: WheelEvent) => {
                if (e.ctrlKey || e.deltaY === 0 || Math.abs(e.deltaX) >= Math.abs(e.deltaY)) return;

                // The mode is read at the time of the event, so a navbar that stops being a horizontal
                // scrolling one keeps the listener without it doing anything.
                const root = container.parentElement;
                if (!root || !root.classList.contains('bit-nbr-scr') || root.classList.contains('bit-nbr-vrt')) return;

                const max = container.scrollWidth - container.clientWidth;
                if (max <= 0) return;

                // Content an item template renders may scroll down its own length; a wheel over it that
                // can still move it that way is its own, not the bar's.
                if (NavBar.hasVerticalScroller(e.target as Element | null, container, e.deltaY)) return;

                // The direction is read off the dir attribute rather than the computed style: this handler
                // runs for every wheel tick and holds the page scroll until it returns, and a style read there
                // is a forced recalculation on each one.
                const rtl = (container.closest('[dir]') as HTMLElement | null)?.dir.toLowerCase() === 'rtl';
                const delta = e.deltaMode === 1 ? e.deltaY * 16 : e.deltaMode === 2 ? e.deltaY * container.clientWidth : e.deltaY;
                // In RTL scrollLeft runs from 0 down to -max, so the distance travelled is its magnitude.
                const position = Math.abs(container.scrollLeft);

                if ((delta < 0 && position <= 0) || (delta > 0 && position >= max - 1)) return;

                e.preventDefault();
                // The list scrolls smoothly (scroll-behavior), and a relative scroll started while the previous
                // one is still animating would add to where that one has got to rather than to where it is
                // going, losing distance on every tick of a fast spin. The wheel's own ticks are the motion here,
                // so each one lands at once.
                container.scrollBy({ left: rtl ? -delta : delta, behavior: 'instant' as ScrollBehavior });
            }, { passive: false });
        }


        private static hasVerticalScroller(target: Element | null, container: HTMLElement, deltaY: number): boolean {
            for (let el = target; el && el !== container; el = el.parentElement) {
                if (el.scrollHeight <= el.clientHeight) continue;

                // Whether the element could still move that way is checked first, since it is cheap: the style
                // is only read for an element that could, to tell a scroller from one that merely overflows.
                if (deltaY < 0 ? el.scrollTop <= 0 : el.scrollTop >= el.scrollHeight - el.clientHeight - 1) continue;

                const overflowY = getComputedStyle(el).overflowY;
                if (overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') return true;
            }

            return false;
        }

        private static isEditable(element: HTMLElement): boolean {
            if (element.isContentEditable) return true;

            const tag = element.tagName;
            return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
        }
    }

    NavBar.install();
}
