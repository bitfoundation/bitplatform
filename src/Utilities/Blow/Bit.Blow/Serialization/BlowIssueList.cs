namespace Bit.Blow;

/// <summary>Collects an import's issues, reporting each limit once and stopping at a cap.</summary>
internal sealed class BlowIssueList(int max)
{
    private readonly HashSet<string> _once = new(StringComparer.Ordinal);

    public List<BlowImportIssue> Issues { get; } = [];

    public bool Full { get; private set; }

    public void Add(BlowImportIssue issue)
    {
        if (Full)
        {
            // An error left out still fails the import: the summary standing for it becomes one.
            if (issue.Severity == BlowValidationSeverity.Error && Issues[^1].Severity != BlowValidationSeverity.Error)
                Issues[^1] = Issues[^1] with { Severity = BlowValidationSeverity.Error };
            return;
        }
        if (Issues.Count >= Math.Max(1, max))
        {
            Full = true;
            Issues.Add(new BlowImportIssue(BlowImportCodes.TooMany, issue.Severity == BlowValidationSeverity.Error ? BlowValidationSeverity.Error : BlowValidationSeverity.Warning,
                issue.Path, $"More than {max} issues; the rest are not listed."));
            return;
        }
        Issues.Add(issue);
    }

    /// <summary>Adds the issue the first time its key comes up only.</summary>
    public void Once(string key, BlowImportIssue issue)
    {
        if (_once.Add(key)) Add(issue);
    }
}
