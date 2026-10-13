namespace Bit.BlazorUI;

/// <summary>
/// The light / dark pair a server-rendered response resolves its theme against
/// (<see cref="BitThemeSsr.ResolveThemePair"/>).
/// </summary>
/// <param name="Light">The normalized light theme.</param>
/// <param name="Dark">The normalized dark theme.</param>
/// <param name="IsPersisted">Whether it is the visitor's stored pair rather than the one the page declares.</param>
/// <param name="StoredScheme">
/// <c>light</c> / <c>dark</c> when the stored preference is that theme of the declared pair, or of a stored pair
/// set aside (one the page declares itself, or no longer offers), so it follows the declared pair;
/// <see langword="null"/> otherwise, and always alongside a stored pair in use.
/// </param>
/// <param name="Offered">
/// The themes the page offers - the <see cref="BitThemeSsrOptions.AvailableThemes"/> it lists, plus the declared
/// pair and <c>system</c>, which count as offered whether listed or not - or <see langword="null"/> when it lists
/// none, and every name is offered.
/// </param>
internal readonly record struct BitThemeSsrPair(string Light, string Dark, bool IsPersisted, string? StoredScheme,
    IReadOnlySet<string>? Offered = null)
{
    /// <summary>Whether the page offers <paramref name="theme"/> - the client's <c>Theme.isAvailable</c>.</summary>
    public bool Offers(string theme) => Offered is null || Offered.Contains(theme);
}
