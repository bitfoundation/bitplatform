namespace Bit.Butil;

/// <summary>
/// The options bag for a fullscreen request.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/requestFullscreen">Element.requestFullscreen()</see>
/// </summary>
public class FullScreenOptions
{
    /// <summary>
    /// Whether browser navigation controls stay visible. <c>null</c> is the same as
    /// <see cref="FullScreenNavigationUI.Auto"/>.
    /// </summary>
    public FullScreenNavigationUI? NavigationUI { get; set; }

    internal FullScreenJsOptions ToJsObject()
    {
        var navigationUI = NavigationUI switch
        {
            FullScreenNavigationUI.Hide => "hide",
            FullScreenNavigationUI.Show => "show",
            _ => "auto",
        };

        return new()
        {
            NavigationUI = navigationUI,
        };
    }
}
