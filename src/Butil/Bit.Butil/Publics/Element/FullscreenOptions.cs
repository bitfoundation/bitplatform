namespace Bit.Butil;

public class FullScreenOptions
{
    public FullScreenNavigationUI? NavigationUI { get; set; }

    public object ToJsObject()
    {
        var navigationUI = NavigationUI switch
        {
            FullScreenNavigationUI.Hide => "hide",
            FullScreenNavigationUI.Show => "show",
            _ => "auto",
        };

        return new
        {
            NavigationUI = navigationUI,
        };
    }
}
