using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// Copying and pasting elements as text: the format <see cref="BinkCanvas"/> puts on the system clipboard, usable
/// on its own for an application's own copy and paste, drag between documents or templates.
/// </summary>
/// <remarks>
/// <para>
/// A payload is an ordinary Bink document holding the copied elements on one layer, in z-order, so anything that
/// reads Bink documents reads it, and pasting a whole saved drawing works the same way. It is read with the same
/// validation as a file, because clipboard content can come from anywhere.
/// </para>
/// <para>
/// Pasted elements get new ids, so pasting twice produces two independent copies. Metadata that refers to other
/// elements by id (a diagram connector naming the nodes it joins, say) is copied as it is and has to be re-pointed
/// by the application.
/// </para>
/// </remarks>
public static class BinkClipboard
{
    /// <summary>The document metadata key that marks a payload as copied elements rather than a saved drawing.</summary>
    public const string MetadataKey = "bink:clipboard";

    /// <summary>
    /// Serializes elements for the clipboard. Eraser strokes are left out: they are not selectable, and a mask
    /// pasted without the ink it erased would erase whatever it landed on.
    /// </summary>
    /// <param name="elements">The elements, bottom-most first.</param>
    /// <param name="options">How to write them; the defaults keep the payload compact.</param>
    public static string Serialize(IEnumerable<BinkElement> elements, BinkSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var document = new BinkDocument(new BinkDocumentSettings { Width = 0, Height = 0, Background = BinkColor.Transparent });
        Guid layer = document.Layers[0].Id;
        using (document.BeginBatch("Copy", BinkChangeOrigin.Load))
        {
            foreach (BinkElement element in elements)
            {
                if (element is BinkStroke { IsEraser: true } or BinkUnknownElement) continue;
                if (!document.ContainsElement(element.Id)) document.AddElement(element, layer);
            }
        }
        document.SetMetadata(MetadataKey, "1");
        return BinkSerializer.Serialize(document, options);
    }

    /// <summary>
    /// Reads a payload into new elements ready to add to a document: each has a new id and starts over at version
    /// zero. Accepts a clipboard payload or a whole saved document, whose visible layers are flattened in order.
    /// </summary>
    /// <param name="payload">The text from the clipboard.</param>
    /// <param name="elements">The elements, bottom-most first. Empty when the payload is not Bink content.</param>
    /// <param name="error">Why the payload could not be read, or null.</param>
    /// <param name="options">Validation applied to the payload. Pass the target document's limits.</param>
    public static bool TryDeserialize(string? payload, out IReadOnlyList<BinkElement> elements, out string? error, BinkDeserializerOptions? options = null)
    {
        elements = [];
        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "The clipboard is empty.";
            return false;
        }
        if (!BinkSerializer.TryDeserialize(payload, out BinkDocument? document, out error, options) || document is null) return false;

        var result = new List<BinkElement>(document.ElementCount);
        foreach (BinkLayer layer in document.Layers)
        {
            if (!layer.IsVisible) continue;
            foreach (BinkElement element in layer.Elements)
            {
                if (element is BinkStroke { IsEraser: true } or BinkUnknownElement) continue;
                result.Add(element with { Id = Guid.NewGuid(), Version = 0 });
            }
        }
        RenameGroups(result);
        elements = result;
        return true;
    }

    /// <summary>
    /// Gives the pasted groups new ids, so pasting twice makes two independent groups rather than one group spread
    /// over both copies, and drops the groups that mean nothing among the pasted elements: one with a single member,
    /// and one with exactly the members of the group nested inside it, which is what copying part of a group leaves.
    /// </summary>
    private static void RenameGroups(List<BinkElement> elements)
    {
        var members = new Dictionary<Guid, int>();
        foreach (BinkElement element in elements)
        {
            foreach (Guid group in element.GroupIds) members[group] = members.GetValueOrDefault(group) + 1;
        }
        if (members.Count == 0) return;

        var renamed = new Dictionary<Guid, Guid>();
        var chain = new List<Guid>();
        for (int i = 0; i < elements.Count; i++)
        {
            IReadOnlyList<Guid> groups = elements[i].GroupIds;
            if (groups.Count == 0) continue;
            chain.Clear();
            for (int g = 0; g < groups.Count; g++)
            {
                int count = members[groups[g]];
                // Nesting means an outer group holds at least its inner group's members, so the same count is the
                // same set.
                if (count < 2 || (g > 0 && count == members[groups[g - 1]])) continue;
                if (!renamed.TryGetValue(groups[g], out Guid name)) renamed[groups[g]] = name = Guid.NewGuid();
                chain.Add(name);
            }
            elements[i] = elements[i] with { GroupIds = chain };
        }
    }

    /// <summary>True when the text looks like Bink content, without parsing it.</summary>
    public static bool IsPayload(string? text)
        => text is not null && text.AsSpan().TrimStart().StartsWith("{\"format\":\"bink\"", StringComparison.Ordinal);

    /// <summary>The union of the elements' bounds.</summary>
    public static BinkRect Bounds(IEnumerable<BinkElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        BinkRect bounds = BinkRect.Empty;
        foreach (BinkElement element in elements) bounds = bounds.Union(element.Bounds);
        return bounds;
    }

    /// <summary>Returns the elements moved by a document-space offset.</summary>
    public static IReadOnlyList<BinkElement> Offset(IReadOnlyList<BinkElement> elements, Vector2 offset)
    {
        ArgumentNullException.ThrowIfNull(elements);
        if (offset == Vector2.Zero) return elements;
        var moved = new BinkElement[elements.Count];
        for (int i = 0; i < moved.Length; i++) moved[i] = elements[i].Translate(offset);
        return moved;
    }
}
