namespace Boilerplate.Workbench.Infrastructure.Services;

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
