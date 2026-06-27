namespace BitBlazorUI {
    export class DataGrid {
        // Infinite scrolling is the one feature that genuinely needs to read scroll
        // position (which Blazor's scroll EventArgs do not expose), so this watches
        // the viewport and notifies .NET when the user nears the end.
        public static initInfiniteScroll(viewport: HTMLElement, dotNetRef: DotNetObject, threshold: number) {
            const distance = threshold ?? 200;
            let ticking = false;
            let disposed = false;
            // Guards against firing OnInfiniteScrollNearEndAsync again while a prior invocation is still
            // in flight, which would otherwise overlap loads and duplicate interop on rapid scrolling.
            let pending = false;

            const check = () => {
                ticking = false;
                if (disposed || !viewport || pending) return;
                const remaining = viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
                if (remaining <= distance) {
                    pending = true;
                    // The circuit may disconnect (navigation, refresh) between the disposed check and
                    // this async call, so swallow the resulting rejection to avoid unhandled console errors.
                    // Only re-check once the load settles if the .NET callback reports more data was
                    // appended and remains; otherwise stop, so end-of-data (a no-op load) doesn't spin
                    // this check()->invoke->check() loop forever.
                    dotNetRef.invokeMethodAsync<boolean>('OnInfiniteScrollNearEndAsync')
                        .then(
                            (more) => { pending = false; if (!disposed && more) check(); },
                            () => { pending = false; }
                        );
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

        // Triggers a client-side file download for the given text content. Used by CSV export so the
        // (potentially large) CSV is generated only on demand instead of living in a DOM attribute and
        // being regenerated on every render. Uses a Blob + object URL to avoid data-URI length limits.
        public static download(fileName: string, content: string, mimeType: string) {
            const blob = new Blob([content], { type: mimeType || 'text/plain;charset=utf-8' });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName || 'download';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            // Revoke after the click has been dispatched so the download isn't cancelled prematurely.
            setTimeout(() => URL.revokeObjectURL(url), 0);
        }
    }
}
