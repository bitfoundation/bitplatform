namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Shapes the agents' tasks into the JSON an agent reads: lower-case names, and no activity it wrote itself.</summary>
public static class WorkbenchProgressJson
{
    public static object DescribeList(IEnumerable<WorkbenchProgressTask> tasks)
    {
        return tasks.Select(task => new
        {
            task.Id,
            Page = WorkbenchUrls.ProgressTask(task.Id),
            task.Title,
            Status = Name(task.Status),
            task.Agent,
            Steps = new { Done = task.CompletedCount, Total = task.Steps.Length },
            NeedsYou = task.NeedsYouCount,
            task.UpdatedAt
        }).ToArray();
    }

    /// <summary>A task as the agent left it, with the user's answers and the cursor to wait from.</summary>
    public static object Describe(WorkbenchProgressTask task)
    {
        return new
        {
            task.Id,
            Page = WorkbenchUrls.ProgressTask(task.Id),
            task.Title,
            task.Summary,
            Status = Name(task.Status),
            task.Agent,
            task.Cursor,
            task.Report,
            Steps = task.Steps.Select(step => new { step.Id, step.Text, Owner = Name(step.Owner), Status = Name(step.Status), step.Detail, step.Link }).ToArray(),
            Notes = task.Notes.Select(note => new { note.Id, Kind = Name(note.Kind), note.Text }).ToArray(),
            Links = task.Links.Select(link => new { link.Label, link.Url }).ToArray(),
            Asks = task.Asks.Select(ask => new
            {
                ask.Id,
                ask.Question,
                Kind = Name(ask.Kind),
                ask.Options,
                Status = ask.Answer is not null ? "answered" : ask.IsWithdrawn ? "withdrawn" : "open",
                Answer = ask.Answer is { } answer ? new { answer.Choices, answer.Text, answer.By, answer.At } : null,
                Secret = DescribeSecret(task, ask)
            }).ToArray()
        };
    }

    /// <summary>Where a secret question's answer goes, with the file a file's goes into; never the secret.</summary>
    private static object? DescribeSecret(WorkbenchProgressTask task, WorkbenchProgressAsk? ask)
    {
        return ask?.Secret is { } secret
            ? new { To = Name(secret.To), secret.Name, secret.Environment, File = secret.To is WorkbenchProgressSecretTo.File ? WorkbenchSecrets.FilePath(task.Id, ask.Id) : null }
            : null;
    }

    /// <summary>What the user did since the cursor: answers, finished steps and messages, oldest first.</summary>
    public static object DescribeWait(long cursor, WorkbenchProgressTask task, IEnumerable<WorkbenchProgressEntry> events)
    {
        return new
        {
            Cursor = cursor,
            Status = Name(task.Status),
            Events = events.Select(entry =>
            {
                var ask = entry.Kind is WorkbenchProgressEntryKind.Answer ? task.Asks.FirstOrDefault(a => a.Id == entry.RefId) : null;

                return new
                {
                    Kind = entry.Kind switch
                    {
                        WorkbenchProgressEntryKind.Answer => "answer",
                        WorkbenchProgressEntryKind.Step => "step",
                        _ => "message"
                    },
                    Ask = ask?.Id,
                    Question = ask?.Question,
                    Choices = ask?.Answer?.Choices,
                    Secret = DescribeSecret(task, ask),
                    Step = entry.Kind is WorkbenchProgressEntryKind.Step ? entry.RefId : null,
                    Text = string.IsNullOrEmpty(entry.Text) ? null : entry.Text,
                    By = entry.Author,
                    entry.At
                };
            }).ToArray()
        };
    }

    private static string Name<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString().ToLowerInvariant();
}
