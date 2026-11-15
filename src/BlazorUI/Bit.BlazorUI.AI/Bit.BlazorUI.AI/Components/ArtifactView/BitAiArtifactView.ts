namespace BitBlazorUI {
    export class AiArtifactView {
        private static _expanded = new Map<string, AbortController>();

        // Holds the keyboard inside the full-screen panel, as a modal dialog does: Tab and Shift+Tab cycle through it
        // (Utils.setupFocusTrap). Two keys never reach that trap, being pressed inside the page an HTML artifact runs in
        // its sandboxed frame, whose document hears them alone:
        // - the Tab out of the end of that page, which the browser answers by moving the focus to whatever follows the
        //   frame in the document - out of the panel, onto the page behind its overlay. The focus that lands outside the
        //   panel straight from one of its frames is sent back to the first control of the panel, where the trap would
        //   have wrapped it. Only that focus: the list of the version dropdown is rendered outside the panel too, and
        //   takes the focus from inside it as it opens.
        // - the Escape, which the panel's own script in the frame posts to this window once the page left it alone (see
        //   BitAiArtifactView.FrameDocument). A message is taken from a frame of this panel only, and reported through
        //   OnFrameEscape.
        public static expand(panelId: string, dotnetObj: DotNetObject) {
            AiArtifactView.collapse(panelId);

            const panel = document.getElementById(panelId);
            if (!panel) return;

            Utils.setupFocusTrap(panelId);

            const controller = new AbortController();
            const signal = controller.signal;

            const isOwnFrame = (el: Element | null): el is HTMLIFrameElement => el instanceof HTMLIFrameElement && panel.contains(el);

            // Whether one of the panel's frames held the focus last. The focus moving into a frame is a focus of the
            // frame element in this document, but not in every engine, so the window losing it to a frame says so too
            // - read a moment later, once the engine has made the frame the active element.
            let inFrame = false;

            // Firefox reports neither: the focus goes into a frame of another origin with no focusin on the frame and no
            // blur of the window. What it does report is the focus leaving the panel for nowhere in this document, and
            // coming back from nowhere (relatedTarget null) - which, from inside the panel, is a frame of it.
            let leftPanel = false;

            document.addEventListener('focusout', e => {
                leftPanel = panel.contains(e.target as Node) && e.relatedTarget === null;
            }, { signal, capture: true });

            document.addEventListener('focusin', e => {
                const target = e.target as HTMLElement;
                const fromFrame = inFrame || (leftPanel && e.relatedTarget === null);

                leftPanel = false;

                inFrame = isOwnFrame(target);

                if (fromFrame === false || panel.contains(target)) return;

                (Utils.firstFocusable(panel) ?? panel).focus();
            }, { signal, capture: true });

            window.addEventListener('blur', () => {
                setTimeout(() => {
                    if (signal.aborted) return;

                    inFrame = isOwnFrame(document.activeElement);
                });
            }, { signal });

            window.addEventListener('message', e => {
                if (e.data?.bitAiArtifactView !== 'escape') return;

                const frames = Array.from(panel.querySelectorAll('iframe'));
                if (frames.some(f => f.contentWindow === e.source) === false) return;

                // The panel may be disposed while the call is on its way, which is all the call was for.
                dotnetObj.invokeMethodAsync('OnFrameEscape').catch(() => { });
            }, { signal });

            AiArtifactView._expanded.set(panelId, controller);
        }

        public static collapse(panelId: string) {
            const controller = AiArtifactView._expanded.get(panelId);
            if (!controller) return;

            controller.abort();
            AiArtifactView._expanded.delete(panelId);

            Utils.disposeFocusTrap(panelId);
        }

        // Offers the text of an artifact to the user as a file. A blob behind an object URL rather than a data URL, so
        // an artifact of any length downloads whole, and the URL is revoked once the click has been dispatched - a
        // moment later, since revoking it at once cancels the download in some engines.
        public static downloadText(fileName: string, text: string, mimeType?: string) {
            const blob = new Blob([text], { type: mimeType || 'text/plain' });
            const url = URL.createObjectURL(blob);
            const anchor = document.createElement('a');
            anchor.href = url;
            anchor.download = fileName || 'artifact.txt';
            document.body.appendChild(anchor);
            anchor.click();
            document.body.removeChild(anchor);
            setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    }
}

// bit-lazy: teardown AiArtifactView.collapse
