using System.Globalization;
using System.Text.RegularExpressions;
using Bit.Bcrdt;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Reads and writes the agents' tasks in the review's Bcrdt document, one map per task under <c>progress</c>. The agent
/// changes a task over HTTP; the user answers its questions, finishes their own steps and writes to it from the page.
/// Texts and lists are cut at fixed sizes, so a chatty agent can't make the document grow without bound.
/// </summary>
public static partial class WorkbenchProgressDocument
{
    public const string MapName = "progress";

    public const int MaxTitleLength = 120;
    public const int MaxSummaryLength = 600;
    public const int MaxTextLength = 2000;
    public const int MaxReportLength = 30000;
    public const int MaxIdLength = 40;
    public const int MaxSteps = 60;
    public const int MaxNotes = 30;
    public const int MaxLinks = 12;
    public const int MaxAsks = 40;
    public const int MaxOptions = 8;
    public const int MaxFeed = 200;
    public const int MaxTasks = 50;

    /// <summary>The choices a confirmation is answered with.</summary>
    public const string Yes = "yes";
    public const string No = "no";

    public static List<WorkbenchProgressTask> ReadTasks(BcrdtMap progress)
    {
        return [.. progress.Keys.ToArray()
                                .Select(id => ReadTask(progress, id))
                                .OfType<WorkbenchProgressTask>()
                                .OrderByDescending(t => t.UpdatedAt)];
    }

    public static WorkbenchProgressTask? ReadTask(BcrdtMap progress, string id)
    {
        if (progress.GetMap(id) is not { } task)
            return null;

        return new(id,
                   ReadString(task, "title") ?? id,
                   ReadString(task, "summary"),
                   ReadString(task, "agent") ?? "Agent",
                   Parse(ReadString(task, "status"), WorkbenchProgressStatus.Working),
                   ReadTime(task, "createdAt") ?? DateTimeOffset.MinValue,
                   ReadTime(task, "updatedAt") ?? DateTimeOffset.MinValue,
                   ReadTime(task, "seenAt"),
                   [.. ReadMaps(task.GetArray("steps")).Select(step => new WorkbenchProgressStep(ReadString(step, "id") ?? string.Empty,
                                                                                                 ReadString(step, "text") ?? string.Empty,
                                                                                                 Parse(ReadString(step, "owner"), WorkbenchProgressOwner.Agent),
                                                                                                 Parse(ReadString(step, "status"), WorkbenchProgressStepStatus.Todo),
                                                                                                 ReadString(step, "detail"),
                                                                                                 ReadString(step, "link")))],
                   [.. ReadMaps(task.GetArray("notes")).Select(note => new WorkbenchProgressNote(ReadString(note, "id") ?? string.Empty,
                                                                                                 Parse(ReadString(note, "kind"), WorkbenchProgressNoteKind.Info),
                                                                                                 ReadString(note, "text") ?? string.Empty))],
                   [.. ReadMaps(task.GetArray("links")).Select(link => new WorkbenchProgressLink(ReadString(link, "label") ?? string.Empty, ReadString(link, "url") ?? string.Empty))],
                   [.. ReadMaps(task.GetArray("asks")).Select(ReadAsk)],
                   [.. ReadMaps(task.GetArray("feed")).Select(entry => new WorkbenchProgressEntry(Parse(ReadString(entry, "kind"), WorkbenchProgressEntryKind.Update),
                                                                                                  ReadString(entry, "author") ?? string.Empty,
                                                                                                  ReadString(entry, "text") ?? string.Empty,
                                                                                                  ReadTime(entry, "at") ?? DateTimeOffset.MinValue,
                                                                                                  ReadLong(entry, "seq"),
                                                                                                  ReadString(entry, "ref")) { Tone = ParseTone(ReadString(entry, "tone")) })])
        {
            Report = ReadString(task, "report")
        };
    }

