namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>A step of the agent's plan for a comment.</summary>
public sealed record WorkbenchReviewTask(string Text, bool Done);
