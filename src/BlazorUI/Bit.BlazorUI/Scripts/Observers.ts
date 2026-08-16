namespace BitBlazorUI {
    export class Observers {
        private static _resizeObservers: Record<string, ResizeObserver> = {};
        private static _intersectionObservers: Record<string, IntersectionObserver> = {};

        // Watches an element for entering the visible region of its nearest scrollable ancestor and
        // reports it back to .NET. Used by lists that load their next page as the end of the loaded
        // items comes into view, where the element observed is a zero height sentinel sitting after
        // the last one. rootMargin grows the region the sentinel is measured against, so a positive
        // margin fires the callback before the sentinel itself is actually reached.
        public static registerIntersection(id: string, element: HTMLElement, obj: DotNetObject, rootMargin: string) {
            if (!element || !(element instanceof Element)) return;

            try {
                Observers.unregisterIntersection(id);

                // The sentinel is unobserved for as long as .NET is busy with the callback and observed
                // again once it is done. That does two things: it keeps a burst of scroll events from
                // stacking up callbacks, and it delivers a fresh observation afterwards - which is what
                // keeps the loading going where a newly loaded page still did not fill the viewport, since
                // an observer only reports a crossing and the sentinel never left the region to cross it
                // again. Once the element stops being observed altogether (the list finished loading and
                // unregistered), the entry is gone from the registry and it is not observed again.
                const observer = new IntersectionObserver(entries => {
                    const entry = entries[entries.length - 1];
                    if (!entry || !entry.isIntersecting) return;

                    observer.unobserve(element);

                    obj.invokeMethodAsync("OnIntersect")
                        .then(() => {
                            if (Observers._intersectionObservers[id] === observer) {
                                observer.observe(element);
                            }
                        })
                        .catch(() => { });
                }, { rootMargin: rootMargin || '0px' });

                observer.observe(element);

                Observers._intersectionObservers[id] = observer;
            } catch (err) {
                console.error(err);
            }
        }

        public static unregisterIntersection(id: string) {
            try {
                const observer = Observers._intersectionObservers[id];
                if (!observer) return;

                observer.disconnect();
                delete Observers._intersectionObservers[id];
            } catch (err) {
                console.error(err);
            }
        }

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
