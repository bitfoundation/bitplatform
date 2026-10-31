namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchCheckResult(WorkbenchAccessibilityFinding[] Accessibility, WorkbenchThemeFinding[] Theme);

public sealed record WorkbenchAccessibilityFinding(string Rule, string Impact, string Help, string HelpUrl, int Count, string[] Targets);

public sealed record WorkbenchThemeFinding(string Property, string Color, int Count, string Example);