    private static WorkbenchProgressAsk ReadAsk(BcrdtMap ask)
    {
        WorkbenchProgressAnswer? answer = ReadTime(ask, "answeredAt") is { } answeredAt
            ? new([.. ReadStrings(ask.GetArray("choices"))], ReadString(ask, "answer"), ReadString(ask, "answeredBy") ?? string.Empty, answeredAt)
            : null;

        return new(ReadString(ask, "id") ?? string.Empty,
                   ReadString(ask, "question") ?? string.Empty,
                   ReadString(ask, "detail"),
                   Parse(ReadString(ask, "kind"), WorkbenchProgressAskKind.Text),
                   [.. ReadStrings(ask.GetArray("options"))],
                   ReadString(ask, "placeholder"),
                   ReadTime(ask, "at") ?? DateTimeOffset.MinValue,
                   answer,
                   ReadBool(ask, "withdrawn"))
        {
            Secret = ReadString(ask, "secretTo") is { } to && TryParse<WorkbenchProgressSecretTo>(to, out var target)
                ? new(target, ReadString(ask, "secretName"), ReadString(ask, "secretEnvironment"))
                : null
        };
    }

    /// <summary>What is wrong with an agent's update, or null when it can be applied; <paramref name="existing"/> is null for a new task.</summary>
    public static string? Check(WorkbenchProgressUpdate update, WorkbenchProgressTask? existing)
    {
        if (existing is null && string.IsNullOrWhiteSpace(update.Title))
            return "Give the task a title.";

        if (update.Status is { } status && TryParse<WorkbenchProgressStatus>(status, out _) is false)
            return $"The status \"{status}\" is none of working, done, failed or stopped.";

        if (update.LogKind is { } logKind && TryParse<WorkbenchProgressNoteKind>(logKind, out _) is false)
            return $"The log kind \"{logKind}\" is none of info, warning, decision, success or error.";

        foreach (var step in update.Steps ?? [])
        {
            if (IsId(step.Id) is false)
                return $"Give every step an id of up to {MaxIdLength} characters.";

            if (step.Remove)
                continue;

            if (existing?.Steps.Any(s => s.Id == step.Id) is not true && string.IsNullOrWhiteSpace(step.Text))
                return $"The new step \"{step.Id}\" needs a text.";

            if (step.Owner is { } owner && TryParse<WorkbenchProgressOwner>(owner, out _) is false)
                return $"The owner of the step \"{step.Id}\" is neither agent nor you.";

            if (step.Status is { } stepStatus && TryParse<WorkbenchProgressStepStatus>(stepStatus, out _) is false)
                return $"The status of the step \"{step.Id}\" is none of todo, doing, done, blocked or skipped.";

            if (step.Link is { Length: > 0 } link && IsSafeUrl(link) is false)
                return $"The link of the step \"{step.Id}\" is neither an http(s) address nor a path of the Workbench.";
        }

        foreach (var note in update.Notes ?? [])
        {
            if (note.Id is not null && IsId(note.Id) is false)
                return $"A note's id is up to {MaxIdLength} characters.";

            if (note.Remove)
                continue;

            if (existing?.Notes.Any(n => n.Id == note.Id) is not true && string.IsNullOrWhiteSpace(note.Text))
                return "A new note needs a text.";

            if (note.Kind is { } kind && TryParse<WorkbenchProgressNoteKind>(kind, out _) is false)
                return $"The kind \"{kind}\" is none of info, warning, decision, success or error.";
        }

        foreach (var link in update.Links ?? [])
        {
            if (string.IsNullOrWhiteSpace(link.Label))
                return "Give every link a label.";

            if (link.Remove is false && (string.IsNullOrWhiteSpace(link.Url) || IsSafeUrl(link.Url) is false))
                return $"The link \"{link.Label}\" needs an http(s) address or a path of the Workbench.";
        }

        foreach (var ask in update.Asks ?? [])
        {
            if (IsId(ask.Id) is false)
                return $"Give every question an id of up to {MaxIdLength} characters.";

            var asked = existing?.Asks.FirstOrDefault(a => a.Id == ask.Id);

            // An answered question stays as it was answered, so sending it again changes nothing.
            if (ask.Withdraw || asked?.Answer is not null)
                continue;

            if (asked is null && string.IsNullOrWhiteSpace(ask.Question))
                return $"The new question \"{ask.Id}\" needs a text.";

            var kind = WorkbenchProgressAskKind.Text;

            if (ask.Kind is { } kindName && TryParse(kindName, out kind) is false)
                return $"The kind of the question \"{ask.Id}\" is none of text, choice, choices, confirm or secret.";

            kind = ask.Kind is null ? ask.Secret is null ? asked?.Kind ?? WorkbenchProgressAskKind.Text : WorkbenchProgressAskKind.Secret : kind;

            var options = Options(ask.Options) ?? asked?.Options ?? [];

            if (kind is WorkbenchProgressAskKind.Choice or WorkbenchProgressAskKind.Choices && options.Length is < 2 or > MaxOptions)
                return $"The question \"{ask.Id}\" needs 2 to {MaxOptions} options.";

            if (kind is WorkbenchProgressAskKind.Secret && CheckSecret(ask, asked) is { } secretProblem)
                return secretProblem;
        }

        var steps = (existing?.Steps.Select(s => s.Id) ?? []).Union((update.Steps ?? []).Where(s => s.Remove is false).Select(s => s.Id)).Count();

        if (steps > MaxSteps)
            return $"A task holds up to {MaxSteps} steps.";

        if ((existing?.Asks.Length ?? 0) + (update.Asks ?? []).Count(a => existing?.Asks.Any(x => x.Id == a.Id) is not true) > MaxAsks)
            return $"A task holds up to {MaxAsks} questions.";

        return null;
    }

