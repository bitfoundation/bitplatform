//+:cnd:noEmit

// The following code gives you ideas on how to handle bit theme changes in your application.

(function () {
    if (typeof BitTheme === 'undefined')
        return;
    BitTheme.init({
        system: true,
        persist: true,
        onChange: (newTheme, oldTheme) => {
            document.body.classList.add('theme-' + newTheme);
            document.body.classList.remove('theme-' + oldTheme);

            const primaryBgColor = getComputedStyle(document.documentElement).getPropertyValue('--bit-clr-bg-pri');
            if (!primaryBgColor) return;

            document.querySelector('meta[name=theme-color]')?.setAttribute('content', primaryBgColor);
        }
    });
}());
