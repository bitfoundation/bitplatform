namespace Boilerplate.Workbench.Components.Layout;

public partial class WorkbenchLayout
{
    private readonly BitDir? currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
}