    /// <summary>What is wrong with where a secret question's answer goes, or null when it can go there.</summary>
    private static string? CheckSecret(WorkbenchProgressAskUpdate ask, WorkbenchProgressAsk? asked)
    {
        if (SecretAskId().IsMatch(ask.Id.Trim()) is false)
            return $"The secret question \"{ask.Id}\" needs an id of letters, digits, - and _.";

        if (ask.Secret is null)
            return asked?.Secret is null ? $"The secret question \"{ask.Id}\" needs a secret saying where it goes: to github, with its name, or to file." : null;

        if (ask.Secret.To is not { } to || TryParse<WorkbenchProgressSecretTo>(to, out var target) is false)
            return $"The secret of the question \"{ask.Id}\" goes to neither github nor file.";

        if (target is WorkbenchProgressSecretTo.GitHub
            && (ask.Secret.Name is not { } name || GitHubSecretName().IsMatch(name) is false || name.StartsWith("GITHUB_", StringComparison.OrdinalIgnoreCase)))
            return $"The secret of the question \"{ask.Id}\" needs a GitHub secret's name: letters, digits and _, starting with neither a digit nor GITHUB_.";

        if (target is WorkbenchProgressSecretTo.GitHub && ask.Secret.Environment is { } environment && GitHubEnvironment().IsMatch(environment) is false)
            return $"The environment \"{environment}\" of the question \"{ask.Id}\" isn't a GitHub environment's name.";

        return null;
    }

    /// <summary>Starts a task from a checked update.</summary>
    public static void Create(BcrdtMap progress, string id, string agent, WorkbenchProgressUpdate update)
    {
        var task = new BcrdtMap();
        var now = Now();

        task.Set("createdAt", now);
        task.Set("status", nameof(WorkbenchProgressStatus.Working));
        task.Set("steps", new BcrdtArray());
        task.Set("notes", new BcrdtArray());
        task.Set("links", new BcrdtArray());
        task.Set("asks", new BcrdtArray());
        task.Set("feed", new BcrdtArray());

        progress.Set(id, task);

        Apply(progress, id, agent, update);
        RemoveOldest(progress);
    }

