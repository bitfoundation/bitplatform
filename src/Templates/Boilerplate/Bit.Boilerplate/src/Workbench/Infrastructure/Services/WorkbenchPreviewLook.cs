namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>The inline style a preview's root element gets for the view's look, such as a larger text size.</summary>
public static class WorkbenchPreviewLook
{
    public static string GetRootStyle(WorkbenchScenario scenario)
    {
        var style = string.Empty;

        if (scenario.TextScale != 100)
        {
            style += FormattableString.Invariant($"font-size:{scenario.TextScale}%;");
        }

        return style;
    }
}
