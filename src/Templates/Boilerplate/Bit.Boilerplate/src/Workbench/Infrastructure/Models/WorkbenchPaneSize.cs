namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>The size a view was dragged or typed to, in CSS pixels.</summary>
public sealed record WorkbenchPaneSize(WorkbenchPane Pane, int Width, int Height);
