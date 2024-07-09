namespace Bit.BlazorUI;

public class BitLinkTarget
{
    /// <summary>
    /// The current browsing context. (Default)
    /// </summary>
    public const string Self = "_self";

    /// <summary>
    /// Usually a new tab, but users can configure browsers to open a new window instead.
    /// </summary>
    public const string Blank = "_blank";

    /// <summary>
    /// The parent browsing context of the current one. If no parent, behaves as _self.
    /// </summary>
    public const string Parent = "_parent";

    /// <summary>
    /// The topmost browsing context. To be specific, this means the 'highest' context that's an ancestor of the current one. If no ancestors, behaves as _self.
    /// </summary>
    public const string Top = "_top";

    /// <summary>
    /// Allows embedded fenced frames to navigate the top-level frame.
    /// </summary>
    public const string UnfencedTop = "_unfencedTop";
}
