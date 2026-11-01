namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>An arrow between two screens of a flow, starting at the element its tap names when that element was found.</summary>
public sealed record WorkbenchFlowLink(string Id, string From, string To, string? Label, WorkbenchFlowSpot? Spot);
