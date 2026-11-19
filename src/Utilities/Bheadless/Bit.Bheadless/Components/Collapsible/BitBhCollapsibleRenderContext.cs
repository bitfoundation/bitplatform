namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by collapsible parts.</summary>
public sealed record BitBhCollapsibleRenderContext
{
    /// <summary>Whether the content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the content.</summary>
    public required Action Close { get; init; }
}
