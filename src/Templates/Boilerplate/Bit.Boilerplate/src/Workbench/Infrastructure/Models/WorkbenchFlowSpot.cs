namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>An element's box on a screen, as fractions of the screen's width and height.</summary>
public sealed record WorkbenchFlowSpot(double X, double Y, double Width, double Height);
