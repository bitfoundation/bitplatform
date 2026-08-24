namespace Boilerplate.Client.Core.Styles;

// [mirror] These must equal Bit.BlazorUI's --bit-clr-bg-pri for the active theme, because they paint the native
// chrome around the WebView (MAUI status bar and page background, Windows caption, the in-app browser toolbar) and
// the WebView's own background before any CSS has loaded. If they drift, the native surface and the page meet in a
// visible seam. Keep in sync with: Styles/app.scss.
public partial class ThemeColors
{
    public static readonly string PrimaryDarkBgColor = "#0F1318";
    public static readonly string PrimaryLightBgColor = "#FFFFFF";
}
