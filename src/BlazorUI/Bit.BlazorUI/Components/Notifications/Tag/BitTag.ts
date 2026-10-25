// A dismissible BitTag removes itself on Delete and Backspace in its .NET keydown handler, which runs too late to prevent
// the default of the key, and a Blazor @onkeydown:preventDefault would hold for every key of the tag - Tab and Enter too.
// Backspace is still the Back navigation of WebKit, so pressing it to dismiss a tag would leave the page as well. The
// controls of the tag say in their markup when the handler acts on the two keys (data-bit-tag-dsm, rendered from the
// very state the handler decides on), and only then is their default prevented here, on the document like Blazor's own
// delegation, so a key the page stopped before it never reaches .NET and keeps its default either way.
// bit-trim: owners BitTag
document.addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.key !== 'Delete' && e.key !== 'Backspace') return;

    const target = e.target as Element | null;
    if (!target?.closest?.('[data-bit-tag-dsm]')) return;

    e.preventDefault();
});
