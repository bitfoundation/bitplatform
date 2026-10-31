namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlowCapture(string? State, WorkbenchFlowSpot?[] Spots, string? MissingTap);