    /// <summary>Applies a checked update to a task; returns whether the task was found.</summary>
    public static bool Apply(BcrdtMap progress, string id, string agent, WorkbenchProgressUpdate update)
    {
        if (progress.GetMap(id) is not { } task)
            return false;

        if (string.IsNullOrWhiteSpace(update.Title) is false) task.Set("title", Trim(update.Title, MaxTitleLength));
        if (update.Summary is not null) task.Set("summary", Trim(update.Summary, MaxSummaryLength));
        if (update.Status is { } status && TryParse<WorkbenchProgressStatus>(status, out var parsed)) task.Set("status", parsed.ToString());
        if (update.Report is not null) task.Set("report", Blank(Trim(update.Report, MaxReportLength)));

        if (task.GetArray("steps") is { } steps)
        {
            foreach (var step in update.Steps ?? [])
            {
                ApplyStep(steps, step);
            }
        }

        if (task.GetArray("notes") is { } notes)
        {
            foreach (var note in update.Notes ?? [])
            {
                ApplyNote(notes, note);
            }
        }

        if (task.GetArray("links") is { } links)
        {
            foreach (var link in update.Links ?? [])
            {
                ApplyLink(links, link);
            }
        }

        if (task.GetArray("asks") is { } asks)
        {
            foreach (var ask in update.Asks ?? [])
            {
                ApplyAsk(asks, ask);
            }
        }

        if (string.IsNullOrWhiteSpace(update.Log) is false)
        {
            AddEntry(task, WorkbenchProgressEntryKind.Update, agent, update.Log, 0, null, ParseTone(update.LogKind));
        }

        task.Set("agent", Trim(agent, WorkbenchReviewDocument.MaxNameLength));
        task.Set("updatedAt", Now());
        task.Set("seenAt", Now());

        return true;
    }

    /// <summary>Records that the agent of a task is still there, as a wait for the user does.</summary>
    public static bool Touch(BcrdtMap progress, string id, string agent)
    {
        if (progress.GetMap(id) is not { } task)
            return false;

        task.Set("agent", Trim(agent, WorkbenchReviewDocument.MaxNameLength));
        task.Set("seenAt", Now());

        return true;
    }

    /// <summary>
    /// The user's answer to an open question: the options picked (<see cref="Yes"/> or <see cref="No"/> for a confirmation)
    /// and what they wrote. Returns false when the question is gone, answered or the answer doesn't fit it.
    /// </summary>
    public static bool Answer(BcrdtMap progress, string taskId, string askId, IReadOnlyList<string> choices, string? text, string by)
    {
        if (FindOpenAsk(progress, taskId, askId) is not var (task, ask))
            return false;

        var kind = Parse(ReadString(ask, "kind"), WorkbenchProgressAskKind.Text);
        var options = ReadStrings(ask.GetArray("options")).ToArray();
        var picked = choices.Distinct(StringComparer.Ordinal).ToArray();

        // A secret is answered only by storing it, which MarkStored records.
        var fits = kind switch
        {
            WorkbenchProgressAskKind.Text => picked.Length == 0 && string.IsNullOrWhiteSpace(text) is false,
            WorkbenchProgressAskKind.Choice => picked.Length == 1 && options.Contains(picked[0]),
            WorkbenchProgressAskKind.Choices => picked.Length > 0 && picked.All(options.Contains),
            WorkbenchProgressAskKind.Secret => false,
            _ => picked is [Yes] or [No]
        };

        if (fits is false)
            return false;

        var list = new BcrdtArray();

        ask.Set("choices", list);
        list.PushRange(picked.Cast<object>());
        ask.Set("answer", string.IsNullOrWhiteSpace(text) ? null : Trim(text, MaxTextLength));
        ask.Set("answeredBy", Trim(by, WorkbenchReviewDocument.MaxNameLength));
        ask.Set("answeredAt", Now());

        AddEntry(task, WorkbenchProgressEntryKind.Answer, by, text ?? string.Empty, WorkbenchReviewDocument.NextSequence(), askId);
        task.Set("updatedAt", Now());

        return true;
    }

