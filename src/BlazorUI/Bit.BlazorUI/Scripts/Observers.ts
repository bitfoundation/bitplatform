namespace BitBlazorUI {
    export class Observers {
        private static _resizeObservers: Record<string, ResizeObserver> = {};

        public static registerResize(id: string, element: HTMLElement, obj: DotNetObject) {
            if (!element || !(element instanceof Element)) return;

            try {
                const observer = new ResizeObserver(entries => {
                    const entry = entries[0];
                    if (!entry) return;

                    obj.invokeMethodAsync("OnResize", entry.contentRect);
                });
                observer.observe(element);

                Observers._resizeObservers[id] = observer;
            } catch (err) {
                console.error(err);
            }
        }

        public static unregisterResize(id: string, element: HTMLElement, obj: DotNetObject) {
            if (!element || !(element instanceof Element)) return;

            try {
                const observer = Observers._resizeObservers[id];
                if (!observer) return;

                observer.unobserve(element);
                delete Observers._resizeObservers[id];
                obj.dispose();
            } catch (err) {
                console.error(err);
            }
        }
    }
}
