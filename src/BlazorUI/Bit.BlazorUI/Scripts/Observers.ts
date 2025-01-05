namespace BitBlazorUI {
    export class Observers {
        private static _resizeObservers: Record<string, ResizeObserver> = {};

        public static registerResize(element: HTMLElement, obj: DotNetObject, method: string) {
            const observer = new ResizeObserver(entries => {
                const entry = entries[0];
                if (!entry) return;

                obj.invokeMethodAsync(method, entry.contentRect);
            });
            observer.observe(element);

            const id = Utils.uuidv4();
            Observers._resizeObservers[id] = observer;

            return id;
        }

        public static unregisterResize(element: HTMLElement, id: string, obj: DotNetObject) {
            const observer = Observers._resizeObservers[id];
            if (!observer) return;

            observer.unobserve(element);
            delete Observers._resizeObservers[id];
            obj.dispose();
        }
    }
}
