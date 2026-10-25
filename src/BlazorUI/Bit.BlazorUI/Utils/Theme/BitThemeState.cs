namespace Bit.BlazorUI;

/// <summary>
/// What <see cref="BitThemeManager.GetThemeStateAsync"/> reads back from the theme script in one round trip.
/// </summary>
/// <param name="Theme">The applied <c>bit-theme</c>, or <see langword="null"/> when none is.</param>
/// <param name="Light">The light theme of the active pair - the visitor's, or else the page's.</param>
/// <param name="Dark">The dark theme of the active pair.</param>
/// <param name="ConfiguredLight">The light theme of the pair the page declares (<c>light</c> when it declares none).</param>
/// <param name="ConfiguredDark">The dark theme of the pair the page declares (<c>dark</c> when it declares none).</param>
internal sealed record BitThemeState(string? Theme, string Light, string Dark, string ConfiguredLight, string ConfiguredDark);
