//+:cnd:noEmit

// The following code gives you ideas on how to handle bit theme changes in your application.

(function () {
    if (typeof BitBlazorUI === 'undefined')
        return;
    BitBlazorUI.Theme.onChange((newTheme, oldTheme) => {
        document.body.classList.add('theme-' + newTheme);
        document.body.classList.remove('theme-' + oldTheme);
    });
}());
