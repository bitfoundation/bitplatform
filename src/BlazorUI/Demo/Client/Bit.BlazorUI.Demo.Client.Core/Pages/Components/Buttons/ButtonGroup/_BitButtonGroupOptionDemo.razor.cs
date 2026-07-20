namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
    private int clickCounter;
    private string? clickedOption;

    private string? toggleKey = "play";
    private BitButtonGroupOption? onChangeToggleOption;

    private string? loadingKey;
    private readonly string[] defaultKeys = ["bold"];
    private readonly string[] indicatorDefaultKeys = ["name", "size"];
    private IEnumerable<string>? formatKeys = ["bold"];

    // The option's IsLoading is a component parameter, so it is driven from here through the key of
    // the option that is currently loading instead of being assigned on the option itself.
    private async Task HandleLoadingClick(string key)
    {
        loadingKey = key;
        StateHasChanged();

        await Task.Delay(2000);

        loadingKey = null;
        StateHasChanged();
    }
}
