namespace Bit.BlazorUI;

/// <summary>
/// The light / dark pair a server-rendered response resolves its theme against
/// (<see cref="BitThemeSsr.ResolveThemePair"/>).
/// </summary>
/// <param name="Light">The normalized light theme.</param>
/// <param name="Dark">The normalized dark theme.</param>
/// <param name="IsPersisted">Whether it is the visitor's stored pair rather than the one the page declares.</param>
/// <param name="StoredScheme">
/// <c>light</c> / <c>dark</c> when the stored preference is that theme of the declared pair, so it follows the
/// pair; <see langword="null"/> otherwise, and always alongside a stored pair.
/// </param>
internal readonly record struct BitThemeSsrPair(string Light, string Dark, bool IsPersisted, string? StoredScheme);
