// The clickable item of a BitTimeline is a role="button" element, so the page scroll that Space starts on it
// is suppressed here instead of with @onkeypress:preventDefault on the item: Blazor applies that flag to every
// keypress bubbling through the item, so it would also swallow what is typed into a control of a custom
// template. Only a key pressed on the item itself is taken; the activation stays in the Blazor keydown handler.
document.addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.key !== ' ' && e.key !== 'Spacebar') return;

    const target = e.target as HTMLElement | null;
    if (!target?.classList?.contains('bit-tln-btn')) return;

    e.preventDefault();
});
