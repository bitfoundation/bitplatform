namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The checks of a view: axe-core's accessibility findings and the colors that match none of the theme's.</summary>
public sealed record WorkbenchCheckResult(WorkbenchAccessibilityFinding[] Accessibility, WorkbenchThemeFinding[] Theme);

/// <summary>An axe-core rule a view breaks, with how many elements break it and selectors of some of them.</summary>
public sealed record WorkbenchAccessibilityFinding(string Rule, string Impact, string Help, string HelpUrl, int Count, string[] Targets);

/// <summary>A color a page sets that matches none of the theme's, with the CSS property, how often, and an element that uses it.</summary>
public sealed record WorkbenchThemeFinding(string Property, string Color, int Count, string Example);
