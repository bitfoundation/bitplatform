namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchPageInfo(string Key,
                                       string TitleKey,
                                       string DescriptionKey,
                                       string IconName,
                                       Type ComponentType,
                                       string CategoryKey,
                                       string[] Showcases);
