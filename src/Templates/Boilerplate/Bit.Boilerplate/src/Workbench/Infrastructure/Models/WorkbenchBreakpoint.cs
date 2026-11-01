namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>One of the breakpoints the app's styles and components switch at, with its minimum width in CSS pixels.</summary>
public sealed record WorkbenchBreakpoint(string Name, int Min);
