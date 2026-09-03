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
/// <c>--bit-bp-*</c> variable is unset). Range members are half-open: the upper bound sits a
/// hundredth of a pixel below the next breakpoint, which is close enough that no width falls
/// between two neighboring members - a viewport is not always a whole number of CSS pixels, so a
/// whole-pixel bound would leave 959.5px matching neither Sm nor Md. The <c>*To*</c> members span from the start of the first named
/// breakpoint through the end of the second (both inclusive); a span starting at Xs or ending at
/// Xxl is one of the <c>Lt*</c> / <c>Gt*</c> members instead. For a one-off breakpoint that isn't
/// part of the theme scale, use <see cref="BitMediaQuery.Query"/> with an explicit query string
/// instead.
/// </remarks>
public enum BitScreenQuery
{
    /// <summary>
    /// Extra small query: [@media screen and (max-width: 599.98px)]
    /// </summary>
    Xs,

    /// <summary>
    /// Small query: [@media screen and (min-width: 600px) and (max-width: 959.98px)]
    /// </summary>
    Sm,

    /// <summary>
    /// Medium query: [@media screen and (min-width: 960px) and (max-width: 1279.98px)]
    /// </summary>
    Md,

    /// <summary>
    /// Large query: [@media screen and (min-width: 1280px) and (max-width: 1919.98px)]
    /// </summary>
    Lg,

    /// <summary>
    /// Extra large query: [@media screen and (min-width: 1920px) and (max-width: 2559.98px)]
    /// </summary>
    Xl,

    /// <summary>
    /// Extra extra large query: [@media screen and (min-width: 2560px)]
    /// </summary>
    Xxl,

    /// <summary>
    /// Less than small query: [@media screen and (max-width: 599.98px)]
    /// </summary>
    LtSm,

    /// <summary>
    /// Less than medium query: [@media screen and (max-width: 959.98px)]
    /// </summary>
    LtMd,

    /// <summary>
    /// Less than large query: [@media screen and (max-width: 1279.98px)]
    /// </summary>
    LtLg,

    /// <summary>
    /// Less than extra large query: [@media screen and (max-width: 1919.98px)]
    /// </summary>
    LtXl,

    /// <summary>
    /// Less than extra extra large query: [@media screen and (max-width: 2559.98px)]
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
    GtXl,

    /// <summary>
    /// Small through medium query: [@media screen and (min-width: 600px) and (max-width: 1279.98px)]
    /// </summary>
    SmToMd,

    /// <summary>
    /// Small through large query: [@media screen and (min-width: 600px) and (max-width: 1919.98px)]
    /// </summary>
    SmToLg,

    /// <summary>
    /// Small through extra large query: [@media screen and (min-width: 600px) and (max-width: 2559.98px)]
    /// </summary>
    SmToXl,

    /// <summary>
    /// Medium through large query: [@media screen and (min-width: 960px) and (max-width: 1919.98px)]
    /// </summary>
    MdToLg,

    /// <summary>
    /// Medium through extra large query: [@media screen and (min-width: 960px) and (max-width: 2559.98px)]
    /// </summary>
    MdToXl,

    /// <summary>
    /// Large through extra large query: [@media screen and (min-width: 1280px) and (max-width: 2559.98px)]
    /// </summary>
    LgToXl
}
