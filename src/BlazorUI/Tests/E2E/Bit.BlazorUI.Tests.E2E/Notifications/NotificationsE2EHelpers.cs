namespace Bit.BlazorUI.Tests.E2E.Notifications;

/// <summary>
/// The measurements the Notifications E2E tests share: an element's box, the color a theme token resolves to, and a
/// wait for every finite animation of the page to have run, so a box is measured once a scale-in has landed.
/// </summary>
internal static class NotificationsE2EHelpers
{
    /// <summary>
    /// The CSS suffix and the theme token of every BitColor, in the enum's order (BitCssClasses.Color).
    /// </summary>
    public static readonly (string Name, string Suffix, string Token)[] Colors =
    [
        ("Primary", "pri", "--bit-clr-pri"),
        ("Secondary", "sec", "--bit-clr-sec"),
        ("Tertiary", "ter", "--bit-clr-ter"),
        ("Info", "inf", "--bit-clr-inf"),
        ("Success", "suc", "--bit-clr-suc"),
        ("Warning", "wrn", "--bit-clr-wrn"),
        ("SevereWarning", "swr", "--bit-clr-swr"),
        ("Error", "err", "--bit-clr-err"),
        ("PrimaryBackground", "pbg", "--bit-clr-bg-pri"),
        ("SecondaryBackground", "sbg", "--bit-clr-bg-sec"),
        ("TertiaryBackground", "tbg", "--bit-clr-bg-ter"),
        ("PrimaryForeground", "pfg", "--bit-clr-fg-pri"),
        ("SecondaryForeground", "sfg", "--bit-clr-fg-sec"),
        ("TertiaryForeground", "tfg", "--bit-clr-fg-ter"),
        ("PrimaryBorder", "pbr", "--bit-clr-brd-pri"),
        ("SecondaryBorder", "sbr", "--bit-clr-brd-sec"),
        ("TertiaryBorder", "tbr", "--bit-clr-brd-ter"),
    ];

    /// <summary>
    /// The box of an element in the viewport.
    /// </summary>
    public static async Task<Box> BoxAsync(this IPage page, string selector)
    {
        var r = await page.Locator(selector).First.EvaluateAsync<double[]>(
            "el => { const r = el.getBoundingClientRect(); return [r.left, r.top, r.right, r.bottom]; }");

        return new(r[0], r[1], r[2], r[3]);
    }

    /// <summary>
    /// The box of an element in the viewport.
    /// </summary>
    public static async Task<Box> BoxAsync(this ILocator locator)
    {
        var r = await locator.EvaluateAsync<double[]>(
            "el => { const r = el.getBoundingClientRect(); return [r.left, r.top, r.right, r.bottom]; }");

        return new(r[0], r[1], r[2], r[3]);
    }

    /// <summary>
    /// Waits until every animation of the page that ends has ended (an entry scale-in, a bump), so what is measured
    /// afterwards is the resting layout. Looping ones (a pulse, a spinner) are left running.
    /// </summary>
    public static Task SettleAnimationsAsync(this IPage page)
    {
        return page.EvaluateAsync(
            "() => Promise.all(document.getAnimations().filter(a => a.effect && a.effect.getComputedTiming().iterations !== Infinity).map(a => a.finished.catch(() => null)))");
    }

    /// <summary>
    /// The color a theme token resolves to, as getComputedStyle reports colors (rgb()/rgba()).
    /// </summary>
    public static Task<string> ResolveColorAsync(this IPage page, string token, string property = "background-color")
    {
        return page.EvaluateAsync<string>(
            @"([token, property]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, `var(${token})`);
                document.body.appendChild(probe);
                const color = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return color;
            }",
            new[] { token, property });
    }

    /// <summary>
    /// The color a raw CSS value (a var(), a color-mix()) resolves to, as getComputedStyle reports colors.
    /// </summary>
    public static Task<string> ResolveCssColorAsync(this IPage page, string value, string property = "background-color")
    {
        return page.EvaluateAsync<string>(
            @"([value, property]) => {
                const probe = document.createElement('div');
                probe.style.setProperty(property, value);
                document.body.appendChild(probe);
                const color = getComputedStyle(probe).getPropertyValue(property);
                probe.remove();
                return color;
            }",
            new[] { value, property });
    }

    /// <summary>
    /// Waits until the given time has passed in the page, for a test whose subject is that something does NOT happen
    /// while the user rests the pointer, keeps the focus or leaves the window (a held countdown): the dwell is the
    /// user's action, not a wait for the app.
    /// </summary>
    public static Task DwellAsync(this IPage page, int milliseconds)
    {
        return page.EvaluateAsync("ms => new Promise(resolve => setTimeout(resolve, ms))", milliseconds);
    }

    /// <summary>
    /// The computed value of a property of a pseudo-element of the first element the selector finds.
    /// </summary>
    public static Task<string> PseudoStyleAsync(this IPage page, string selector, string pseudo, string property)
    {
        return page.Locator(selector).First.EvaluateAsync<string>(
            "(el, [pseudo, property]) => getComputedStyle(el, pseudo).getPropertyValue(property)",
            new[] { pseudo, property });
    }

    /// <summary>
    /// The prerendered HTML of a page of the host, as the server sends it before any circuit exists.
    /// </summary>
    public static async Task<string> PrerenderedHtmlAsync(this IPage page, string url)
    {
        var response = await page.APIRequest.GetAsync(url);

        Assert.IsTrue(response.Ok, $"GET {url} answered {response.Status}.");

        return await response.TextAsync();
    }

    public static double Px(string cssLength) => double.Parse(cssLength.Replace("px", "").Trim(), System.Globalization.CultureInfo.InvariantCulture);
}

/// <summary>
/// An element's box in the viewport.
/// </summary>
internal readonly record struct Box(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;

    public double Height => Bottom - Top;

    public double CenterX => (Left + Right) / 2;

    public double CenterY => (Top + Bottom) / 2;
}
