var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Answers "was this key pressed while the user was typing?" for the keyboard shortcuts and for the
    // keyboard event payload. Its own module because those two are the only callers: parked in utils it
    // would be downloaded by every app, and parked in events it would drag that module into every app
    // that registers a shortcut.
    butil.editableTarget = {
        of,
        isEditable
    };

    // The input types a keystroke edits nothing in - Delete or Backspace on a checkbox has no text to
    // remove. Every other type (text, search, email, number, the date and time pickers, ...) takes typing,
    // and an unknown type reads back as "text", so listing the exceptions is the shorter and safer list.
    const nonTextInputTypes = ['button', 'checkbox', 'color', 'file', 'hidden', 'image', 'radio', 'range', 'reset', 'submit'];

    // event.target is retargeted to the host at every shadow boundary, so an input inside a web component
    // would otherwise read as the component element itself. The first entry of the composed path is the
    // element the key actually went to (as far as an open shadow root lets the listener see).
    function of(e: Event): any {
        return e.composedPath?.()[0] ?? e.target;
    }

    function isEditable(target: any): boolean {
        if (!target || target.nodeType !== 1) return false;
        if (target.isContentEditable) return true;
        switch (target.tagName) {
            case 'INPUT':
                return !target.readOnly && !nonTextInputTypes.includes(target.type);
            case 'TEXTAREA':
                return !target.readOnly;
            case 'SELECT':
                return true;
            default:
                return false;
        }
    }
}(BitButil));
