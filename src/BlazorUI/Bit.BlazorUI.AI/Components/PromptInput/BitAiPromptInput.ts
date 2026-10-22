namespace BitBlazorUI {
    // The browser side of BitAiPromptInput: what the browser will not give a .NET component for free - the height of its
    // textarea, the caret, a file pasted or dropped onto the composer, an Enter that sends rather than breaking the line,
    // and a draft that outlives a reload. Every helper fails quietly, since a composer has to keep working when a permission
    // is denied or an API is missing. What one registers on an element is kept by the id of the component that asked for
    // it and released by that id, so nothing here outlives the composer it was attached to without .NET asking for it.
    export class AiPromptInput {
        private static _pastes = new Map<string, () => void>();
        private static _drops = new Map<string, () => void>();
        private static _submitKeys = new Map<string, () => void>();
        private static _previews = new Set<string>();

        // The keys that drive the command and mention menu while it is open, which must not also move the caret or the focus.
        private static readonly MENU_KEYS = ['ArrowDown', 'ArrowUp', 'Enter', 'Tab', 'Escape'];

        // Session storage rather than local storage, so a draft never follows the user to another tab or another day.
        private static readonly DRAFT_PREFIX = 'bit-ai-draft:';

        // Grows the textarea to fit its content up to a row cap, then lets it scroll.
        public static autoResize(textarea: HTMLTextAreaElement | null, maxRows: number) {
            if (!textarea) return 0;

            const style = getComputedStyle(textarea);
            const lineHeight = parseFloat(style.lineHeight) || 20;
            const padding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom);
            const border = parseFloat(style.borderTopWidth) + parseFloat(style.borderBottomWidth);

            textarea.style.height = 'auto';
            const max = lineHeight * Math.max(1, maxRows || 1) + padding + border;
            const height = Math.min(textarea.scrollHeight + border, max);
            textarea.style.height = `${height}px`;
            textarea.style.overflowY = textarea.scrollHeight + border > max ? 'auto' : 'hidden';

            return height;
        }

        public static focus(element: HTMLElement | null) {
            element?.focus({ preventScroll: true });
        }

        // Clicks an element on behalf of .NET: how a real button opens the visually hidden file input.
        public static click(element: HTMLElement | null) {
            if (!element || typeof element.click !== 'function') return;

            element.click();
        }

        // A thumbnail URL for a file chosen through the file input, found by its position and checked by its name.
        public static previewFromInput(input: HTMLInputElement | null, index: number, name: string): string | null {
            const files = input?.files;
            if (!files || files.length === 0) return null;

            let file: File | undefined = files[index];
            if (!file || (name && file.name !== name)) {
                file = Array.from(files).find(f => f.name === name);
            }

            return file ? AiPromptInput.createPreview(file) : null;
        }

        public static revokePreview(url: string) {
            if (!url || !AiPromptInput._previews.has(url)) return;

            AiPromptInput._previews.delete(url);

            try {
                URL.revokeObjectURL(url);
            } catch {
                // already revoked
            }
        }

        // Reports a file pasted into the composer - a screenshot on the clipboard becomes an attachment. A paste of text
        // is left to the browser. A disabled composer refuses the file: nothing is read and nothing reaches .NET.
        public static registerPaste(id: string, element: HTMLElement | null, dotnetObj: DotNetObject, maxSize: number, disabled: boolean) {
            AiPromptInput.unregisterPaste(id);
            if (!element) return;

            const handler = async (e: ClipboardEvent) => {
                const items = Array.from(e.clipboardData?.items ?? []);
                const files = items.filter(i => i.kind === 'file').map(i => i.getAsFile()).filter((f): f is File => !!f);
                if (files.length === 0) return;

                e.preventDefault();

                if (disabled) return;

                for (const file of files) {
                    await dotnetObj.invokeMethodAsync('OnPastedFile', AiPromptInput.describeFile(file, 'pasted-image.png', maxSize))
                                   .catch(() => { });
                }
            };

            element.addEventListener('paste', handler);
            AiPromptInput._pastes.set(id, () => element.removeEventListener('paste', handler));
        }

        public static unregisterPaste(id: string) {
            AiPromptInput.release(AiPromptInput._pastes, id);
        }

        // Reports the files dropped onto the composer, and tells .NET while a drag of files is over it. A disabled composer
        // refuses a drag of files instead: the pointer says nothing can be dropped there, no overlay is raised, and a drop
        // that happens anyway is swallowed rather than staged - or left to the browser, which would open the file in place
        // of the page.
        public static registerDrop(id: string, element: HTMLElement | null, dotnetObj: DotNetObject, maxSize: number, disabled: boolean) {
            AiPromptInput.unregisterDrop(id);
            if (!element) return;

            let depth = 0;

            const setActive = (active: boolean) => {
                dotnetObj.invokeMethodAsync('OnDragStateChanged', active).catch(() => { });
            };

            const refuse = (e: DragEvent) => {
                e.preventDefault();
                if (e.dataTransfer) e.dataTransfer.dropEffect = 'none';
            };

            const onEnter = (e: DragEvent) => {
                if (!AiPromptInput.hasFiles(e)) return;

                if (disabled) {
                    refuse(e);
                    return;
                }

                e.preventDefault();
                depth++;
                if (depth === 1) setActive(true);
            };

            const onOver = (e: DragEvent) => {
                if (!AiPromptInput.hasFiles(e)) return;

                if (disabled) {
                    refuse(e);
                } else {
                    e.preventDefault();
                }
            };

            // Only a drag that was counted on the way in is counted on the way out, or dragging text across the
            // composer would leave the depth negative and a later drag of files unannounced.
            const onLeave = (e: DragEvent) => {
                if (!AiPromptInput.hasFiles(e) || depth === 0) return;

                depth--;
                if (depth === 0) setActive(false);
            };

            const onDrop = async (e: DragEvent) => {
                const files = e.dataTransfer?.files;
                if (!files || files.length === 0) return;

                e.preventDefault();

                if (disabled) return;

                depth = 0;
                setActive(false);

                for (const file of Array.from(files)) {
                    await dotnetObj.invokeMethodAsync('OnDroppedFile', AiPromptInput.describeFile(file, 'file', maxSize))
                                   .catch(() => { });
                }
            };

            // A drag abandoned outside the window never fires a dragleave on the element.
            const onEnd = () => {
                if (depth === 0) return;

                depth = 0;
                setActive(false);
            };

            element.addEventListener('dragenter', onEnter);
            element.addEventListener('dragover', onOver);
            element.addEventListener('dragleave', onLeave);
            element.addEventListener('drop', onDrop);
            window.addEventListener('dragend', onEnd);
            window.addEventListener('drop', onEnd);

            AiPromptInput._drops.set(id, () => {
                element.removeEventListener('dragenter', onEnter);
                element.removeEventListener('dragover', onOver);
                element.removeEventListener('dragleave', onLeave);
                element.removeEventListener('drop', onDrop);
                window.removeEventListener('dragend', onEnd);
                window.removeEventListener('drop', onEnd);
            });
        }

        public static unregisterDrop(id: string) {
            AiPromptInput.release(AiPromptInput._drops, id);
        }

        // The caret position, so the composer can open its menu where the user is typing.
        public static getCaret(element: HTMLTextAreaElement | null): number | null {
            if (!element || typeof element.selectionStart !== 'number') return null;

            return element.selectionStart;
        }

        // Focuses the textarea and puts the caret at a position, after .NET has rewritten its text.
        public static setCaret(element: HTMLTextAreaElement | null, position: number) {
            if (!element) return;

            element.focus({ preventScroll: true });

            if (typeof element.setSelectionRange !== 'function') return;

            const at = Math.max(0, Math.min(position, element.value.length));
            element.setSelectionRange(at, at);
        }

        public static readDraft(key: string): string | null {
            if (!key) return null;

            try {
                return sessionStorage.getItem(AiPromptInput.DRAFT_PREFIX + key);
            } catch {
                return null;
            }
        }

        public static writeDraft(key: string, text: string) {
            if (!key) return;

            try {
                if (text) {
                    sessionStorage.setItem(AiPromptInput.DRAFT_PREFIX + key, text);
                } else {
                    sessionStorage.removeItem(AiPromptInput.DRAFT_PREFIX + key);
                }
            } catch {
                // The storage is full or blocked; a draft is a convenience, not a record.
            }
        }

        public static clearDraft(key: string) {
            AiPromptInput.writeDraft(key, '');
        }

        // Keeps Enter from breaking the line when it is the submit key, and the keys that drive the command and mention
        // menu from also moving the caret or the focus while that menu is open. Blazor cannot decide preventDefault per
        // keystroke, so it is decided here, where the event is. Enter alone submits when submitOnEnter is set, Ctrl or Cmd
        // with Enter submits either way, and Shift or Alt with Enter is left to the browser, which breaks the line. A key
        // of an IME composition is the composition's own, never a submit. The menu marks the textarea with
        // data-bit-aipri-mnu while it is open.
        public static registerSubmitKey(id: string, element: HTMLElement | null, submitOnEnter: boolean) {
            AiPromptInput.unregisterSubmitKey(id);
            if (!element) return;

            const handler = (e: KeyboardEvent) => {
                if (Utils.isComposingKey(e)) return;

                if (element.hasAttribute('data-bit-aipri-mnu') && AiPromptInput.MENU_KEYS.indexOf(e.key) >= 0) {
                    e.preventDefault();
                    return;
                }

                if (e.key !== 'Enter' || e.shiftKey || e.altKey) return;
                if (!(e.ctrlKey || e.metaKey) && !submitOnEnter) return;

                e.preventDefault();
            };

            element.addEventListener('keydown', handler);
            AiPromptInput._submitKeys.set(id, () => element.removeEventListener('keydown', handler));
        }

        public static unregisterSubmitKey(id: string) {
            AiPromptInput.release(AiPromptInput._submitKeys, id);
        }

        private static release(registrations: Map<string, () => void>, id: string) {
            const dispose = registrations.get(id);
            if (!dispose) return;

            registrations.delete(id);

            try {
                dispose();
            } catch {
                // a listener of a detached element is already gone
            }
        }

        private static hasFiles(e: DragEvent) {
            return Array.from(e.dataTransfer?.types ?? []).indexOf('Files') >= 0;
        }

        // A short object URL for an image, so the composer shows a thumbnail without a data URL: a data URL puts the whole
        // image into the render tree, and over the circuit with every diff that touches it, while an object URL is a few
        // dozen characters and the bytes never leave the browser. revokePreview releases it with the attachment.
        private static createPreview(file: File): string | null {
            if (!file || !file.type || !file.type.startsWith('image/')) return null;

            try {
                const url = URL.createObjectURL(file);
                AiPromptInput._previews.add(url);
                return url;
            } catch {
                return null;
            }
        }

        // Describes a file to .NET with its content as a stream rather than inline: the bytes in the interop message itself
        // would exceed the SignalR limit of Blazor Server (32 KB by default) for anything but a tiny file, which would then
        // be dropped without a word, while a stream reference is read in chunks on Server and WebAssembly alike. A file
        // over the limit is still described, without its content, so .NET can tell the user why it was turned away; the
        // check is made here, before anything is read.
        private static describeFile(file: File, fallbackName: string, maxSize: number) {
            const withinLimit = !(maxSize > 0) || file.size <= maxSize;

            return {
                name: file.name || fallbackName,
                contentType: file.type || 'application/octet-stream',
                size: file.size,
                content: withinLimit ? (window as any).DotNet.createJSStreamReference(file) : null,
                previewUrl: withinLimit ? AiPromptInput.createPreview(file) : null,
            };
        }
    }
}

// bit-lazy: teardown AiPromptInput.unregisterPaste AiPromptInput.unregisterDrop AiPromptInput.unregisterSubmitKey AiPromptInput.revokePreview
