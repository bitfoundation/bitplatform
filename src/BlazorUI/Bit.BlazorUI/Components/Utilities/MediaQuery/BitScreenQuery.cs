namespace Bit.BlazorUI;

/// <summary>
/// The predefined screen media queries in the bit BlazorUI.
/// </summary>
/// <remarks>
/// When used via <see cref="BitMediaQuery.ScreenQuery"/>, the effective media query is built at
/// runtime from the live <c>--bit-bp-*</c> breakpoint tokens (i.e. from
/// <see cref="BitThemeLayout.Breakpoints"/> when you customize them), not from fixed values baked
/// into the component - so overriding the theme breakpoints changes these queries too. The pixel
/// values shown on each member below are the built-in <em>defaults</em> (used when the matching
/// <c>--bit-bp-*</c> variable is unset). Range members are half-open: the upper bound is one CSS
/// pixel below the next breakpoint. For a one-off breakpoint that isn't part of the theme scale,
/// use <see cref="BitMediaQuery.Query"/> with an explicit query string instead.
/// </remarks>
public enum BitScreenQuery
{
    /// <summary>
    /// Extra small query: [@media screen and (max-width: 599px)]
    /// </summary>
    Xs,

    /// <summary>
    /// Small query: [@media screen and (min-width: 600px) and (max-width: 959px)]
    /// </summary>
    Sm,

    /// <summary>
    /// Medium query: [@media screen and (min-width: 960px) and (max-width: 1279px)]
    /// </summary>
    Md,

    /// <summary>
    /// Large query: [@media screen and (min-width: 1280px) and (max-width: 1919px)]
    /// </summary>
    Lg,

    /// <summary>
    /// Extra large query: [@media screen and (min-width: 1920px) and (max-width: 2559px)]
    /// </summary>
    Xl,

    /// <summary>
    /// Extra extra large query: [@media screen and (min-width: 2560px)]
    /// </summary>
    Xxl,

    /// <summary>
    /// Less than small query: [@media screen and (max-width: 599px)]
    /// </summary>
    LtSm,

    /// <summary>
    /// Less than medium query: [@media screen and (max-width: 959px)]
    /// </summary>
    LtMd,

    /// <summary>
    /// Less than large query: [@media screen and (max-width: 1279px)]
    /// </summary>
    LtLg,

    /// <summary>
    /// Less than extra large query: [@media screen and (max-width: 1919px)]
    /// </summary>
    LtXl,

    /// <summary>
    /// Less than extra extra large query: [@media screen and (max-width: 2559px)]
    /// </summary>
    LtXxl,

    /// <summary>
    /// Greater than extra small query: [@media screen and (min-width: 600px)]
    /// </summary>
    GtXs,

    /// <summary>
    /// Greater than small query: [@media screen and (min-width: 960px)]
    /// </summary>
    GtSm,

    /// <summary>
    /// Greater than medium query: [@media screen and (min-width: 1280px)]
    /// </summary>
    GtMd,

    /// <summary>
    /// Greater than large query: [@media screen and (min-width: 1920px)]
    /// </summary>
    GtLg,

    /// <summary>
    /// Greater than extra large query: [@media screen and (min-width: 2560px)]
    /// </summary>
    GtXl
}
