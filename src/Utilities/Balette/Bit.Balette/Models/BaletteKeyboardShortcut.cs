using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bit.Balette;

/// <summary>
/// A keyboard shortcut: one or more chords pressed in sequence (<c>Mod+K</c>, <c>Ctrl+Shift+P</c>, <c>G I</c>).
/// <c>Mod</c> maps to ⌘ on macOS and Ctrl elsewhere.
/// </summary>
public sealed record BaletteKeyboardShortcut
{
    /// <summary>Creates a shortcut from its chords, pressed in order.</summary>
    public BaletteKeyboardShortcut(IReadOnlyList<BaletteKeyChord> chords)
    {
        if (chords.Count == 0)
        {
            throw new ArgumentException("A shortcut needs at least one chord.", nameof(chords));
        }

        Chords = chords;
    }

    /// <summary>The chords, pressed in order; one for a combination such as <c>Mod+K</c>.</summary>
    public IReadOnlyList<BaletteKeyChord> Chords { get; }

    /// <summary>True for multi-chord sequences such as <c>G I</c>.</summary>
    public bool IsSequence => Chords.Count > 1;

    /// <summary>Parse a shortcut string. Chords are separated by spaces, keys inside a chord by <c>+</c>.</summary>
    public static BaletteKeyboardShortcut Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var chords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(BaletteKeyChord.Parse)
            .ToArray();
        return new BaletteKeyboardShortcut(chords);
    }

    /// <summary>Parses a shortcut string, returning false instead of throwing for an invalid one.</summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out BaletteKeyboardShortcut? shortcut)
    {
        shortcut = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            shortcut = Parse(text);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Canonical lower-case form used for matching, e.g. <c>mod+k</c> or <c>g i</c>.</summary>
    public string Normalized => string.Join(' ', Chords.Select(c => c.Normalized));

    /// <summary>Platform-specific display tokens per chord, e.g. [["⌘","K"]] or [["Ctrl","K"]].</summary>
    public IReadOnlyList<IReadOnlyList<string>> DisplayTokens(bool isMac) => Chords.Select(c => c.DisplayTokens(isMac)).ToArray();

    /// <summary>Human readable text, e.g. <c>Ctrl+K</c> or <c>G then I</c>.</summary>
    public string ToDisplayString(bool isMac) => string.Join(isMac ? " " : " then ", DisplayTokens(isMac).Select(t => string.Join(isMac ? string.Empty : "+", t)));

    /// <summary>
    /// Human readable text with the chords of a sequence joined by <paramref name="then"/> on every platform, e.g.
    /// <c>⌘K</c>, <c>Ctrl+K</c> or <c>G dann I</c>: what screen readers are given for a shortcut hint.
    /// </summary>
    public string ToDisplayString(bool isMac, string then) => string.Join($" {then} ", DisplayTokens(isMac).Select(t => string.Join(isMac ? string.Empty : "+", t)));

    /// <summary>Shortcuts are equal when their normalized forms are: <c>Ctrl+K</c> equals <c>control+k</c>.</summary>
    public bool Equals(BaletteKeyboardShortcut? other) => other is not null && Normalized == other.Normalized;

    public override int GetHashCode() => Normalized.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Normalized;

    /// <summary>Parses a string, so a shortcut can be written as text wherever one is expected.</summary>
    public static implicit operator BaletteKeyboardShortcut(string text) => Parse(text);
}
