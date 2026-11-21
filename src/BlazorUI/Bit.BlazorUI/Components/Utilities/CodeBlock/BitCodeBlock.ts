namespace BitBlazorUI {
    export class CodeBlock {
        // Puts the code on the clipboard and answers whether it got there, so the code block only confirms a copy that
        // happened. The text is the Code the component was given, or else the text the browser renders of its content.
        // A click on the code that ends a selection of a part of it is the reader picking out a line, not asking for a
        // copy of the whole, so it copies nothing.
        public static async copy(code: HTMLElement | null, text: string | null, fromClick: boolean): Promise<boolean> {
            if (fromClick && code && CodeBlock.hasSelectionIn(code)) return false;

            text ??= code?.textContent ?? '';

            // The async Clipboard API is unavailable outside a secure context and in a few older browsers, so a hidden
            // textarea and the deprecated copy command stand in for it there - and wherever the API refuses (a
            // permission denied) - rather than leaving the copy silently undone.
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
            // below the fold scrolls the page away from the code block the copy belongs to.
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

        private static hasSelectionIn(element: HTMLElement): boolean {
            const selection = window.getSelection();

            if (!selection || selection.isCollapsed || selection.rangeCount === 0) return false;

            const range = selection.getRangeAt(0);

            return element.contains(range.commonAncestorContainer) || range.intersectsNode(element);
        }
    }
}
