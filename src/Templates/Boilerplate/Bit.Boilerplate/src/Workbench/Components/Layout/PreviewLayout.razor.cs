namespace Boilerplate.Workbench.Components.Layout;

/// <summary>
/// The layout of the /preview pages, which every view's frame and screenshot shows: the page with the app's own
/// bit BlazorUI defaults, in the direction of the view's culture.
/// </summary>
public partial class PreviewLayout
{
    // Right-to-left for a right-to-left culture; null leaves the bit components' default.
    private readonly BitDir? currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;

    /// <summary>The app's own (MainLayout.ModalParameters): its modals carry the class the app's styles give them.</summary>
    private static readonly BitModalParameters modalParameters = new() { Classes = new() { Root = "modal" } };
}