    /// <summary>
    /// Records that the Workbench stored the secret a question asked for. The secret itself never enters the document, so
    /// the agent learns only that it is where the question said. Returns false when the question is gone, answered or not a
    /// secret's.
    /// </summary>
    public static bool MarkStored(BcrdtMap progress, string taskId, string askId, string by)
    {
        if (FindOpenAsk(progress, taskId, askId) is not var (task, ask) || ReadString(ask, "kind") is not nameof(WorkbenchProgressAskKind.Secret))
            return false;

        ask.Set("choices", new BcrdtArray());
        ask.Set("answeredBy", Trim(by, WorkbenchReviewDocument.MaxNameLength));
        ask.Set("answeredAt", Now());

        AddEntry(task, WorkbenchProgressEntryKind.Answer, by, string.Empty, WorkbenchReviewDocument.NextSequence(), askId);
        task.Set("updatedAt", Now());

        return true;
    }

    /// <summary>A question of a running task that still waits for its answer, with that task.</summary>
    private static (BcrdtMap Task, BcrdtMap Ask)? FindOpenAsk(BcrdtMap progress, string taskId, string askId)
    {
        if (progress.GetMap(taskId) is not { } task || ReadString(task, "status") is not nameof(WorkbenchProgressStatus.Working))
            return null;

        if (FindItem(task.GetArray("asks"), askId) is not { } ask || ReadTime(ask, "answeredAt") is not null || ReadBool(ask, "withdrawn"))
            return null;

        return (task, ask);
    }

    /// <summary>Marks a step of the user's done, for the agent to go on; returns false when it isn't theirs or is done already.</summary>
    public static bool CompleteStep(BcrdtMap progress, string taskId, string stepId, string by)
    {
        if (progress.GetMap(taskId) is not { } task || ReadString(task, "status") is not nameof(WorkbenchProgressStatus.Working))
            return false;

        if (FindItem(task.GetArray("steps"), stepId) is not { } step
            || ReadString(step, "owner") is not nameof(WorkbenchProgressOwner.You)
            || ReadString(step, "status") is nameof(WorkbenchProgressStepStatus.Done) or nameof(WorkbenchProgressStepStatus.Skipped))
            return false;

        step.Set("status", nameof(WorkbenchProgressStepStatus.Done));

        AddEntry(task, WorkbenchProgressEntryKind.Step, by, ReadString(step, "text") ?? string.Empty, WorkbenchReviewDocument.NextSequence(), stepId);
        task.Set("updatedAt", Now());

        return true;
    }

    /// <summary>A message from the user to the task's agent.</summary>
    public static bool AddMessage(BcrdtMap progress, string taskId, string text, string by)
    {
        if (string.IsNullOrWhiteSpace(text) || progress.GetMap(taskId) is not { } task)
            return false;

        AddEntry(task, WorkbenchProgressEntryKind.Message, by, text, WorkbenchReviewDocument.NextSequence(), null);
        task.Set("updatedAt", Now());

        return true;
    }

