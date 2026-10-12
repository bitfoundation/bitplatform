namespace Bit.BlazorUI;

/// <summary>
/// The light / dark pair a server-rendered response resolves its theme against
/// (<see cref="BitThemeSsr.ResolveThemePair"/>).
/// </summary>
/// <param name="Light">The normalized light theme.</param>
/// <param name="Dark">The normalized dark theme.</param>
/// <param name="IsDeclared">Whether a pair is named at all - by the page or by the visitor - rather than the core default standing in.</param>
/// <param name="IsPersisted">Whether it is the visitor's stored pair rather than the one the page declares.</param>
internal readonly record struct BitThemeSsrPair(string Light, string Dark, bool IsDeclared, bool IsPersisted);
