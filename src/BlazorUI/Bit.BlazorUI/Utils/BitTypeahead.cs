using System;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The typeahead of a list, a menu or a tree: the keys typed in quick succession make one search term, and the keyboard
/// moves to the next item whose text starts with it. The components that move between their items by the start of
/// their text go through it, so what makes a term and what a term matches live in one place.
/// </summary>
/// <remarks>
/// <para>
/// A pause longer than <see cref="Timeout"/> starts a new term. One character typed again and again is not a term of
/// doubled letters but the cycle of the WAI-ARIA APG: each press moves on to the next item that starts with it. So a
/// term of one character, repeated or not, is looked for from the item after the one the keyboard is on, and a real
/// term from that item itself, which keeps the keyboard where it is while the term typed further still matches it.
/// </para>
/// <para>
/// The match follows the rules of the current culture. Case is ignored the way that culture ignores it (the dotted and
/// dotless i of Turkish), and so are the accents the culture treats as marks on a letter rather than letters of their
/// own: "e" finds "Église" in English, while in Swedish "a" does not find "Åland", since å is a letter of its own
/// there. An app that runs with invariant globalization gets the case ignored and the accents kept.
/// </para>
/// </remarks>
internal sealed class BitTypeahead
{
    /// <summary>
    /// How long a pause in typing starts a new term.
    /// </summary>
    internal static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(1000);

    private const CompareOptions Comparison = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    private readonly Func<long> _clock;

    private string _term = string.Empty;
    private string _first = string.Empty;
    private bool _repeated;
    private long _typedAt;



    internal BitTypeahead() : this(static () => Environment.TickCount64) { }

    /// <summary>
    /// A typeahead that reads the time, in milliseconds, from the given clock instead of the system's.
    /// </summary>
    internal BitTypeahead(Func<long> clock)
    {
        _clock = clock;
    }



    /// <summary>
    /// Adds a typed key to the term, or starts a new term with it after a pause, and returns what to look for: the
    /// text and whether the search starts from the item after the one the keyboard is on.
    /// </summary>
    internal (string Text, bool FromNext) Type(string key)
    {
        var now = _clock();

        if (_term.Length == 0 || now - _typedAt > Timeout.TotalMilliseconds)
        {
            _term = key;
            _first = key;
            _repeated = true;
        }
        else
        {
            _term += key;
            _repeated = _repeated && Same(key, _first);
        }

        _typedAt = now;

        return _repeated ? (_first, true) : (_term, false);
    }

    /// <summary>
    /// Adds a typed key to the term and returns the index of the item it takes the keyboard to, or -1 when no item
    /// matches. The items are walked from the one the keyboard is on (<paramref name="current"/>, -1 for none) and
    /// around the end of the list, leaving out the ones <paramref name="skip"/> names.
    /// </summary>
    internal int Find(string key, int count, int current, Func<int, string?> textAt, Func<int, bool>? skip = null)
    {
        return Find(Type(key), count, current, textAt, skip);
    }

    /// <summary>
    /// The index of the item a search returned by <see cref="Type"/> takes the keyboard to, or -1 when no item matches.
    /// </summary>
    internal static int Find((string Text, bool FromNext) search, int count, int current, Func<int, string?> textAt, Func<int, bool>? skip = null)
    {
        if (count <= 0) return -1;

        var start = current < 0 ? 0 : (search.FromNext ? current + 1 : current);

        for (var k = 0; k < count; k++)
        {
            var i = (start + k) % count;

            if (skip?.Invoke(i) is true) continue;

            if (Matches(textAt(i), search.Text)) return i;
        }

        return -1;
    }

    /// <summary>
    /// Whether the text starts with the term, by the rules of the current culture. The white space a text starts with
    /// is not part of what anyone types.
    /// </summary>
    internal static bool Matches(string? text, string term)
    {
        if (string.IsNullOrEmpty(text) || term.Length == 0) return false;

        return CultureInfo.CurrentCulture.CompareInfo.IsPrefix(text.AsSpan().TrimStart(), term, Comparison);
    }

    private static bool Same(string key, string first)
    {
        return CultureInfo.CurrentCulture.CompareInfo.Compare(key, first, Comparison) == 0;
    }
}