    /// <summary>Writes a whole task as it is, times included; the samples and the tests start from one.</summary>
    public static void Write(BcrdtMap progress, WorkbenchProgressTask task)
    {
        var map = new BcrdtMap();
        var steps = new BcrdtArray();
        var notes = new BcrdtArray();
        var links = new BcrdtArray();
        var asks = new BcrdtArray();
        var feed = new BcrdtArray();

        map.Set("title", task.Title);
        map.Set("summary", task.Summary);
        map.Set("agent", task.Agent);
        map.Set("status", task.Status.ToString());
        map.Set("createdAt", Format(task.CreatedAt));
        map.Set("updatedAt", Format(task.UpdatedAt));
        map.Set("seenAt", task.SeenAt is { } seen ? Format(seen) : null);
        map.Set("report", task.Report);
        map.Set("steps", steps);
        map.Set("notes", notes);
        map.Set("links", links);
        map.Set("asks", asks);
        map.Set("feed", feed);

        progress.Set(task.Id, map);

        foreach (var step in task.Steps)
        {
            steps.Push(NewMap(("id", step.Id), ("text", step.Text), ("owner", step.Owner.ToString()), ("status", step.Status.ToString()), ("detail", step.Detail), ("link", step.Link)));
        }

        foreach (var note in task.Notes)
        {
            notes.Push(NewMap(("id", note.Id), ("kind", note.Kind.ToString()), ("text", note.Text)));
        }

        foreach (var link in task.Links)
        {
            links.Push(NewMap(("label", link.Label), ("url", link.Url)));
        }

        foreach (var ask in task.Asks)
        {
            var item = NewMap(("id", ask.Id), ("question", ask.Question), ("detail", ask.Detail), ("kind", ask.Kind.ToString()), ("placeholder", ask.Placeholder), ("at", Format(ask.At)), ("withdrawn", ask.IsWithdrawn),
                              ("secretTo", ask.Secret?.To.ToString()), ("secretName", ask.Secret?.Name), ("secretEnvironment", ask.Secret?.Environment));
            var options = new BcrdtArray();

            asks.Push(item);
            item.Set("options", options);
            options.PushRange(ask.Options.Cast<object>());

            if (ask.Answer is { } answer)
            {
                var choices = new BcrdtArray();

                item.Set("choices", choices);
                choices.PushRange(answer.Choices.Cast<object>());
                item.Set("answer", answer.Text);
                item.Set("answeredBy", answer.By);
                item.Set("answeredAt", Format(answer.At));
            }
        }

        foreach (var entry in task.Feed)
        {
            feed.Push(NewMap(("kind", entry.Kind.ToString()), ("author", entry.Author), ("text", entry.Text), ("at", Format(entry.At)), ("seq", entry.Sequence), ("ref", entry.RefId), ("tone", entry.Tone?.ToString())));
        }
    }

    private static void ApplyStep(BcrdtArray steps, WorkbenchProgressStepUpdate update)
    {
        var step = FindItem(steps, update.Id);

        if (update.Remove)
        {
            RemoveItem(steps, update.Id);
            return;
        }

        if (step is null)
        {
            if (steps.Count >= MaxSteps)
                return;

            step = NewMap(("id", update.Id.Trim()), ("owner", nameof(WorkbenchProgressOwner.Agent)), ("status", nameof(WorkbenchProgressStepStatus.Todo)));
            steps.Push(step);
        }

        if (string.IsNullOrWhiteSpace(update.Text) is false) step.Set("text", Trim(update.Text, MaxTitleLength * 2));
        if (update.Owner is { } owner && TryParse<WorkbenchProgressOwner>(owner, out var parsedOwner)) step.Set("owner", parsedOwner.ToString());
        if (update.Status is { } status && TryParse<WorkbenchProgressStepStatus>(status, out var parsedStatus)) step.Set("status", parsedStatus.ToString());
        if (update.Detail is not null) step.Set("detail", Blank(Trim(update.Detail, MaxTextLength)));
        if (update.Link is not null) step.Set("link", Blank(update.Link.Trim()));
    }

    private static void ApplyNote(BcrdtArray notes, WorkbenchProgressNoteUpdate update)
    {
        var note = update.Id is null ? null : FindItem(notes, update.Id);

        if (update.Remove)
        {
            if (update.Id is not null)
            {
                RemoveItem(notes, update.Id);
            }

            return;
        }

        if (note is null)
        {
            if (notes.Count >= MaxNotes)
            {
                notes.Delete(0, 1);
            }

            note = NewMap(("id", update.Id?.Trim() ?? Guid.NewGuid().ToString("N")[..8]), ("kind", nameof(WorkbenchProgressNoteKind.Info)));
            notes.Push(note);
        }

        if (string.IsNullOrWhiteSpace(update.Text) is false) note.Set("text", Trim(update.Text, MaxTextLength));
        if (update.Kind is { } kind && TryParse<WorkbenchProgressNoteKind>(kind, out var parsedKind)) note.Set("kind", parsedKind.ToString());
    }

