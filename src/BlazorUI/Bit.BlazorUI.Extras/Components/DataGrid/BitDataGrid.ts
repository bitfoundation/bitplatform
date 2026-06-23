namespace BitBlazorUI {
    export class DataGrid {
        // Infinite scrolling is the one feature that genuinely needs to read scroll
        // position (which Blazor's scroll EventArgs do not expose), so this watches
        // the viewport and notifies .NET when the user nears the end.
        public static initInfiniteScroll(viewport: HTMLElement, dotNetRef: DotNetObject, threshold: number) {
            const distance = threshold ?? 200;
            let ticking = false;
            let disposed = false;

            const check = () => {
                ticking = false;
                if (disposed || !viewport) return;
                const remaining = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
                if (remaining <= distance) {
                    // The circuit may disconnect (navigation, refresh) between the disposed check and
                    // this async call, so swallow the resulting rejection to avoid unhandled console errors.
                    dotNetRef.invokeMethodAsync('OnInfiniteScrollNearEndAsync').catch(() => { });
                }
            };

            const onScroll = () => {
                if (!ticking) {
                    ticking = true;
                    requestAnimationFrame(check);
                }
            };

            viewport.addEventListener('scroll', onScroll, { passive: true });
            // Initial check so a first batch that doesn't fill the viewport keeps loading.
            setTimeout(check, 0);

            return {
                check: () => check(),
                scrollToTop: () => { if (viewport) viewport.scrollTop = 0; },
                dispose: () => { disposed = true; viewport.removeEventListener('scroll', onScroll); }
            };
        }
    }
}
