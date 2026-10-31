namespace Boilerplate.Workbench.Components.Shell;

public partial class TextPanel
{
    private bool isSaving;
    private WorkbenchTextMatch? selected;
    private WorkbenchTextTranslation[] translations = [];
    private Dictionary<string, string?> values = [];
    private List<BitDropdownItem<string>> matchItems = [];

    [AutoInject] private WorkbenchTextEdits edits = default!;

    [Parameter] public string? Text { get; set; }

    [Parameter] public WorkbenchTextMatch[] Matches { get; set; } = [];

    [Parameter] public bool CanSave { get; set; }

    [Parameter] public string? Culture { get; set; }

    [Parameter] public EventCallback OnSaved { get; set; }

    [Parameter] public EventCallback<string> OnAskAgent { get; set; }

    private string? SelectedId => selected is null ? null : $"{selected.Resource}/{selected.Key}";

    private bool IsChanged => translations.Any(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty));

    protected override void OnParametersSet()
    {
        matchItems = [.. Matches.Select(m => new BitDropdownItem<string> { Value = $"{m.Resource}/{m.Key}", Text = $"{m.Key} · {m.Value}" })];

        if (selected is null || Matches.Contains(selected) is false)
        {
            Select(Matches.Length > 0 ? $"{Matches[0].Resource}/{Matches[0].Key}" : null);
        }
    }

    private void Select(string? id)
    {
        selected = Matches.FirstOrDefault(m => $"{m.Resource}/{m.Key}" == id);
        translations = selected is null ? [] : [.. edits.Translations(selected.Resource, selected.Key).OrderBy(t => t.Culture == (Culture ?? CultureInfoManager.DefaultCulture.Name) ? 0 : 1)];
        values = translations.ToDictionary(t => t.Culture, t => t.Value);
    }

    private async Task Save()
    {
        if (selected is null || CanSave is false)
            return;

        isSaving = true;

        try
        {
            foreach (var translation in translations.Where(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty)))
            {
                await edits.Save(selected.Resource, selected.Key, translation.Culture, values[translation.Culture] ?? string.Empty);
            }

            Select(SelectedId);
            await OnSaved.InvokeAsync();
        }
        finally
        {
            isSaving = false;
        }
    }

    private Task Ask()
    {
        if (selected is null)
            return Task.CompletedTask;

        var changes = translations.Where(t => (values.GetValueOrDefault(t.Culture) ?? string.Empty) != (t.Value ?? string.Empty))
                                  .Select(t => $"{t.DisplayName}: “{t.Value}” → “{values[t.Culture]}”");

        return OnAskAgent.InvokeAsync($"{Localizer[nameof(WorkbenchStrings.ChangeTextRequest), selected.Resource, selected.Key]}\n{string.Join('\n', changes)}");
    }
}