    private static void ApplyLink(BcrdtArray links, WorkbenchProgressLinkUpdate update)
    {
        var label = Trim(update.Label, 80)!;
        var index = IndexOf(links, link => ReadString(link, "label") == label);

        if (update.Remove)
        {
            if (index >= 0)
            {
                links.Delete(index, 1);
            }

            return;
        }

        if (index >= 0)
        {
            links.GetMap(index)!.Set("url", update.Url!.Trim());
        }
        else if (links.Count < MaxLinks)
        {
            links.Push(NewMap(("label", label), ("url", update.Url!.Trim())));
        }
    }

    private static void ApplyAsk(BcrdtArray asks, WorkbenchProgressAskUpdate update)
    {
        var ask = FindItem(asks, update.Id);

        if (ask is not null && ReadTime(ask, "answeredAt") is not null)
            return;

        if (update.Withdraw)
        {
            ask?.Set("withdrawn", true);
            return;
        }

        if (ask is null)
        {
            if (asks.Count >= MaxAsks)
                return;

            ask = NewMap(("id", update.Id.Trim()), ("kind", nameof(WorkbenchProgressAskKind.Text)), ("at", Now()), ("withdrawn", false));
            ask.Set("options", new BcrdtArray());
            asks.Push(ask);
        }

        if (string.IsNullOrWhiteSpace(update.Question) is false) ask.Set("question", Trim(update.Question, MaxSummaryLength));
        if (update.Detail is not null) ask.Set("detail", Blank(Trim(update.Detail, MaxTextLength)));
        if (update.Placeholder is not null) ask.Set("placeholder", Blank(Trim(update.Placeholder, MaxTitleLength)));
        if (update.Kind is { } kind && TryParse<WorkbenchProgressAskKind>(kind, out var parsedKind)) ask.Set("kind", parsedKind.ToString());

        if (update.Secret is { To: { } to } secret && TryParse<WorkbenchProgressSecretTo>(to, out var target))
        {
            ask.Set("secretTo", target.ToString());
            ask.Set("secretName", target is WorkbenchProgressSecretTo.GitHub ? secret.Name : null);
            ask.Set("secretEnvironment", target is WorkbenchProgressSecretTo.GitHub ? Blank(secret.Environment) : null);

            if (update.Kind is null)
            {
                ask.Set("kind", nameof(WorkbenchProgressAskKind.Secret));
            }
        }

        if (Options(update.Options) is { } options)
        {
            var list = new BcrdtArray();

            ask.Set("options", list);
            list.PushRange(options.Cast<object>());
        }

        // Asked again, it is new to the user again.
        ask.Set("withdrawn", false);
    }

    /// <summary>Adds a line to the activity, dropping the oldest once it is full.</summary>
    private static void AddEntry(BcrdtMap task, WorkbenchProgressEntryKind kind, string author, string text, long sequence, string? refId, WorkbenchProgressNoteKind? tone = null)
    {
        if (task.GetArray("feed") is not { } feed)
            return;

        if (feed.Count >= MaxFeed)
        {
            feed.Delete(0, feed.Count - MaxFeed + 1);
        }

        feed.Push(NewMap(("kind", kind.ToString()),
                         ("author", Trim(author, WorkbenchReviewDocument.MaxNameLength)),
                         ("text", Trim(text, MaxTextLength)),
                         ("at", Now()),
                         ("seq", sequence),
                         ("ref", refId),
                         ("tone", tone?.ToString())));
    }

