namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewReplyRequest(string Text, string? Agent);

public sealed record WorkbenchReviewStatusRequest(string Status, string? Agent);

public sealed record WorkbenchReviewTasksRequest(WorkbenchReviewTask[] Tasks, string? Agent);

public sealed record WorkbenchFlowRequest(string Name, WorkbenchFlowStepRequest[] Steps, string? Agent);

public sealed record WorkbenchFlowStepRequest(string Page, string? Query, string? Note);
