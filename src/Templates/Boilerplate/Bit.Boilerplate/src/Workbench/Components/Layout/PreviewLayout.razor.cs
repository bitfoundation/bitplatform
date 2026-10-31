namespace Boilerplate.Workbench.Components.Layout;

public partial class PreviewLayout
{
    private readonly BitDir? currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
}
