using System.Text;

namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Checks tab: runs axe-core and the search for colors that don't come from the theme inside the selected view's
/// frame, on the page and on each part of it the checks can open, like its tabs, panels and menus, and lists what they
/// find with where each was found. A finding goes to the agent as a review comment pinned on its element, and all of
/// them go out as one prompt for an AI agent.
/// </summary>
public partial class ChecksPanel
{
    private bool isRunning;
    private bool hasFailed;
    private bool isAsking;
    private bool isExported;
    // Where the checks are and how many states of the page they have checked, while they run.
    private string? progress;
    private int checkedCount;
    // The view the result was found on, as CurrentView put it then.
    private string? checkedView;
    private WorkbenchCheckResult? result;
    // The findings of the result already sent to the agent.
    private readonly HashSet<object> asked = [];

    [AutoInject] private Clipboard clipboard = default!;

    [Parameter] public string PaneId { get; set; } = default!;

    [Parameter] public string PageKey { get; set; } = default!;

    [Parameter] public string PageTitle { get; set; } = default!;

    /// <summary>The selected view's scenario as a query, so a result is dropped once the view shows something else.</summary>
    [Parameter] public string Query { get; set; } = default!;

    /// <summary>The page switches the page changes by itself, like the tab it shows, which the checks go through on their own.</summary>
    [Parameter] public IReadOnlySet<string> InPageSwitches { get; set; } = new HashSet<string>();

    /// <summary>
    /// Sends a request about an element to the agent as a review comment, with the element's selector and the presses
    /// that open the part of the page it is in; true once sent. Null for a viewer who can't comment.
    /// </summary>
    [Parameter] public Func<string, string, WorkbenchCheckStep[], Task<bool>>? AskAgent { get; set; }

    private string CurrentView => $"{PaneId}|{PageKey}{Query}";

    protected override void OnParametersSet()
    {
        // A result belongs to the view it ran on, so selecting or changing the view clears it; moving between the parts
        // of the page the checks went through, like its tabs, keeps it.
        if (checkedView is not null && IsSameView(checkedView, CurrentView) is false)
        {
            result = null;
            checkedView = null;
            asked.Clear();
        }
    }

    /// <summary>Runs the checks in the view's frame; a null result means the frame didn't answer in time.</summary>
    private async Task Run()
    {
        var view = CurrentView;
        using var progressReference = DotNetObjectReference.Create(this);

        isRunning = true;
        hasFailed = false;
        progress = null;
        checkedCount = 0;

        try
        {
            var found = await JSRuntime.InvokeAsync<WorkbenchCheckResult?>("workbench.checkFrame", TimeSpan.FromMinutes(11), PaneId, progressReference);

            // The view changed while the checks ran, so the result describes a view that is no longer shown.
            if (IsSameView(view, CurrentView) is false)
                return;

            result = found;
            checkedView = view;
            hasFailed = found is null;
            asked.Clear();
        }
        catch (Exception exception) when (exception is JSException or TaskCanceledException)
        {
            hasFailed = true;
        }
        finally
        {
            isRunning = false;
            progress = null;
        }
    }

    /// <summary>Shows where the checks are, as the view's frame opens one part of the page after another.</summary>
    [JSInvokable]
    public void ShowProgress(string where, int checkedSoFar)
    {
        progress = string.IsNullOrEmpty(where) ? null : where;
        checkedCount = checkedSoFar;
        StateHasChanged();
    }

    /// <summary>Outlines the element a finding names inside the view's frame, first opening the part of the page it was found in.</summary>
    private async Task Highlight(string selector, WorkbenchCheckStep[] steps)
    {
        try
        {
            await JSRuntime.InvokeAsync<bool>("workbench.highlightInFrame", PaneId, selector, steps);
        }
        catch (JSException)
        {
            // The frame has moved on, so there is nothing left to outline.
        }
    }

    /// <summary>Sends one finding to the agent as a review comment pinned on its element.</summary>
    private async Task Ask(object finding)
    {
        if (AskAgent is null || isAsking)
            return;

        var (text, selector, steps) = finding switch
        {
            WorkbenchAccessibilityFinding accessibility => (Describe(accessibility, Localizer[nameof(WorkbenchStrings.ChecksAskAccessibility)], isMarkdown: false), accessibility.Nodes[0].Target, accessibility.Steps),
            WorkbenchThemeFinding theme => (Describe(theme, Localizer[nameof(WorkbenchStrings.ChecksAskTheme)], isMarkdown: false), theme.Target, theme.Steps),
            _ => throw new ArgumentOutOfRangeException(nameof(finding))
        };

        isAsking = true;

        try
        {
            if (await AskAgent(text, selector, steps))
            {
                asked.Add(finding);
            }
        }
        catch (JSException)
        {
            // The frame has moved on, so the finding can't be pinned on it.
        }
        finally
        {
            isAsking = false;
        }
    }

    /// <summary>Copies a prompt with every finding and where it is, for an AI agent; a browser that refuses the clipboard gets it as a file.</summary>
    private async Task ExportAll()
    {
        if (result is null)
            return;

        var prompt = BuildPrompt(result);

        // A browser can refuse the clipboard to a copy that arrives after a server round trip; Safari always does.
        try
        {
            await clipboard.WriteText(prompt);
        }
        catch (JSException)
        {
            await JSRuntime.InvokeVoidAsync("workbench.saveText", $"{PageKey}-checks.md", prompt);
            return;
        }

        isExported = true;
        StateHasChanged();

        await Task.Delay(1500);

        isExported = false;
        StateHasChanged();
    }

