namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchReviewReplyRequest(string Text, string? Agent);

public sealed record WorkbenchReviewStatusRequest(string Status, string? Agent);

public sealed record WorkbenchReviewTasksRequest(WorkbenchReviewTask[] Tasks, string? Agent);
