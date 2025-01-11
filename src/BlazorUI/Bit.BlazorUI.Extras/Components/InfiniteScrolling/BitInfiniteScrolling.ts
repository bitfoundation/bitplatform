namespace BitBlazorUI {
    export class InfiniteScrolling {
        private static _observers: { [key: string]: IntersectionObserver } = {};

        public static setup(
            id: string,
            scrollerSelector: string | undefined,
            rootElement: HTMLElement,
            lastElement: HTMLElement,
            dotnetObj: DotNetObject) {

            let element = rootElement;

            if (scrollerSelector) {
                const scrollerElement = document.querySelector(scrollerSelector);
                if (scrollerElement) {
                    element = scrollerElement as HTMLElement;
                }
            }

            const observer = new IntersectionObserver(async (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        observer.unobserve(lastElement);
                        await dotnetObj.invokeMethodAsync("Load");
                    }
                }
            }, {
                root: element,
                threshold: 0.69,
            });

            observer.observe(lastElement);

            InfiniteScrolling._observers[id] = observer;
        }

        public static reobserve(id: string, lastElement: HTMLElement) {
            const observer = InfiniteScrolling._observers[id];
            if (!observer) return;

            observer.unobserve(lastElement);
            observer.observe(lastElement);
        }

        public static dispose(id: string) {
            const observer = InfiniteScrolling._observers[id];
            if (!observer) return;

            observer.disconnect();
            delete InfiniteScrolling._observers[id];
        }
    }
}