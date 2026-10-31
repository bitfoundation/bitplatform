namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchFlowLink(string Id, string From, string To, string? Label, WorkbenchFlowSpot? Spot);
