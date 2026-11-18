namespace Bit.Bheadless;

/// <summary>One entry of a pagination's page range: a page number, or a gap (an ellipsis).</summary>
/// <param name="Page">The page number (1-based), or 0 for a gap.</param>
/// <param name="Key">A key that stays stable for the entry while the page changes (<c>@key</c> it).</param>
public sealed record BitBhPaginationEntry(int Page, string Key)
{
    /// <summary>Whether the entry is a gap (an ellipsis) rather than a page.</summary>
    public bool IsEllipsis => Page == 0;
}
