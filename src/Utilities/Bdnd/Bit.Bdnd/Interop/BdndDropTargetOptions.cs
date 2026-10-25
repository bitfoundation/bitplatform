namespace Bit.Bdnd;

/// <summary>Options of a drop target registration in <c>bit-bdnd.js</c>.</summary>
internal sealed record BdndDropTargetOptions
{
    /// <summary>The target's handler id, by which a keyboard drag names the targets it may move to.</summary>
    public string Id { get; init; } = "";

    /// <summary>Whether the target can accept the in-app item currently being dragged.</summary>
    public bool CanDrop { get; init; }

    /// <summary>Whether the target has an <c>OnHover</c>, which a pointer drag then reports its moves to.</summary>
    public bool Hover { get; init; }

    /// <summary>The edges (<see cref="BdndEdges"/> flags) the module reports the closest of while a drag is over the target.</summary>
    public int Edges { get; init; }

    /// <summary>The file types (MIME types, <c>image/*</c>, <c>.png</c>) a native file drop is limited to; empty for any.</summary>
    public string[] FileTypes { get; init; } = [];

    /// <summary>Whether a dropped folder is expanded into the files inside it.</summary>
    public bool ExpandDirectories { get; init; }

    /// <summary>The most files a native drop hands over; 0 for no limit.</summary>
    public int MaxFiles { get; init; }

    /// <summary>
    /// The <c>DataTransfer</c> types of the native drags the target accepts. The same array instance
    /// is reused while the accepted types are unchanged, so unchanged options compare equal.
    /// </summary>
    public string[] NativeTypes { get; init; } = [];
}