    /// <summary>The prompt of every finding, in Markdown: what the view is, what to do, then each finding with where it is.</summary>
    private string BuildPrompt(WorkbenchCheckResult found)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(CultureInfo.InvariantCulture, $"# {Localizer[nameof(WorkbenchStrings.ChecksPromptTitle), PageTitle]}").AppendLine();
        prompt.AppendLine(Localizer[nameof(WorkbenchStrings.ChecksPromptIntro), PageTitle, $"`/workbench/{PageKey}{Query}`"]).AppendLine();
        prompt.AppendLine(Localizer[nameof(WorkbenchStrings.ChecksPromptAsk), $"`/api/checks/{PageKey}{Query}`"]).AppendLine();

        if (found.Accessibility.Length > 0)
        {
            prompt.AppendLine(CultureInfo.InvariantCulture, $"## {Localizer[nameof(WorkbenchStrings.AccessibilityFindings), found.Accessibility.Length]}").AppendLine();

            foreach (var (finding, index) in found.Accessibility.Select((finding, index) => (finding, index + 1)))
            {
                prompt.AppendLine(CultureInfo.InvariantCulture, $"{index}. {Describe(finding, null, isMarkdown: true).Replace("\n", "\n   ", StringComparison.Ordinal)}").AppendLine();
            }
        }

        if (found.Theme.Length > 0)
        {
            prompt.AppendLine(CultureInfo.InvariantCulture, $"## {Localizer[nameof(WorkbenchStrings.ThemeFindings), found.Theme.Length]}").AppendLine();

            foreach (var (finding, index) in found.Theme.Select((finding, index) => (finding, index + 1)))
            {
                prompt.AppendLine(CultureInfo.InvariantCulture, $"{index}. {Describe(finding, null, isMarkdown: true).Replace("\n", "\n   ", StringComparison.Ordinal)}").AppendLine();
            }
        }

        return prompt.ToString().TrimEnd() + "\n";
    }

    /// <summary>
    /// An accessibility finding as text: what is wrong, where it is and the elements, each with its markup and what axe-core
    /// says about it, then the rule's page. Markdown marks code; a comment is plain text.
    /// </summary>
    private string Describe(WorkbenchAccessibilityFinding finding, string? request, bool isMarkdown)
    {
        var text = new StringBuilder();

        if (request is not null)
        {
            text.Append(request).Append("\n\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"{Strong(ImpactOf(finding), isMarkdown)} · {finding.Help} ({Code(finding.Rule, isMarkdown)}) × {finding.Count}");

        if (finding.Where is not null)
        {
            text.Append("\n- ").Append(Localizer[nameof(WorkbenchStrings.ChecksPromptWhere), finding.Where]);
        }

        foreach (var node in finding.Nodes)
        {
            text.Append("\n- ").Append(Code(node.Target, isMarkdown));

            if (node.Html.Length > 0)
            {
                text.Append("\n  ").Append(Code(node.Html.ReplaceLineEndings(" "), isMarkdown));
            }

            if (node.Summary.Length > 0)
            {
                text.Append("\n  ").Append(node.Summary);
            }
        }

        return text.Append("\n- ").Append(Localizer[nameof(WorkbenchStrings.ChecksPromptRule), finding.HelpUrl]).ToString();
    }

    /// <summary>A color outside the theme as text: the property and color, how often, where, and an element that uses it.</summary>
    private string Describe(WorkbenchThemeFinding finding, string? request, bool isMarkdown)
    {
        var text = new StringBuilder();

        if (request is not null)
        {
            text.Append(request).Append("\n\n");
        }

        text.Append(CultureInfo.InvariantCulture, $"{Code($"{finding.Property}: {finding.Color}", isMarkdown)} × {finding.Count}");

        if (finding.Where is not null)
        {
            text.Append("\n- ").Append(Localizer[nameof(WorkbenchStrings.ChecksPromptWhere), finding.Where]);
        }

        return text.Append("\n- ").Append(Localizer[nameof(WorkbenchStrings.ChecksPromptExample), finding.Example, Code(finding.Target, isMarkdown)]).ToString();
    }

    private string ImpactOf(WorkbenchAccessibilityFinding finding) => Localizer[$"Impact{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(finding.Impact)}"];

    private static string Code(string value, bool isMarkdown) => isMarkdown is false ? value : value.Contains('`', StringComparison.Ordinal) ? $"`` {value} ``" : $"`{value}`";

    private static string Strong(string value, bool isMarkdown) => isMarkdown ? $"**{value}**" : value;

    /// <summary>Whether two views are the same one, whatever the switches the page changes by itself are set to.</summary>
    private bool IsSameView(string first, string second) => WithoutInPageSwitches(first) == WithoutInPageSwitches(second);

    /// <summary>A view's address without the query keys of the switches the page changes by itself.</summary>
    private string WithoutInPageSwitches(string view)
    {
        var queryStart = view.IndexOf('?', StringComparison.Ordinal);

        if (queryStart < 0 || InPageSwitches.Count == 0)
            return view;

        var names = InPageSwitches.Select(key => $"{WorkbenchScenario.SwitchQueryPrefix}{key}=").ToArray();
        var kept = view[(queryStart + 1)..].Split('&').Where(part => names.Any(name => part.StartsWith(name, StringComparison.Ordinal)) is false);

        return kept.Any() ? $"{view[..queryStart]}?{string.Join('&', kept)}" : view[..queryStart];
    }
}
