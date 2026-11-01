namespace Boilerplate.Workbench.Components.Shell;

/// <summary>
/// A value as text with the values its kind usually takes, like margin's, one click away under the arrow in the field;
/// any other value can still be typed.
/// </summary>
public partial class SuggestField
{
    private bool isOpen;

    [Parameter] public string? Value { get; set; }

    [Parameter] public EventCallback<string?> OnChange { get; set; }

    [Parameter] public string[] Suggestions { get; set; } = [];

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public string? AriaLabel { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    private void Toggle() => isOpen = isOpen is false;

    private Task Pick(string suggestion)
    {
        isOpen = false;

        return Change(suggestion);
    }

    private Task Change(string? value) => OnChange.InvokeAsync(string.IsNullOrWhiteSpace(value) ? null : value.Trim());
}
