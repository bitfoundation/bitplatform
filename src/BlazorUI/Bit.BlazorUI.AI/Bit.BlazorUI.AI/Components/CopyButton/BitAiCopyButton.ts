namespace BitBlazorUI {
    export class AiCopyButton {
        // Puts the text on the clipboard and answers whether it got there, so the button only confirms a copy that
        // happened. The async Clipboard API is unavailable outside a secure context and in a few older browsers, so a
        // hidden textarea and the deprecated copy command stand in for it there - and wherever the API refuses (a
        // permission denied) - rather than leaving the copy silently undone.
        public static async copy(text: string): Promise<boolean> {
            text ??= '';

            try {
                if (navigator.clipboard && window.isSecureContext) {
                    await navigator.clipboard.writeText(text);
                    return true;
                }
            } catch {
                // Falls through to the legacy path below.
            }

            const area = document.createElement('textarea');
            area.value = text;
            area.setAttribute('readonly', '');
            area.setAttribute('aria-hidden', 'true');
            // Pinned to the top-left corner of the viewport rather than left to flow: a textarea selected while it sits
            // below the fold scrolls the page away from the button the copy belongs to.
            area.style.position = 'fixed';
            area.style.top = '0';
            area.style.left = '0';
            area.style.width = '1px';
            area.style.height = '1px';
            area.style.padding = '0';
            area.style.border = 'none';
            area.style.opacity = '0';
            area.style.pointerEvents = 'none';

            // Selecting the textarea takes the focus off the button that was pressed, so it is put back.
            const previouslyFocused = document.activeElement as HTMLElement | null;

            document.body.appendChild(area);

            try {
                area.select();
                return document.execCommand('copy');
            } catch {
                return false;
            } finally {
                area.remove();
                previouslyFocused?.focus?.({ preventScroll: true });
            }
        }
    }
}
