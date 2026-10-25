using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// What the E2E tests of the Buttons category share: the BitColor and BitSize tables in the enums' order with the
/// class suffixes the components derive from them (BitCssClasses) and the theme tokens each role paints from, and
/// a few readers of the live page - a color or a length resolved the way the stylesheet resolves it, a computed
/// style of a located element, its box.
/// </summary>
internal static class ButtonsE2EHelpers
{
    /// <summary>
    /// Matches any value of an attribute, so <c>Not.ToHaveAttributeAsync(name, AnyValue)</c> asserts it is absent.
    /// </summary>
    public static readonly Regex AnyValue = new(".*");

    /// <summary>
    /// Every BitColor in the enum's order: its name, the class suffix (BitCssClasses.Color), the theme token of the
    /// role's main color (<c>--bit-clr-{token}</c>) and the token of the color painted on it.
    /// </summary>
    public static readonly (string Name, string Suffix, string Token, string OnToken)[] Colors =
    [
        ("Primary", "pri", "pri", "pri-text"),
        ("Secondary", "sec", "sec", "sec-text"),
        ("Tertiary", "ter", "ter", "ter-text"),
        ("Info", "inf", "inf", "inf-text"),
        ("Success", "suc", "suc", "suc-text"),
        ("Warning", "wrn", "wrn", "wrn-text"),
        ("SevereWarning", "swr", "swr", "swr-text"),
        ("Error", "err", "err", "err-text"),
        ("PrimaryBackground", "pbg", "bg-pri", "fg-pri"),
        ("SecondaryBackground", "sbg", "bg-sec", "fg-pri"),
        ("TertiaryBackground", "tbg", "bg-ter", "fg-pri"),
        ("PrimaryForeground", "pfg", "fg-pri", "bg-pri"),
        ("SecondaryForeground", "sfg", "fg-sec", "bg-pri"),
        ("TertiaryForeground", "tfg", "fg-ter", "bg-pri"),
        ("PrimaryBorder", "pbr", "brd-pri", "fg-pri"),
        ("SecondaryBorder", "sbr", "brd-sec", "fg-pri"),
        ("TertiaryBorder", "tbr", "brd-ter", "fg-pri"),
    ];

    /// <summary>
    /// The rows of <see cref="Colors"/> for a DynamicData test.
    /// </summary>
    public static IEnumerable<object[]> ColorRows => Colors.Select(c => new object[] { c.Name, c.Suffix, c.Token, c.OnToken });

    /// <summary>
    /// Every BitSize in the enum's order: its name, the class suffix (BitCssClasses.Size) and the suffix of the
    /// global size tokens of that class (<c>--bit-siz-ctrl-{token}</c>, <c>--bit-siz-icon-{token}</c>).
    /// </summary>
    public static readonly (string Name, string Suffix)[] Sizes =
    [
        ("Small", "sm"),
        ("Medium", "md"),
        ("Large", "lg"),
    ];

    /// <summary>
    /// The control height of a size class, as the stylesheet resolves it through the density-aware alias.
    /// </summary>
    public static string ControlHeight(string sizeSuffix)
        => $"var(--bit-siz-ctrl-{sizeSuffix}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var(--bit-siz-ctrl-{sizeSuffix}-steps)))";

    /// <summary>
    /// The stylesheet's spacing(n), the rhythm unit times the density scale times n.
    /// </summary>
    public static string Spacing(double steps)
        => $"calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * {steps.ToString(CultureInfo.InvariantCulture)})";

    /// <summary>
    /// Resolves a CSS color (a keyword, a var() of a theme token) to the rgb() a computed style reports, by painting
    /// it on a probe element appended to the given parent (the body by default) so it inherits what that one does.
    /// </summary>
    public static Task<string> ResolveColorAsync(this IPage page, string cssColor, string? parentSelector = null)
    {
        return page.EvaluateAsync<string>(
            """
            ([value, parent]) => {
                const host = parent ? document.querySelector(parent) : document.body;
                const probe = document.createElement('div');
                probe.style.color = value;
                host.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, new object?[] { cssColor, parentSelector });
    }

    /// <summary>
    /// Resolves a CSS length (a var() of a theme token, a calc()) to pixels, through the width of a probe element.
    /// </summary>
    public static async Task<double> ResolvePxAsync(this IPage page, string cssLength, string? parentSelector = null)
    {
        var px = await page.EvaluateAsync<string>(
            """
            ([value, parent]) => {
                const host = parent ? document.querySelector(parent) : document.body;
                const probe = document.createElement('div');
                probe.style.position = 'absolute';
                probe.style.boxSizing = 'content-box';
                probe.style.width = value;
                host.appendChild(probe);
                const resolved = getComputedStyle(probe).width;
                probe.remove();
                return resolved;
            }
            """, new object?[] { cssLength, parentSelector });

        return ParsePx(px);
    }

    /// <summary>
    /// A computed style of the located element.
    /// </summary>
    public static Task<string> StyleAsync(this ILocator locator, string property)
    {
        return locator.EvaluateAsync<string>("(el, p) => getComputedStyle(el).getPropertyValue(p)", property);
    }

    /// <summary>
    /// A computed style of the located element in pixels.
    /// </summary>
    public static async Task<double> PxAsync(this ILocator locator, string property)
    {
        return ParsePx(await locator.StyleAsync(property));
    }

    /// <summary>
    /// The box of the located element, which has to be rendered.
    /// </summary>
    public static async Task<LocatorBoundingBoxResult> BoxAsync(this ILocator locator)
    {
        return await locator.BoundingBoxAsync() ?? throw new AssertFailedException("The element has no box.");
    }

    /// <summary>
    /// Whether the located element carries the attribute, whatever its value.
    /// </summary>
    public static Task<bool> HasAttributeAsync(this ILocator locator, string name)
    {
        return locator.EvaluateAsync<bool>("(el, n) => el.hasAttribute(n)", name);
    }

    /// <summary>
    /// Parses the leading number of a CSS length ("12.5px" is 12.5).
    /// </summary>
    public static double ParsePx(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"-?\d+(\.\d+)?(e-?\d+)?");

        return match.Success ? double.Parse(match.Value, CultureInfo.InvariantCulture) : double.NaN;
    }

    /// <summary>
    /// Parses the first duration of a computed transition- or animation-duration list into seconds.
    /// </summary>
    public static double ParseSeconds(string value)
    {
        var first = (value ?? string.Empty).Split(',')[0].Trim();

        if (first.EndsWith("ms", StringComparison.Ordinal))
        {
            return double.Parse(first[..^2], NumberStyles.Float, CultureInfo.InvariantCulture) / 1000;
        }

        return double.Parse(first.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Asserts that two lengths agree to within the given tolerance.
    /// </summary>
    public static void AreClose(double expected, double actual, double tolerance, string message)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= tolerance, $"{message}: expected {expected}, was {actual} (tolerance {tolerance}).");
    }
}
