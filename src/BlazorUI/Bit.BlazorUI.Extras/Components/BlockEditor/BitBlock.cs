using System.Collections.ObjectModel;

namespace Bit.BlazorUI;

/// <summary>
/// One block of a <see cref="BitBlockDocument"/>: a paragraph, a heading, a list item, an image, or a block of an app's
/// own type. Immutable, so a changed block is a new one made with <c>with</c>, and the blocks a change did not touch
/// stay the same instances.
/// </summary>
/// <remarks>
/// Two blocks are equal when their id, type, text, props and children are; the props compare as a set of pairs, in
/// whatever order they were written.
/// </remarks>
public sealed record BitBlock
{
    private static readonly IReadOnlyDictionary<string, string> NoProps = ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// The block's id, unique in its document and kept through every edit of the block. A new block is given a short
    /// random one when none is set.
    /// </summary>
    public string Id { get; init; } = NewId();

    /// <summary>
    /// The block's type: the <see cref="BitBlockType.Name"/> of a built-in type (<see cref="BitBlockTypes"/>) or of an
    /// app's own. A document keeps a block whose type is not registered, so saving it never drops it.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The block's content. For a <see cref="BitBlockKind.Text"/> type it is inline HTML in a fixed vocabulary - <c>b</c>,
    /// <c>i</c>, <c>s</c>, <c>code</c>, <c>a href</c>, <c>sub</c>, <c>sup</c>, <c>mark</c>, <c>ins</c> and <c>br</c> - which
    /// is what Markdown can say, so a Markdown export loses nothing; anything else is dropped (its text kept) wherever the
    /// document is read or written. For a <see cref="BitBlockKind.Code"/> type it is plain text, and an
    /// <see cref="BitBlockKind.Atom"/> type has none.
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// The block's settings, as strings, so the JSON form needs no type information: a heading's <c>level</c>, a list
    /// item's <c>style</c> and <c>checked</c>, an image's <c>src</c>. Each type documents its own and reads them leniently,
    /// so a value it cannot read falls back to its default.
    /// </summary>
    public IReadOnlyDictionary<string, string> Props { get; init; } = NoProps;

    /// <summary>The blocks nested under this one, for a type that nests (<see cref="BitBlockType.Nests"/>).</summary>
    public IReadOnlyList<BitBlock> Children { get; init; } = [];

    /// <summary>Returns the prop of this name, or null when the block has none.</summary>
    public string? GetProp(string name) => Props.TryGetValue(name, out var value) ? value : null;

    /// <summary>
    /// Returns a copy of this block with the prop set to <paramref name="value"/>, or removed when the value is null.
    /// </summary>
    public BitBlock WithProp(string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(name);

        var props = new Dictionary<string, string>(Props, StringComparer.Ordinal);
        if (value is null)
        {
            if (props.Remove(name) is false) return this;
        }
        else
        {
            props[name] = value;
        }

        return this with { Props = props };
    }

    /// <summary>A new block id: eight characters of lowercase letters and digits.</summary>
    public static string NewId()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

        return string.Create(8, 0, static (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = alphabet[Random.Shared.Next(alphabet.Length)];
            }
        });
    }

    /// <inheritdoc />
    public bool Equals(BitBlock? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        return Id == other.Id
            && Type == other.Type
            && Text == other.Text
            && PropsEqual(Props, other.Props)
            && Children.SequenceEqual(other.Children);
    }

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Id, Type, Text, Props.Count, Children.Count);

    private static bool PropsEqual(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a.Count != b.Count) return false;

        foreach (var (key, value) in a)
        {
            if (b.TryGetValue(key, out var other) is false || other != value) return false;
        }

        return true;
    }
}
