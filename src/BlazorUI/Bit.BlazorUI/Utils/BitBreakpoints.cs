namespace Bit.BlazorUI;

/// <summary>
/// The shared breakpoints of bit BlazorUI, in pixels, for the components that have to resolve one in C#.
/// </summary>
/// <remarks>
/// These are the same widths the stylesheets build their media queries from (the $mq-*-min variables of
/// media-queries.scss) and the same ones <see cref="BitThemeBreakpointDefaults"/> hands to a theme, so a
/// component that resolves a breakpoint itself lands on the same one the CSS does. Anything that changes
/// here has to change there as well.
/// </remarks>
internal static class BitBreakpoints
{
    public const double Xs = 0;
    public const double Sm = 600;
    public const double Md = 960;
    public const double Lg = 1280;
    public const double Xl = 1920;
    public const double Xxl = 2560;
}
