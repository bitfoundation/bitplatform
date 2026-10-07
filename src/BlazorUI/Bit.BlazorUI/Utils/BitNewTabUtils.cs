using System;

namespace Bit.BlazorUI;

/// <summary>
/// What every component rendering an anchor shares about one that opens a new browsing context: recognizing
/// it, keeping the opened page from reaching back into this one, and telling a screen reader it happens.
/// </summary>
internal static class BitNewTabUtils
{
    /// <summary>
    /// The sentence a new-tab anchor is announced with when nothing else is said.
    /// </summary>
    internal const string DefaultHint = "(opens in a new tab)";

    /// <summary>
    /// Whether the target opens a new browsing context.
    /// </summary>
    /// <remarks>
    /// The browser matches the target keyword case-insensitively, so a <c>_BLANK</c> opens the same new tab a
    /// <c>_blank</c> does and has to be recognized as one - by the rel that hardens it and by the sentence that
    /// announces it alike.
    /// </remarks>
    internal static bool IsNewTab(string? target)
    {
        return string.Equals(target, BitLinkTarget.Blank, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Merges the rel values an anchor was given with the one a new-tab anchor is not safe without.
    /// </summary>
    /// <remarks>
    /// The page a <c>_blank</c> anchor opens is handed a reference back to the one that opened it, which it can
    /// navigate somewhere else; <c>noopener</c> is what severs that. It is added to whatever rel the anchor
    /// already has - a <c>nofollow</c> is about crawling and says nothing about the opener - unless that rel
    /// already says what the opener relationship should be: an author asking for <c>opener</c> back means it,
    /// and <c>noreferrer</c> already implies <c>noopener</c>. rel is a set of case-insensitive tokens, so each
    /// token is compared whole - a <c>noopener-policy</c> closes nothing.
    /// </remarks>
    internal static string? AddNoOpener(string? rel, string? target)
    {
        if (IsNewTab(target) is false) return rel;

        var tokens = rel?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? [];

        foreach (var token in tokens)
        {
            if (string.Equals(token, "noopener", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "noreferrer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "opener", StringComparison.OrdinalIgnoreCase))
            {
                return rel;
            }
        }

        return tokens.Length > 0 ? $"{rel} noopener" : "noopener";
    }

    /// <summary>
    /// The sentence a new-tab anchor is announced with, or null where there is nothing to announce.
    /// </summary>
    /// <param name="target">The target the anchor actually renders.</param>
    /// <param name="hint">The sentence the app gave in place of the default one; an empty one takes it off.</param>
    /// <param name="suppressed">Whether the app took the announcement off altogether.</param>
    internal static string? GetHint(string? target, string? hint, bool suppressed = false)
    {
        if (suppressed || IsNewTab(target) is false) return null;

        hint ??= DefaultHint;

        return hint.HasValue() ? hint : null;
    }

    /// <summary>
    /// Works out where the new-tab sentence goes, given how the anchor is named.
    /// </summary>
    /// <remarks>
    /// The sentence has to land wherever the name of the anchor comes from, since a name given from somewhere
    /// else replaces the content rather than adding to it. An aria-labelledby is the one that wins, and it
    /// points at elements rather than holding text, so the sentence is rendered as an element of its own and
    /// its id appended to the list; an aria-label holds the text itself, so the sentence is appended to it; an
    /// anchor named by nothing but its own content gets the sentence as visually hidden text inside it.
    /// </remarks>
    /// <param name="hint">The sentence, as <see cref="GetHint"/> resolved it.</param>
    /// <param name="ariaLabel">The aria-label the anchor would render without the sentence.</param>
    /// <param name="ariaLabelledBy">The aria-labelledby the anchor would render without the sentence.</param>
    /// <param name="hintId">The id the element carrying the sentence gets where an aria-labelledby points at it.</param>
    internal static BitNewTabHintPlacement PlaceHint(string? hint, string? ariaLabel, string? ariaLabelledBy, string hintId)
    {
        if (hint.HasNoValue()) return new(ariaLabel, ariaLabelledBy, null, null);

        if (ariaLabelledBy.HasValue()) return new(ariaLabel, $"{ariaLabelledBy} {hintId}", hint, hintId);

        if (ariaLabel.HasValue()) return new($"{ariaLabel} {hint}", ariaLabelledBy, null, null);

        return new(ariaLabel, ariaLabelledBy, hint, null);
    }
}

/// <summary>
/// What a new-tab anchor renders to carry its sentence: the aria-label and aria-labelledby to put on the
/// anchor, and the visually hidden text to put inside it, if any, with the id that text is rendered with.
/// </summary>
internal readonly record struct BitNewTabHintPlacement(string? AriaLabel, string? AriaLabelledBy, string? HintText, string? HintId);
