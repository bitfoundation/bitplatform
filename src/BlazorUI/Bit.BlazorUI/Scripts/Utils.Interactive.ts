namespace BitBlazorUI {
    export namespace Utils {

        // What the browser itself activates on a click - a link, a form control, a disclosure's summary, a media
        // player's controls, an editing host - and the ARIA widgets a component draws in its place. Only the first
        // summary of a details element is its disclosure control; any other summary is plain content.
        const interactiveSelector =
            'a[href], button, input, select, textarea, details > summary:first-of-type, ' +
            'audio[controls], video[controls], [contenteditable]:not([contenteditable="false"]), ' +
            '[role="button"], [role="link"], [role="checkbox"], [role="radio"], [role="switch"], ' +
            '[role="slider"], [role="spinbutton"], [role="textbox"], [role="searchbox"], [role="combobox"], ' +
            '[role="option"], [role="tab"], [role="treeitem"], ' +
            '[role="menuitem"], [role="menuitemcheckbox"], [role="menuitemradio"]';

        // Whether a pointer press on the element is the element's own - something the user reached for - rather than
        // one on the surface around it: a drag surface leaves it alone (DragDrop), and so do the cells and the rows of
        // a grid (BitDataGrid). It answers for the element alone; a caller walks the target's ancestors up to its own
        // boundary.
        // An element says so itself with data-bit-interactive, which wins over everything below: a control of the
        // page's own the rules cannot see (an icon with a Blazor @onclick, a widget drawn in a closed shadow root)
        // opts in with the bare attribute, and "false" opts a control out (a button whose click the row should take
        // too). Without it: what the selector lists, a label only while it has a control to hand the click to (a bare
        // label is text), and an element put in the tab sequence by hand - a widget of the page's own (a negative
        // tabindex only makes it focusable by script, the way a roving tabindex parks the items it is not on). This is
        // the library's one definition of it: every script asks it rather than keeping a list of its own that a fix
        // here would not reach - the scripts of Bit.BlazorUI.Extras through Scripts/BitBlazorUI.d.ts.
        export function isInteractive(el: Element) {
            const mark = el.getAttribute('data-bit-interactive');
            if (mark !== null) return mark !== 'false';

            if (el.matches(interactiveSelector)) return true;

            if (el.tagName === 'LABEL') return (el as HTMLLabelElement).control != null;

            const tabindex = el.getAttribute('tabindex');
            return tabindex !== null && parseInt(tabindex, 10) >= 0;
        }
    }
}
