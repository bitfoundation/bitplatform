namespace BitBlazorUI {
    export class Observers {
        private static _resizeObservers: Record<string, ResizeObserver> = {};
        private static _intersectionObservers: Record<string, IntersectionObserver> = {};

        // Watches an element for entering the visible region (the viewport, as clipped by every scrolling
        // ancestor in between) and reports it back to .NET. Used by lists that load their next page as the
        // end of the loaded items comes into view, where the element observed is a sentinel sitting after the
        // last one. An early start is drawn into the sentinel itself (BitBasicList lays a strip of the length
        // of its threshold back over the end of its items), since that is clipped by whichever box scrolls
        // the list; a rootMargin only ever grows the viewport, and so does nothing for a list that scrolls
        // its own content. rootMargin is still taken for an observer that is meant to be measured against
        // the viewport alone.
        public static registerIntersection(id: string, element: HTMLElement, obj: DotNetObject, rootMargin?: string) {
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
                        .catch(err => { console.error(err); });
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
                // A second registration under the same id replaces the first, whose observer would otherwise keep
                // calling .NET on its own. The reference it was handed is left alone: it may be this one again.
                Observers._resizeObservers[id]?.disconnect();

                // The size is reported on the next frame rather than from inside the callback. In WebAssembly the call
                // runs right where it is made - the render of the component and the measurements that follow it
                // included - and a component that changes its own size there (a breadcrumb collapsing an item, a
                // carousel re-laying its slides) is a size the observer would have to report again within the same
                // frame, which it refuses to with the "loop completed with undelivered notifications" error (an
                // uncaught error of the page in WebKit). Reported from the frame, the change is observed on that
                // frame like any other. The resizes of one frame make one call, with the size the last one left.
                let pending: DOMRectReadOnly | null = null;

                const observer = new ResizeObserver(entries => {
                    const entry = entries[entries.length - 1];
                    if (!entry) return;

                    const scheduled = pending !== null;

                    pending = entry.contentRect;

                    if (scheduled) return;

                    requestAnimationFrame(() => {
                        const rect = pending;

                        pending = null;

                        // An observer unregistered (or replaced) meanwhile has a reference that is released or about
                        // to be, and a component that no longer listens.
                        if (rect === null || Observers._resizeObservers[id] !== observer) return;

                        Utils.notifyDotNet(obj, "OnResize", rect);
                    });
                });
                observer.observe(element);

                Observers._resizeObservers[id] = observer;
            } catch (err) {
                console.error(err);
            }
        }

        // Releases the reference it is handed whether or not an observer was registered for the id, or its element
        // is still there: the components count on this call to release it (BitCarousel), and an element already gone
        // with its parent or a registration that never happened would otherwise leave the reference tracked forever.
        public static unregisterResize(id: string, element: HTMLElement, obj: DotNetObject) {
            try {
                const observer = Observers._resizeObservers[id];
                if (observer) {
                    observer.disconnect();
                    delete Observers._resizeObservers[id];
                }
            } catch (err) {
                console.error(err);
            }

            try {
                obj?.dispose();
            } catch (err) {
                console.error(err);
            }
        }
    }
}

// bit-lazy: teardown Observers.unregisterIntersection Observers.unregisterResize
