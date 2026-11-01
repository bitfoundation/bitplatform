using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Layout;

/// <summary>
/// The layout of the /preview pages, which every view's frame and screenshot shows: the page with the app's own
/// bit BlazorUI defaults, in the direction of the view's culture.
/// </summary>
public partial class PreviewLayout
{
    // Right-to-left for a right-to-left culture; null leaves the bit components' default.
    private readonly BitDir? currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet()
    {
        // An error replaces the page only until another page opens.
        errorBoundary?.Recover();
    }
}
