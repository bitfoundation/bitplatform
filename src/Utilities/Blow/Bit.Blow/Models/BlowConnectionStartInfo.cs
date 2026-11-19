namespace Bit.Blow;

/// <summary>
/// Identifies the handle a connection drag started from. Raised by
/// <c>BlowCanvas.OnConnectStart</c>.
/// </summary>
public readonly record struct BlowConnectionStartInfo(
    string NodeId,
    string? HandleId,
    BlowHandleType HandleType);
