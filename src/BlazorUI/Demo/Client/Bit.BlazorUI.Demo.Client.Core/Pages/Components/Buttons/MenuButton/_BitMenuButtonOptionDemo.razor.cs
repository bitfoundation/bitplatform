namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private string? exampleChangedOption;
    private string? exampleClickedOption;

    private BitMenuButtonOption twoWaySelectedOption = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private bool optionIsToggled;
    private bool optionToggledValue;

    private BitMenuButtonOption optionA = default!;
    private BitMenuButtonOption optionB = default!;
    private BitMenuButtonOption optionC = default!;

    private bool optionIsLoading;

    private string? submenuClickedOption;

    private bool optionShowName = true;
    private bool optionShowStatus = true;
    private bool optionShowOwner;

    private bool optionWrapLines = true;

    private bool optionSortByName = true;
    private bool optionSortByDate;
    private bool optionSortBySize;

    private string SortedBy => optionSortByName ? "Name" : optionSortByDate ? "Date modified" : "Size";

    private string VisibleColumns => string.Join(", ", new[]
    {
        optionShowName ? "Name" : null,
        optionShowStatus ? "Status" : null,
        optionShowOwner ? "Owner" : null
    }.Where(c => c is not null));

    private void ResetColumns()
    {
        optionShowName = true;
        optionShowStatus = true;
        optionShowOwner = false;
    }

    private bool _optionsCaptured;

    // The options the Binding example binds against are captured with @ref, which Blazor assigns
    // only AFTER the render that created them - so that render hands the choice group beside the
    // menu button three null Values and it can match none of them. One more render once they are
    // captured is what lets it show the selection the menu button starts with, and follow it.
    // The trigger is the capture rather than the first render: this page mounts each example's
    // preview only once it is scrolled within reach, so the Binding example is built long after.
    protected override void OnAfterRender(bool firstRender)
    {
        if (_optionsCaptured || optionA is null) return;

        _optionsCaptured = true;

        StateHasChanged();
    }

    private async Task HandleOnSaveClick() => await Task.Delay(2000);

    private async Task HandleOnRefreshClick() => await Task.Delay(2000);
}
