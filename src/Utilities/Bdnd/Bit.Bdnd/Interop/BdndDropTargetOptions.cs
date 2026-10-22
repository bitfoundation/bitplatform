namespace Bit.Bdnd;

/// <summary>Options of a drop target registration in <c>bit-bdnd.js</c>.</summary>
internal sealed record BdndDropTargetOptions
{
    /// <summary>Whether the target can accept the in-app item currently being dragged.</summary>
    public bool CanDrop { get; init; }

    /// <summary>
    /// The <c>DataTransfer</c> types of the native drags the target accepts. The same array instance
    /// is reused while the accepted types are unchanged, so unchanged options compare equal.
    /// </summary>
    public string[] NativeTypes { get; init; } = [];
}
