namespace Bit.Blow;

/// <summary>
/// A measured handle anchor relative to its node's top-left, in flow units.
/// </summary>
/// <param name="HandleId">The handle's own id, or an empty string when it has none.</param>
/// <param name="Type">Whether the handle is a source or a target.</param>
/// <param name="Position">The side of the node the handle sits on.</param>
/// <param name="OffsetX">Horizontal offset of the handle's centre from the node's left edge.</param>
/// <param name="OffsetY">Vertical offset of the handle's centre from the node's top edge.</param>
/// <param name="ConnectableEnd">
/// False when the handle refuses to have connections dropped on it (its <c>IsConnectable</c> or
/// <c>IsConnectableEnd</c> is off), so snapping to the nearest handle must pass it over.
/// </param>
public readonly record struct BlowHandleBounds(
    string HandleId,
    BlowHandleType Type,
    BlowPosition Position,
    double OffsetX,
    double OffsetY,
    bool ConnectableEnd = true)
{
    /// <summary>Builds the dictionary key used to store/lookup a handle.</summary>
    /// <remarks>
    /// Asked for both ends of every edge on every render, so the keys of the unnamed handles - the common case - are
    /// constants rather than a fresh string each time.
    /// </remarks>
    public static string Key(BlowHandleType type, string? handleId) => string.IsNullOrEmpty(handleId)
        ? (type == BlowHandleType.Source ? UnnamedSourceKey : UnnamedTargetKey)
        : $"{type}:{handleId}";

    private const string UnnamedSourceKey = "Source:";
    private const string UnnamedTargetKey = "Target:";
}
