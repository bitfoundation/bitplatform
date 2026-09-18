//+:cnd:noEmit

// The following code gives you ideas on how to handle bit theme changes in your application.

(function () {
    if (typeof BitBlazorUI === 'undefined')
        return;
    BitBlazorUI.Theme.init({
        system: true,
        persist: true,
        onChange: (newTheme, oldTheme) => {
            document.body.classList.add('theme-' + newTheme);
            document.body.classList.remove('theme-' + oldTheme);
        }
    });

    // Keeps the browser chrome (the status bar of an installed PWA, the mobile address bar) painted
    // with the page's own background. The library does this itself from the bit-theme-color-meta
    // attribute on <html>, which is the shorter way to write everything below - but only from the
    // release AFTER 10.6.0, the version this template references, so it is done here for now.
    //
    // The color cannot be read inside onChange: with bit-theme-view-transition the bit-theme
    // attribute is only written a frame after the callback, and a picked accent re-derives the whole
    // palette - backgrounds included - as inline --bit-* variables on <body>, applied later still
    // from .NET, or from the style#bit-accent-css snapshot. So it is re-read from <body> (where
    // those overrides live) whenever any of these inputs change.
    let pending = false;
    const sync = () => {
        pending = false;
        const color = getComputedStyle(document.body).getPropertyValue('--bit-clr-bg-pri').trim();
        if (!color) return;
        document.querySelectorAll('meta[name=theme-color]')
            .forEach(meta => meta.getAttribute('content') !== color && meta.setAttribute('content', color));
    };
    const schedule = () => {
        if (pending) return;
        pending = true;
        requestAnimationFrame(sync);
    };

    const observer = new MutationObserver(schedule);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['bit-theme', 'bit-accent'] });
    observer.observe(document.body, { attributes: true, attributeFilter: ['style', 'class'] });
    observer.observe(document.head, { childList: true });
    schedule();
}());
