namespace Bit.Bink;

/// <summary>What area of the document an export covers.</summary>
public enum BinkExportRegion : byte
{
    /// <summary>The page rectangle from the document settings, or the content bounds when the page is unbounded.</summary>
    Page = 0,
    /// <summary>The bounding box of the visible content, with padding.</summary>
    Content = 1,
    /// <summary>An explicit rectangle supplied by the caller.</summary>
    Custom = 2,
    /// <summary>The bounding box of the current selection.</summary>
    Selection = 3,
}
