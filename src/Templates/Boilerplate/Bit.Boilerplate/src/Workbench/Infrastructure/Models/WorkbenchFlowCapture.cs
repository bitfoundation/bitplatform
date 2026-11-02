namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// What the headless browser found on a flow screen: the page's state after the taps, where each arrow starts,
/// and the first tap it could not find.
/// </summary>
public sealed record WorkbenchFlowCapture(string? State, WorkbenchFlowSpot?[] Spots, string? MissingTap);
