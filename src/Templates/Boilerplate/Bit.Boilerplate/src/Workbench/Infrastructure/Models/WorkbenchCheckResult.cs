namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// The checks of a view and of each part of it they could open, like its tabs, panels and menus: axe-core's
/// accessibility findings, the colors that match none of the theme's, how many states of the page were checked, and
/// whether that was all of them or the run stopped at its limits.
/// </summary>
public sealed record WorkbenchCheckResult(WorkbenchAccessibilityFinding[] Accessibility, WorkbenchThemeFinding[] Theme, int Checked, bool IsComplete);

/// <summary>
/// An axe-core rule a view breaks in one of its states, with how many elements break it and some of them; where names the
/// part of the page it was found in, and steps are the presses that open it, empty for the page as it opens.
/// </summary>
public sealed record WorkbenchAccessibilityFinding(string Rule, string Impact, string Help, string HelpUrl, int Count, WorkbenchCheckNode[] Nodes, string? Where, WorkbenchCheckStep[] Steps);

/// <summary>An element an accessibility rule fails on: its place in the page, its markup and what axe-core says is wrong with it.</summary>
public sealed record WorkbenchCheckNode(string Target, string Html, string Summary);

/// <summary>A press that opens part of a page: picking a tab ("tab"), or opening what a control expands ("expand"), with the control's place and name.</summary>
public sealed record WorkbenchCheckStep(string Selector, string Kind, string Label);

/// <summary>
/// A color a page sets that matches none of the theme's, with the CSS property, how often, and an element that uses it,
/// by its description and place, with the part of the page it is in.
/// </summary>
public sealed record WorkbenchThemeFinding(string Property, string Color, int Count, string Example, string Target, string? Where, WorkbenchCheckStep[] Steps);
