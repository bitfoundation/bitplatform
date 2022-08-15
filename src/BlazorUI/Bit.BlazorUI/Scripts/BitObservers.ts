class BitObservers {
    private static _resizeObservers: Record<string, ResizeObserver> = {};

    static resize(element: HTMLElement, obj: DotNetObject, method: string) {
        const observer = new ResizeObserver(entries => {
            const entry = entries[0];
            if (!entry) return;

            obj.invokeMethodAsync(method, entry.contentRect);
        });
        observer.observe(element);

        const id = Bit.uuidv4();
        BitObservers._resizeObservers[id] = observer;

        return id;
    }

    static unresize(element: HTMLElement, id: string) {
        const observer = BitObservers._resizeObservers[id];
        if (!observer) return;

        observer.unobserve(element);
    }
}