    /// <summary>Keeps the number of tasks bounded by dropping the ones not touched for the longest.</summary>
    private static void RemoveOldest(BcrdtMap progress)
    {
        foreach (var task in ReadTasks(progress).Skip(MaxTasks))
        {
            progress.Delete(task.Id);
        }
    }

    /// <summary>The trimmed, distinct, non-empty options, or null when none were sent.</summary>
    private static string[]? Options(string[]? options)
    {
        return options is null ? null : [.. options.Where(o => string.IsNullOrWhiteSpace(o) is false).Select(o => Trim(o, MaxTitleLength)!).Distinct(StringComparer.Ordinal).Take(MaxOptions)];
    }

    /// <summary>An absolute http or https address, or a path of the Workbench itself.</summary>
    public static bool IsSafeUrl(string url)
    {
        url = url.Trim();

        if (url.StartsWith('/'))
            return url.StartsWith("//", StringComparison.Ordinal) is false && url.Contains('\\') is false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static WorkbenchProgressNoteKind? ParseTone(string? value) => value is not null && TryParse<WorkbenchProgressNoteKind>(value, out var tone) ? tone : null;

    private static bool IsId(string? id) => string.IsNullOrWhiteSpace(id) is false && id.Trim().Length <= MaxIdLength;

    private static bool TryParse<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result) && int.TryParse(value, out _) is false;
    }

    private static TEnum Parse<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
    {
        return value is not null && TryParse<TEnum>(value, out var result) ? result : fallback;
    }

    private static BcrdtMap NewMap(params (string Key, object? Value)[] values)
    {
        var map = new BcrdtMap();

        foreach (var (key, value) in values)
        {
            map.Set(key, value);
        }

        return map;
    }

    private static BcrdtMap? FindItem(BcrdtArray? array, string id) => ReadMaps(array).FirstOrDefault(item => ReadString(item, "id") == id.Trim());

    private static int IndexOf(BcrdtArray array, Func<BcrdtMap, bool> match)
    {
        for (var index = 0; index < array.Count; index++)
        {
            if (array.GetMap(index) is { } item && match(item))
                return index;
        }

        return -1;
    }

    private static void RemoveItem(BcrdtArray array, string id)
    {
        var index = IndexOf(array, item => ReadString(item, "id") == id.Trim());

        if (index >= 0)
        {
            array.Delete(index, 1);
        }
    }

    private static IEnumerable<BcrdtMap> ReadMaps(BcrdtArray? array)
    {
        if (array is null)
            yield break;

        for (var index = 0; index < array.Count; index++)
        {
            if (array.GetMap(index) is { } map)
                yield return map;
        }
    }

    private static IEnumerable<string> ReadStrings(BcrdtArray? array)
    {
        if (array is null)
            yield break;

        for (var index = 0; index < array.Count; index++)
        {
            if (array[index] is string value)
                yield return value;
        }
    }

    private static string Now() => Format(DateTimeOffset.UtcNow);

    private static string Format(DateTimeOffset time) => time.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? Trim(string? value, int length) => value is null ? null : value.Trim() is var trimmed && trimmed.Length > length ? trimmed[..length] : value.Trim();

    private static string? ReadString(BcrdtMap map, string key) => map.Get(key) as string;

    private static bool ReadBool(BcrdtMap map, string key) => map.Get(key) is true;

    private static long ReadLong(BcrdtMap map, string key) => map.Get(key) is { } value ? Convert.ToInt64(value, CultureInfo.InvariantCulture) : 0;

    private static DateTimeOffset? ReadTime(BcrdtMap map, string key) => DateTimeOffset.TryParse(ReadString(map, key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;

    // A secret question's id names the file its secret may go into.
    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex SecretAskId();

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]{0,99}$")]
    private static partial Regex GitHubSecretName();

    // Never starting with a dash, which gh would read as an option.
    [GeneratedRegex("^[A-Za-z0-9_][A-Za-z0-9_. /-]{0,99}$")]
    private static partial Regex GitHubEnvironment();
}
