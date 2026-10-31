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

        if (scenario.Density is { } density)
        {
            style += FormattableString.Invariant($"--bit-layout-density-scale:{(density is WorkbenchDensity.Compact ? 0.85 : 1.15)};");
        }

        return style;
    }
}
