namespace Boilerplate.Workbench.Infrastructure.Models;

/// <summary>
/// What an agent sends to start a task or to change it, over HTTP. Only what it holds changes: the steps, notes and
/// questions are matched by id, a new id adds one, and the log line goes into the activity, marked by its kind. The
/// report is the task's write-up in Markdown, sent whole each time, and an empty one takes it away.
/// </summary>
public sealed record WorkbenchProgressUpdate(string? Agent,
                                             string? Title,
                                             string? Summary,
                                             string? Status,
                                             WorkbenchProgressStepUpdate[]? Steps,
                                             WorkbenchProgressNoteUpdate[]? Notes,
                                             WorkbenchProgressLinkUpdate[]? Links,
                                             WorkbenchProgressAskUpdate[]? Asks,
                                             string? Log,
                                             string? LogKind = null,
                                             string? Report = null);

/// <summary>A step to add or change; <c>owner</c> is <c>agent</c> or <c>you</c>, <c>remove</c> takes it away.</summary>
public sealed record WorkbenchProgressStepUpdate(string Id, string? Text, string? Owner, string? Status, string? Detail, string? Link, bool Remove = false);

/// <summary>A note to add or change; one without an id is added, <c>remove</c> takes it away.</summary>
public sealed record WorkbenchProgressNoteUpdate(string? Id, string? Kind, string? Text, bool Remove = false);

/// <summary>A link, matched by its label; <c>remove</c> takes it away.</summary>
public sealed record WorkbenchProgressLinkUpdate(string Label, string? Url, bool Remove = false);

/// <summary>A question to ask or change while it is unanswered; <c>withdraw</c> takes back one that no longer matters.</summary>
public sealed record WorkbenchProgressAskUpdate(string Id, string? Question, string? Detail, string? Kind, string[]? Options, string? Placeholder, bool Withdraw = false, WorkbenchProgressSecretUpdate? Secret = null);

/// <summary>Where a secret question's answer goes: <c>to</c> is <c>github</c>, with the secret's <c>name</c> and its <c>environment</c> unless it is the repository's, or <c>file</c>.</summary>
public sealed record WorkbenchProgressSecretUpdate(string? To, string? Name, string? Environment);
