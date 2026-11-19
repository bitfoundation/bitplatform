namespace Bit.Blow;

/// <summary>The handle found under a screen point, used to resolve where a connection was dropped.</summary>
/// <param name="NodeId">The id of the node the handle belongs to, or null when nothing was hit.</param>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="HandleType">Either <c>"source"</c> or <c>"target"</c>.</param>
/// <param name="Position">The side the handle is anchored to.</param>
/// <param name="ConnectableEnd">False when the handle refuses dropped connections; null (not reported) means it accepts them.</param>
public sealed record BlowHandleHit(
    string? NodeId, string? HandleId, string? HandleType, string? Position, bool? ConnectableEnd = null);
