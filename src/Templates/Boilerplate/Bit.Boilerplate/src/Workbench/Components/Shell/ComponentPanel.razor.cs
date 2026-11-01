namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// Inspect mode's panel: the picked component's parameters, its own and the inherited ones, the parts of its Styles and
/// Classes, and the CSS variables its styles use. A change is written into the component's tag in the code, and hot
/// reload shows it in the views. Values code sets, like @Localizer[...], are shown but left alone.
/// </summary>
public partial class ComponentPanel
{
    // The choice for an enum parameter that the tag leaves out, so null applies.
    private const string NotSet = "~";

    private string? filter;
    private string? status;
    private bool isSaving;
    private string? shownElementId;
    private Dictionary<string, List<BitDropdownItem<string>>> choices = [];

    [AutoInject] private WorkbenchStore store = default!;
    [AutoInject] private WorkbenchComponentEdits edits = default!;

    /// <summary>Whether a click in a view picked something yet.</summary>
    [Parameter] public bool IsPicked { get; set; }

    /// <summary>The picked component; null when the click wasn't on a bit BlazorUI component, or the component is gone.</summary>
    [Parameter] public WorkbenchComponentInfo? Info { get; set; }

    [Parameter] public WorkbenchCssVariable[] Variables { get; set; } = [];

    /// <summary>Whether changes can be written into the code: on this machine, with the project folder found.</summary>
    [Parameter] public bool CanSave { get; set; }

    private WorkbenchComponentParameter? StyleParameter => Info?.Parameters.FirstOrDefault(p => p.Name is nameof(BitComponentBase.Style));

    /// <summary>A CSS variable is written into the component's Style, so it can change only while Style can.</summary>
    private bool CanEditVariables => StyleParameter is { } style && CanEdit(style);

    protected override void OnParametersSet()
    {
        // Another component was picked: what was said about the last one's change no longer applies.
        if (Info?.ElementId != shownElementId)
        {
            status = null;
            shownElementId = Info?.ElementId;
        }

        choices = Info?.Parameters.Where(p => p.Editor is WorkbenchParameterEditor.Choice).ToDictionary(p => p.Name, ItemsOf) ?? [];
    }

    /// <summary>An enum parameter's choices, after Not set when null is a value of its own.</summary>
    private List<BitDropdownItem<string>> ItemsOf(WorkbenchComponentParameter parameter)
    {
        List<BitDropdownItem<string>> items = [.. parameter.Choices.Select(choice => new BitDropdownItem<string> { Value = choice, Text = choice })];

        if (parameter.IsNullable)
        {
            items.Insert(0, new() { Value = NotSet, Text = Localizer[nameof(WorkbenchStrings.ComponentNotSet)] });
        }

        return items;
    }

    private List<BitDropdownItem<string>> ChoicesOf(WorkbenchComponentParameter parameter) => choices.GetValueOrDefault(parameter.Name) ?? [];

    /// <summary>The parameters that pass the filter, the ones the tag sets first.</summary>
    private IEnumerable<WorkbenchComponentParameter> Matching(bool inherited)
    {
        return (Info?.Parameters ?? []).Where(p => (p.InheritedFrom is not null) == inherited && p.Editor is not WorkbenchParameterEditor.Parts && Matches(p.Name))
                                       .OrderBy(p => p.IsSet || p.Code is not null ? 0 : 1);
    }

    private bool Matches(string name) => string.IsNullOrWhiteSpace(filter) || name.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a parameter can change here: its tag was found, code doesn't set it, its type has an editor and this machine can write.</summary>
    private bool CanEdit(WorkbenchComponentParameter parameter)
    {
        return CanSave && isSaving is false && Info?.Tag is not null && parameter.Code is null && parameter.Editor is not WorkbenchParameterEditor.ReadOnly;
    }

    /// <summary>Writes a parameter's value into the tag; null takes the attribute away, so the component's default applies.</summary>
    private Task Set(WorkbenchComponentParameter parameter, string? value)
    {
        if (Info?.Tag is not { } tag || CanEdit(parameter) is false || (value is not null && value == parameter.Value && parameter.IsSet))
            return Task.CompletedTask;

        return Save(tag, () => edits.Set(Info.Name, tag, parameter.Name, value));
    }

    private Task SetToggle(WorkbenchComponentParameter parameter, bool value) => Set(parameter, value ? "true" : "false");

    private Task SetPart(WorkbenchComponentParameter parameter, string partName, string? value)
    {
        if (Info?.Tag is not { } tag || CanEdit(parameter) is false)
            return Task.CompletedTask;

        var parts = parameter.Parts.Select(part => part.Name == partName ? part with { Value = value } : part).ToArray();

        return Save(tag, () => edits.SetParts(Info.Name, tag, parameter.Name, parts));
    }

    /// <summary>Sets a CSS variable in the component's Style, or takes it out when the value is cleared.</summary>
    private Task SetVariable(WorkbenchCssVariable variable, string? value)
    {
        if (StyleParameter is not { } style || CanEditVariables is false || value?.Trim() == variable.Value)
            return Task.CompletedTask;

        var declarations = (style.Value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                                       .Where(declaration => declaration.Split(':', 2)[0].Trim() != variable.Name)
                                                       .ToList();

        if (string.IsNullOrWhiteSpace(value) is false)
        {
            declarations.Add($"{variable.Name}: {value.Trim()}");
        }

        return Set(style, declarations.Count is 0 ? null : $"{string.Join("; ", declarations)};");
    }

    private async Task Save(WorkbenchComponentTag tag, Func<Task<bool>> write)
    {
        isSaving = true;

        try
        {
            if (await write())
            {
                status = Localizer[WorkbenchComponentEdits.IsHotReloading ? nameof(WorkbenchStrings.ComponentSavedHotReload) : nameof(WorkbenchStrings.ComponentSavedRestart), FileName(tag.File)];
            }
            else
            {
                SnackBarService.Error(Localizer[nameof(WorkbenchStrings.ComponentSaveFailed)]);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SnackBarService.Error(Localizer[nameof(WorkbenchStrings.ComponentSaveFailed)], exception.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];

    /// <summary>A variable's name without the --bit- every one of bit BlazorUI's starts with.</summary>
    private static string ShortName(string name) => name.StartsWith("--bit-", StringComparison.Ordinal) ? name["--bit-".Length..] : name;

    /// <summary>Whether a text parameter names an icon, like IconName, so it gets the icon picker.</summary>
    private static bool IsIcon(WorkbenchComponentParameter parameter) => parameter.Editor is WorkbenchParameterEditor.Text && parameter.Name.EndsWith("IconName", StringComparison.Ordinal);

    /// <summary>Whether a text parameter holds a color, by its value or its name, so it gets the color picker.</summary>
    private static bool IsColor(WorkbenchComponentParameter parameter)
    {
        return parameter.Editor is WorkbenchParameterEditor.Text && (ColorField.IsColor(parameter.Value) || parameter.Name.EndsWith("Color", StringComparison.Ordinal));
    }

    /// <summary>Whether a variable holds a color: its value is one, or it has none yet and its name says color or background.</summary>
    private static bool IsColorVariable(WorkbenchCssVariable variable)
    {
        return ColorField.IsColor(variable.Value)
            || (string.IsNullOrEmpty(variable.Value) && (variable.Name.Contains("clr", StringComparison.Ordinal) || variable.Name.Contains("color", StringComparison.OrdinalIgnoreCase) || variable.Name.Contains("background", StringComparison.OrdinalIgnoreCase)));
    }
}
