using AngleSharp.Dom;

namespace Bit.Bheadless.Testing;

/// <summary>One stop of the sequential (Tab) focus order.</summary>
/// <param name="Element">The focusable element.</param>
/// <param name="Role">Its role.</param>
/// <param name="Name">Its accessible name.</param>
public sealed record BitBhTabStop(IElement Element, string? Role, string Name)
{
    /// <inheritdoc />
    public override string ToString() => $"{Role ?? Element.LocalName} \"{Name}\"";
}

/// <summary>
/// Inspects the keyboard focus order of rendered markup: which elements Tab
/// reaches and in which order (positive <c>tabindex</c> first, then document order;
/// disabled, unrendered, inert and <c>tabindex="-1"</c> elements excluded). Content
/// that is only <c>aria-hidden</c> stays in the order, as it does in browsers.
/// </summary>
public static class BitBhFocusOrder
{
    /// <summary>The Tab sequence inside <paramref name="roots"/>.</summary>
    public static IReadOnlyList<BitBhTabStop> TabSequence(IEnumerable<INode> roots)
    {
        var elements = new List<IElement>();
        foreach (var root in roots)
        {
            if (root is IElement e) elements.Add(e);
            if (root is IParentNode parent) elements.AddRange(parent.QuerySelectorAll("*"));
        }
        var tabbable = RadioGroupStops(elements.Distinct().Where(BitBhAria.IsTabbable).ToList());
        int Index(IElement el) => BitBhAria.TryGetTabIndex(el, out var t) ? t : 0;
        return tabbable.Where(e => Index(e) > 0).OrderBy(Index)
            .Concat(tabbable.Where(e => Index(e) <= 0))
            .Select(e => new BitBhTabStop(e, BitBhAria.GetRole(e), BitBhAccessibleName.Compute(e)))
            .ToList();
    }

    /// <summary>
    /// Native radio buttons sharing a name form one tab stop: the checked one, or the
    /// first of the group when none is checked.
    /// </summary>
    private static List<IElement> RadioGroupStops(List<IElement> tabbable)
    {
        static bool IsNamedRadio(IElement e)
            => e.LocalName == "input" && string.Equals(e.GetAttribute("type"), "radio", StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrEmpty(e.GetAttribute("name"));
        static (IElement? Form, string Name) Group(IElement e) => (e.Closest("form"), e.GetAttribute("name")!);

        var keep = new HashSet<IElement>();
        foreach (var group in tabbable.Where(IsNamedRadio).GroupBy(Group))
        {
            var stop = group.FirstOrDefault(e => e.HasAttribute("checked")) ?? group.First();
            keep.Add(stop);
        }
        return tabbable.Where(e => !IsNamedRadio(e) || keep.Contains(e)).ToList();
    }

    /// <summary>The Tab sequence inside <paramref name="root"/>.</summary>
    public static IReadOnlyList<BitBhTabStop> TabSequence(INode root) => TabSequence(new[] { root });

    /// <summary>A readable listing of the sequence (one "role \"name\"" per line).</summary>
    public static string Describe(IEnumerable<BitBhTabStop> stops) => string.Join(Environment.NewLine, stops.Select((s, i) => $"{i + 1}. {s}"));
}
