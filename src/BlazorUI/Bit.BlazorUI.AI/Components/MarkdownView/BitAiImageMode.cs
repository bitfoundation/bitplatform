namespace Bit.BlazorUI;

/// <summary>
/// How images in Markdown are treated (see <see cref="BitAiImagePolicy"/>).
/// </summary>
public enum BitAiImageMode
{
    /// <summary>Show every image whose URL scheme is safe.</summary>
    Allow = 0,

    /// <summary>Show images only from the listed hosts; others get a placeholder.</summary>
    AllowListedHosts = 1,

    /// <summary>Show no remote images; each is replaced by its alternative text.</summary>
    Block = 2,

    /// <summary>Show a placeholder that loads the image when the user asks for it.</summary>
    ClickToLoad = 3,
}
