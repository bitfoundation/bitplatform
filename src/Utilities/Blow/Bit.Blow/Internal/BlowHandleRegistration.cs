using Microsoft.AspNetCore.Components;

namespace Bit.Blow;

/// <summary>
/// The per-handle hooks a <c>BlowHandle</c> hands to the canvas: an optional validation predicate
/// consulted before the handle is offered as a drop target, and a callback raised once a
/// connection touching the handle has been made.
/// </summary>
public sealed class BlowHandleRegistration
{
    /// <summary>Extra validation for connections landing on (or starting from) this handle.</summary>
    public Func<BlowConnection, bool>? IsValidConnection { get; init; }

    /// <summary>Raised when a connection involving this handle is completed.</summary>
    public EventCallback<BlowConnection> OnConnect { get; init; }

    /// <summary>The handle's data type, overriding its declared port's; see <see cref="BlowPort.DataType"/>.</summary>
    public string? DataType { get; init; }

    /// <summary>The handle's connection limit, overriding its declared port's; see <see cref="BlowPort.MaxConnections"/>.</summary>
    public int? MaxConnections { get; init; }

    /// <summary>Builds the registry key identifying one handle of one node.</summary>
    public static string Key(string nodeId, BlowHandleType type, string? handleId) =>
        $"{nodeId}\u001f{type}\u001f{handleId ?? string.Empty}";
}
