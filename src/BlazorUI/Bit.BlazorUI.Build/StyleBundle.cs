namespace Bit.BlazorUI.Build;

/// <summary>
/// A stylesheet of a package that is its own part, every component chunk of the package in order, and its own part again:
/// the part before the components file (the tokens, the utility classes) and the part after it (the icon font, the
/// global rules, the theme). build-assets.mjs checks that the three compile to exactly the whole.
/// </summary>
public sealed class StyleBundle(string bundle, string prefix, string suffix)
{
    /// <summary>The stylesheet's path under the package's wwwroot ("styles/bit.blazorui.css").</summary>
    public string Bundle { get; } = bundle;

    /// <summary>The part before the components, relative to the package's assets folder.</summary>
    public string Prefix { get; } = prefix;

    /// <summary>The part after the components, relative to the package's assets folder.</summary>
    public string Suffix { get; } = suffix;
}
