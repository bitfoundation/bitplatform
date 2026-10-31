namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchPageInfo(string Key,
                                       string TitleKey,
                                       string DescriptionKey,
                                       string IconName,
                                       Type ComponentType,
                                       string CategoryKey,
                                       string[] Showcases,
                                       string? CaptionKey = null)
{
    public WorkbenchPageVariant[] Variants { get; init; } = [];

    public WorkbenchPageSwitch[] Switches { get; init; } = [];

    public WorkbenchPageVariant? ResolveVariant(WorkbenchScenario scenario) => Variants.FirstOrDefault(v => v.Key == scenario.Variant) ?? Variants.FirstOrDefault();
}
