namespace BitBlazorUI {
    export class Ratings {
        private static _controllers = new Map<string, AbortController>();

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        public static setup(id: string, dotnetObj: DotNetObject, focusInHandler: string, focusOutHandler: string) {
            Ratings.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const controller = new AbortController();

            // Attaches a keydown listener that only prevents the default behavior (page scrolling) of the
            // navigation keys pressed on the rating items. The actual keyboard logic runs in the Blazor
            // keydown handler, which cannot conditionally preventDefault per key: its flag is applied by
            // the next render, so the first press of a key would scroll the page anyway. Kept key-scoped
            // so Tab, Space and Enter still behave normally.
            root.addEventListener('keydown', e => {
                if (Ratings._navKeys.indexOf(e.key) === -1) return;

                // A held Ctrl, Alt or Meta makes the key a browser or system shortcut instead - Alt+ArrowLeft
                // goes back, Ctrl+Home reaches the top of the page - and the Blazor handler hands those back
                // for the same reason, so their default action has to survive here too.
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                const target = e.target as HTMLElement | null;
                if (!target || !target.closest('.bit-rtg-btn')) return;

                e.preventDefault();
            }, { signal: controller.signal });

            // The focus moving from one item to the next - which every arrow key does - is a focusout of the
            // one and a focusin of the other, both bubbling up to the root. Only the focus crossing the edge
            // of the rating is reported, which takes the relatedTarget that Blazor's FocusEventArgs lacks.
            // A relatedTarget of null is the focus coming from or going to nowhere in the page - the window
            // itself, say - which is crossing that edge as well.
            root.addEventListener('focusin', e => {
                const from = e.relatedTarget as Node | null;
                if (from && root.contains(from)) return;

                dotnetObj.invokeMethodAsync(focusInHandler);
            }, { signal: controller.signal });

            root.addEventListener('focusout', e => {
                const to = e.relatedTarget as Node | null;
                if (to && root.contains(to)) return;

                dotnetObj.invokeMethodAsync(focusOutHandler);
            }, { signal: controller.signal });

            Ratings._controllers.set(id, controller);
        }

        public static dispose(id: string) {
            const controller = Ratings._controllers.get(id);
            if (!controller) return;

            controller.abort();
            Ratings._controllers.delete(id);

            // The DotNetObjectReference is owned and disposed by the component itself.
        }
    }
}
