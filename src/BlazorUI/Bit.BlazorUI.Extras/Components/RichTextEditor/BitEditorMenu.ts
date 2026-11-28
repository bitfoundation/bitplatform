namespace BitBlazorUI {
    // bit-trim: on-demand
    // The menus an editor opens at a trigger in its text (_BitEditorMenu.razor): BitRichTextEditor's slash commands and
    // mentions. The menu's filter box handles its navigation keys in .NET, which hears them too late to keep the browser
    // from acting on them, so their defaults are prevented here.

    export class EditorMenu {
        public static bindKeys(input: any) {
            if (!input || input._editorMenuKeysBound) return;
            input._editorMenuKeysBound = true;
            input.addEventListener('keydown', (e: KeyboardEvent) => {
                // Escape is not among them: the input claims the plain one (data-bit-esc, see Utils.claimEscape),
                // which prevents its default, and one with a modifier goes on to a dialog the editor sits in.
                if (e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'Enter') {
                    e.preventDefault();
                }
            });
        }
    }
}
