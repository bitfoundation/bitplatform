namespace Bit.BlazorUI;

/// <summary>
/// Default breakpoint widths (px) for <see cref="BitThemeLayout.Breakpoints"/>; align with your app media queries.
/// </summary>
/// <remarks>
/// These are the shared breakpoints of bit BlazorUI, written as CSS lengths. The components that resolve
/// one in C# read the same widths as numbers from BitBreakpoints, so the two have to be changed together.
/// </remarks>
public static class BitThemeBreakpointDefaults
{
    public const string Xs = "0px";
    public const string Sm = "600px";
    public const string Md = "960px";
    public const string Lg = "1280px";
    public const string Xl = "1920px";
    public const string Xxl = "2560px";
}
