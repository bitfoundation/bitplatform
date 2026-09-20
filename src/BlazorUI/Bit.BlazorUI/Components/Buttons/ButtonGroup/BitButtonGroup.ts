namespace BitBlazorUI {
    export class ButtonGroup {
        // A navigable button group owns the arrow keys along its own axis, plus Home and End: they move
        // the focus from button to button. The browser's default for those keys is to scroll the page,
        // which has to be cancelled *before* the event reaches Blazor's .NET handler.
        // @onkeydown:preventDefault cannot do it: Blazor evaluates it at render time, so it cannot know
        // the upcoming key, lags a keystroke behind (the first arrow press still scrolls) and a stale
        // "true" swallows the Tab that follows an arrow key, trapping the focus inside the group.
        // A single capture-phase listener decides per key up front instead, and leaves everything else -
        // Tab, Enter, Space, typing - untouched.
        private static readonly HORIZONTAL_KEYS = ['ArrowLeft', 'ArrowRight'];
        private static readonly VERTICAL_KEYS = ['ArrowUp', 'ArrowDown'];
        private static readonly EDGE_KEYS = ['Home', 'End'];

        private static installed = false;

        public static install() {
            if (ButtonGroup.installed || typeof document === 'undefined') return;
            ButtonGroup.installed = true;

            document.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                const target = e.target as HTMLElement | null;
                if (!target?.closest) return;

                // The caret keys of an editable are never cancelled. Interactive content inside a button or a
                // link is invalid markup, so an item template holds none and this is only a guard against one
                // that does: it leaves the caret alone, while the group's own handler still navigates.
                if (ButtonGroup.isEditable(target)) return;

                const item = target.closest('.bit-btg-itm');
                if (!item) return;

                // A group that is disabled, or one whose Navigable is off, navigates nothing, so nothing
                // of the browser's own behavior is taken away from it either.
                const group = item.closest('.bit-btg');
                if (!group || group.classList.contains('bit-dis') || !group.classList.contains('bit-btg-nav')) return;

                // Only the axis the group is laid out along is taken over. The other pair of arrows is
                // left to the page, which is what a reader scrolling past the group expects of them.
                const keys = group.classList.contains('bit-btg-vrt') ? ButtonGroup.VERTICAL_KEYS : ButtonGroup.HORIZONTAL_KEYS;
                if (keys.indexOf(e.key) < 0 && ButtonGroup.EDGE_KEYS.indexOf(e.key) < 0) return;

                e.preventDefault();
            }, { capture: true });
        }


        private static isEditable(element: HTMLElement): boolean {
            if (element.isContentEditable) return true;

            const tag = element.tagName;
            return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
        }
    }

    ButtonGroup.install();
}
