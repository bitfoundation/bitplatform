namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// One entry of the home page's live showcase: the component to introduce, and the source that
/// produced the running copy shown beside it.
/// </summary>
public class PopularComponent
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Url { get; set; }

    /// <summary>
    /// The markup behind the live preview. It is written out here rather than generated, because the
    /// point of the section is that what the reader sees is what they would type.
    /// </summary>
    public string? Code { get; set; }
}
