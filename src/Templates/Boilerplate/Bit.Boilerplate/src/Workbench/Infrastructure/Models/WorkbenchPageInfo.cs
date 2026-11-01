using Microsoft.AspNetCore.Authorization;

namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// A page of the catalog: its key, the resource keys of its texts, its component, the components it shows off,
/// and the design options and switches its views can pick.
/// </summary>
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

    /// <summary>True when the page has an [Authorize] rule, so a signed-out visitor gets the not-authorized page instead.</summary>
    public bool NeedsSignIn => ComponentType.IsDefined(typeof(AuthorizeAttribute), inherit: true);

    /// <summary>The design option the scenario names, or the first one.</summary>
    public WorkbenchPageVariant? ResolveVariant(WorkbenchScenario scenario) => Variants.FirstOrDefault(v => v.Key == scenario.Variant) ?? Variants.FirstOrDefault();
}
