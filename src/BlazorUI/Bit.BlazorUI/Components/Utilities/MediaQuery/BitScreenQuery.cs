namespace Bit.BlazorUI;

/// <summary>
/// The predefined screen media queries in the bit BlazorUI.
/// </summary>
public enum BitScreenQuery
{
    /// <summary>
    /// Extra small query: [@media screen and (max-width: 600px)]
    /// </summary>
    Xs,

    /// <summary>
    /// Small query: [@media screen and (min-width: 601px) and (max-width: 960px)]
    /// </summary>
    Sm,

    /// <summary>
    /// Medium query: [@media screen and (min-width: 961px) and (max-width: 1280px)]
    /// </summary>
    Md,

    /// <summary>
    /// Large query: [@media screen and (min-width: 1281px) and (max-width: 1920px)]
    /// </summary>
    Lg,

    /// <summary>
    /// Extra large query: [@media screen and (min-width: 1921px) and (max-width: 2560px)]
    /// </summary>
    Xl,

    /// <summary>
    /// Extra extra large query: [@media screen and (min-width: 2561px)]
    /// </summary>
    Xxl,

    /// <summary>
    /// Less than small query: [@media screen and (max-width: 600px)]
    /// </summary>
    LtSm,

    /// <summary>
    /// Less than medium query: [@media screen and (max-width: 960px)]
    /// </summary>
    LtMd,

    /// <summary>
    /// Less than large query: [@media screen and (max-width: 1280px)]
    /// </summary>
    LtLg,

    /// <summary>
    /// Less than extra large query: [@media screen and (max-width: 1920px)]
    /// </summary>
    LtXl,

    /// <summary>
    /// Less than extra extra large query: [@media screen and (max-width: 2560px)]
    /// </summary>
    LtXxl,

    /// <summary>
    /// Greater than extra small query: [@media screen and (min-width: 601px)]
    /// </summary>
    GtXs,

    /// <summary>
    /// Greater than small query: [@media screen and (min-width: 961px)]
    /// </summary>
    GtSm,

    /// <summary>
    /// Greater than medium query: [@media screen and (min-width: 1281px)]
    /// </summary>
    GtMd,

    /// <summary>
    /// Greater than large query: [@media screen and (min-width: 1921px)]
    /// </summary>
    GtLg,

    /// <summary>
    /// Greater than extra large query: [@media screen and (min-width: 2561px)]
    /// </summary>
    GtXl
}
