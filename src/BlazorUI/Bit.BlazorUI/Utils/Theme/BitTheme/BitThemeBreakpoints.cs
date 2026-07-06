namespace Bit.BlazorUI;

/// <summary>
/// Responsive breakpoint tokens, emitted as the <c>--bit-bp-*</c> CSS custom properties.
/// </summary>
/// <remarks>
/// These tokens drive the predefined <see cref="BitScreenQuery"/> values: <see cref="BitMediaQuery"/>
/// resolves the live <c>--bit-bp-*</c> variables at runtime and builds its <c>matchMedia</c> query
/// from them, so overriding a breakpoint here changes the corresponding <see cref="BitScreenQuery"/>
/// behavior. They are also available to any consumer CSS/JS that reads <c>--bit-bp-*</c> directly.
/// Note that the packaged Sass <c>@media</c> rules still compile against fixed breakpoints — a CSS
/// <c>@media</c> query cannot read a custom property — so those specific stylesheet rules are not
/// affected by overrides set here.
/// </remarks>
public class BitThemeBreakpoints
{
    public string? Xs { get; set; }
    public string? Sm { get; set; }
    public string? Md { get; set; }
    public string? Lg { get; set; }
    public string? Xl { get; set; }
    public string? Xxl { get; set; }
}
