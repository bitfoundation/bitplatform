using System;

namespace Bit.BlazorUI;

/// <summary>
/// What the components rendering an anchor from a target and a rel the app gives them - their own, or their
/// items' - share about one that opens a new browsing context: recognizing it, keeping the opened page from
/// reaching back into this one, and telling a screen reader it happens.
/// </summary>
/// <remarks>
/// BitLink, BitTag, BitButton, BitActionButton, BitButtonGroup, BitBadge, BitPersona, BitCard, BitNav, BitNavBar,
/// BitBreadcrumb and BitMenuButton resolve their rel here, and BitLink, BitTag, BitNav, BitNavBar, BitBreadcrumb
/// and BitMenuButton their announcement. Two kinds of anchor keep a rule of their own, since the app hands them
/// no target: the markdown renderer opens an external link in a new tab with <c>noopener noreferrer</c> (or the
/// rel its link options give) and announces it with <see cref="DefaultHint"/>, worded through its texts; and the
/// rich text editor's link popup and the PDF viewer's links always open a new tab, with a fixed
/// <c>noopener noreferrer</c>.
/// </remarks>
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

        // the parsed tokens rather than the string itself, so a rel with stray whitespace around it does not
        // carry that whitespace into the attribute.
        return tokens.Length > 0 ? $"{string.Join(' ', tokens)} noopener" : "noopener";
    }

    /// <summary>
    /// <see cref="AddNoOpener(string?, string?)"/> for the rel an anchor was given as <see cref="BitLinkRels"/> flags.
    /// </summary>
    /// <returns>The rel to render, or null where there is none.</returns>
    internal static string? AddNoOpener(BitLinkRels? rel, string? target)
    {
        var resolved = AddNoOpener(rel.HasValue ? BitLinkRelUtils.GetRels(rel.Value) : null, target);

        return resolved.HasValue() ? resolved : null;
    }

    /// <summary>
    /// The rel the anchor of a component with Href, Rel and Target parameters renders.
    /// </summary>
    /// <remarks>
    /// An empty href renders no anchor and a hash-only one stays on the page, so neither carries a rel; any
    /// other one gets its <see cref="BitLinkRels"/> with <c>noopener</c> added where
    /// <see cref="AddNoOpener(string?, string?)"/> says it belongs.
    /// </remarks>
    internal static string? ResolveRel(string? href, BitLinkRels? rel, string? target)
    {
        if (IsInPageHref(href)) return null;

        return AddNoOpener(rel, target);
    }

    /// <summary>
    /// Whether the href is empty or hash-only - one that renders no anchor or does not leave the page.
    /// </summary>
    internal static bool IsInPageHref(string? href)
    {
        return href.HasNoValue() || href!.StartsWith('#');
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
