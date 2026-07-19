namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupOptionDemo
{
    private int clickCounter;
    private string? clickedOption;

    private string? toggleKey = "play";
    private BitButtonGroupOption? onChangeToggleOption;

    private bool isSaving;
    private readonly string[] defaultKeys = ["bold"];
    private readonly string[] indicatorDefaultKeys = ["name", "size"];
    private IEnumerable<string>? formatKeys = ["bold"];

    private async Task HandleSaveClick()
    {
        isSaving = true;
        StateHasChanged();

        await Task.Delay(2000);

        isSaving = false;
        StateHasChanged();
    }
}
