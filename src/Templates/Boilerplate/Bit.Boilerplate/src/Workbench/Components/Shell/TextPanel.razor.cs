namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// The Edit text panel: the text picked on the page, the resource entries that render it and its value in every
/// language, saved straight into the resource files on this machine or sent to the agent as a request.
/// </summary>
public partial class TextPanel
{
    private bool isSaving;
    private WorkbenchTextMatch? selected;
    private WorkbenchTextTranslation[] translations = [];
    private Dictionary<string, string?> values = [];
    private List<BitDropdownItem<string>> matchItems = [];

    [AutoInject] private WorkbenchTextEdits edits = default!;

    [Parameter] public string? Text { get; set; }

    /// <summary>The resource entries whose value is the picked text; more than one shows a key picker.</summary>
    [Parameter] public WorkbenchTextMatch[] Matches { get; set; } = [];

    /// <summary>Whether the translations can be written into the files: on this machine, with the project folder found.</summary>
    [Parameter] public bool CanSave { get; set; }

    /// <summary>The view's culture, whose translation is listed first.</summary>
    [Parameter] public string? Culture { get; set; }

    [Parameter] public EventCallback OnSaved { get; set; }

    [Parameter] public EventCallback<string> OnAskAgent { get; set; }

    /// <summary>The selected entry as resource/key, the value the key picker uses.</summary>
    private string? SelectedId => selected is null ? null : $"{selected.Resource}/{selected.Key}";

    private bool IsChanged => translations.Any(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty));

    protected override void OnParametersSet()
    {
        matchItems = [.. Matches.Select(m => new BitDropdownItem<string> { Value = $"{m.Resource}/{m.Key}", Text = $"{m.Key} · {m.Value}" })];

        // Keeps the selected entry, and the edits made to it, while it is still among the matches.
        if (selected is null || Matches.Contains(selected) is false)
        {
            Select(Matches.Length > 0 ? $"{Matches[0].Resource}/{Matches[0].Key}" : null);
        }
    }

    /// <summary>Selects an entry and loads its translations, the view's culture first.</summary>
    private void Select(string? id)
    {
        selected = Matches.FirstOrDefault(m => $"{m.Resource}/{m.Key}" == id);
        translations = selected is null ? [] : [.. edits.Translations(selected.Resource, selected.Key).OrderBy(t => t.Culture == (Culture ?? CultureInfoManager.DefaultCulture.Name) ? 0 : 1)];
        values = translations.ToDictionary(t => t.Culture, t => t.Value);
    }

    /// <summary>
    /// Writes the changed translations into the resource files, then reloads them. When a file can't be written, the
    /// edits stay in the panel, so they can be saved again.
    /// </summary>
    private async Task Save()
    {
        if (selected is null || CanSave is false)
            return;

        var changed = translations.Where(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty)).ToArray();

        // A text that drops or adds a {n} placeholder would break every page that formats it, so nothing is saved.
        if (changed.Any(t => edits.KeepsPlaceholders(selected.Resource, selected.Key, values[t.Culture] ?? string.Empty) is false))
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.TextKeepPlaceholders)]);
            return;
        }

        isSaving = true;

        try
        {
            foreach (var translation in changed)
            {
                if (await edits.Save(selected.Resource, selected.Key, translation.Culture, values[translation.Culture] ?? string.Empty) is false)
                {
                    SnackBarService.Error(Localizer[nameof(WorkbenchStrings.SaveTranslationsFailed)]);
                    return;
                }
            }

            Select(SelectedId);
            await OnSaved.InvokeAsync();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.SaveTranslationsFailed)], exception.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    /// <summary>Sends the changed translations to the agent as a comment instead of writing them.</summary>
    private Task Ask()
    {
        if (selected is null)
            return Task.CompletedTask;

        var changes = translations.Where(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty))
                                  .Select(t => $"{t.DisplayName}: “{t.Value}” → “{values[t.Culture]}”");

        return OnAskAgent.InvokeAsync($"{Localizer[nameof(WorkbenchStrings.ChangeTextRequest), selected.Resource, selected.Key]}\n{string.Join('\n', changes)}");
    }
}
