var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Answers "does the element this key went to act on the key itself?" for the keyboard shortcuts and for the
    // keyboard event payload. Its own module because those two are the only callers: parked in utils it
    // would be downloaded by every app, and parked in events it would drag that module into every app
    // that registers a shortcut.
    butil.editableTarget = {
        of,
        takesKey
    };

    // The keys each kind of control acts on, by KeyboardEvent.code. Asked per key rather than per element, so a
    // shortcut stays live where the control has no use for its key - Delete on a select, an arrow on a checkbox -
    // and stands aside where it has one: an arrow on a slider or in a radio group.
    const navigation = 'Arrow(Left|Right|Up|Down)|Home|End|PageUp|PageDown';
    const keys: { [kind: string]: RegExp } = {
        // Everything that types a character, deletes one or moves the caret. Not Tab, Escape, the function keys
        // or the media keys: a text field leaves those to the page.
        text: new RegExp(`^(Key.|Digit.|Numpad.+|Intl.+|Space|Minus|Equal|Bracket(Left|Right)|Backslash|Semicolon|Quote|Backquote|Comma|Period|Slash|Backspace|Delete|Enter|${navigation})$`),
        // A select steps through its options, opens on Space or Enter, and jumps to an option by its first letters.
        select: new RegExp(`^(Key.|Digit.|Numpad\\d|Space|Enter|NumpadEnter|${navigation})$`),
        range: new RegExp(`^(${navigation})$`),
        radio: /^(Arrow(Left|Right|Up|Down)|Space)$/,
        checkbox: /^Space$/,
        // A color or file input opens its picker.
        picker: /^(Space|Enter|NumpadEnter)$/
    };

    // The roles a script-driven widget declares, read as the native control they stand for. A role is also all a
    // listener can learn about a control in a closed shadow root: nothing outside that root sees what has focus
    // inside it, so the key arrives with the host as its target. A Map rather than an object literal, so a role
    // attribute naming an Object.prototype member reads as no role.
    const roles = new Map<string, string>([
        ['textbox', 'text'], ['searchbox', 'text'], ['combobox', 'text'], ['spinbutton', 'text'],
        ['slider', 'range'], ['radio', 'radio'], ['checkbox', 'checkbox'], ['switch', 'checkbox']
    ]);

    // event.target is retargeted to the host at every shadow boundary, so an input inside a web component
    // would otherwise read as the component element itself. The first entry of the composed path is the
    // element the key actually went to, as far as an open shadow root lets the listener see.
    function of(e: Event): any {
        return e.composedPath?.()[0] ?? e.target;
    }

    function takesKey(target: any, e: KeyboardEvent): boolean {
        const kind = kindOf(target);
        if (!kind) return false;
        if (kind === 'text' || kind === 'line') {
            // An IME composing a word takes every key, Enter included - Enter is what commits the word.
            if (e.isComposing) return true;
            // Enter in a single-line field submits its form, and does nothing in a field without one.
            if (kind === 'line' && /Enter$/.test(e.code)) return !!target.form;
            return keys.text.test(e.code);
        }
        return keys[kind].test(e.code);
    }

    function kindOf(target: any): string | undefined {
        if (!target || target.nodeType !== 1) return undefined;
        // A host driven through the EditContext API (a canvas, say) takes typing without being contenteditable.
        if (target.isContentEditable || target.editContext) return 'text';
        // localName, not tagName: tagName keeps the document's case, so an XHTML page reports 'input'.
        switch (target.localName) {
            case 'input': return inputKind(target);
            case 'textarea': return target.readOnly ? undefined : 'text';
            case 'select': return 'select';
        }
        const role = roles.get((target.getAttribute('role') ?? '').trim().split(/\s+/)[0]);
        if (role) return role;
        // A form-associated custom element is a form control by its own declaration, and what it takes behind a
        // closed shadow root is out of sight - so it counts as a text field. Standing a shortcut aside wrongly
        // costs one keypress; firing it wrongly costs what the user was typing.
        return target.constructor?.formAssociated === true ? 'text' : undefined;
    }

    function inputKind(input: any): string | undefined {
        switch (input.type) {
            case 'range': return 'range';
            case 'radio': return 'radio';
            case 'checkbox': return 'checkbox';
            case 'color':
            case 'file': return 'picker';
            // A button holds no value for a key to change, and a hidden input never has focus.
            case 'button':
            case 'submit':
            case 'reset':
            case 'image':
            case 'hidden': return undefined;
            // text, search, email, number, the date and time pickers, ... - and an unknown type reads back as
            // "text", so the text fields are the default rather than a list. A read-only one has no caret to move
            // in Chromium, let alone text to change, so it acts on no key at all.
            default: return input.readOnly ? undefined : 'line';
        }
    }
}(BitButil));
