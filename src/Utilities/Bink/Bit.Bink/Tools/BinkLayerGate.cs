namespace Bit.Bink;

/// <summary>
/// Whether new elements may go on a layer: the one rule the drawing tools and the canvas's own commands (paste,
/// drop, import, adding an image) share, so they refuse the same layers and say the same thing when they do.
/// </summary>
internal static class BinkLayerGate
{
    /// <summary>
    /// Why new elements cannot go on a layer, or null when they can. A locked layer refuses them, and so does a hidden
    /// one: anything added there would be invisible. A layer the document does not have is left to the caller, which
    /// fails on it with its own message.
    /// </summary>
    public static string? Refusal(BinkDocument document, Guid layerId)
    {
        if (!document.TryGetLayer(layerId, out BinkLayer? layer) || (!layer.IsLocked && layer.IsVisible)) return null;
        string name = string.IsNullOrWhiteSpace(layer.Name) ? "The layer" : $"The layer \"{layer.Name}\"";
        return $"{name} is {(layer.IsLocked ? "locked" : "hidden")}, so nothing can be added to it.";
    }

    /// <summary>Whether a tool may add to the active layer, announcing why not when it may not.</summary>
    public static bool Admits(IBinkToolContext context) => Admits(context, context.ActiveLayerId);

    /// <summary>
    /// Whether a tool may add to a layer, announcing why not when it may not. A gesture asks again when it ends, about
    /// the layer it started on: that layer can be locked or hidden while the pointer is down.
    /// </summary>
    public static bool Admits(IBinkToolContext context, Guid layerId)
    {
        if (Refusal(context.Document, layerId) is not { } reason) return true;
        context.Announce(reason);
        return false;
    }
}
