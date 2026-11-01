using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Layout;

/// <summary>
/// The layout of the Workbench's own pages, every page but the previews. It also hosts the reply notifier, so the
/// agent's replies are announced on any of them.
/// </summary>
public partial class WorkbenchLayout
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
