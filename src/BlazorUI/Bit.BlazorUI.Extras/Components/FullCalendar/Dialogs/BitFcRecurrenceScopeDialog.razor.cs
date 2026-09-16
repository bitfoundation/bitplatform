using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// Asks whether an edit or a delete opened on a recurring occurrence applies to that occurrence alone
/// or to the whole series. "This event" is preselected: it is the change that cannot reach further
/// than the one date the user opened.
/// </summary>
public partial class BitFcRecurrenceScopeDialog : IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] public BitFullCalendarTexts Texts { get; set; } = default!;

    /// <summary>The question being asked, such as "Delete recurring event".</summary>
    [Parameter] public string Title { get; set; } = string.Empty;

    /// <summary>Raised with <c>true</c> when the whole series was chosen, <c>false</c> for this occurrence only.</summary>
    [Parameter] public EventCallback<bool> OnConfirm { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    private readonly string _titleId = $"bfc-scope-title-{Guid.NewGuid():N}";
    private readonly string _groupName = $"bfc-scope-{Guid.NewGuid():N}";

    private ElementReference _dialogRef;
    private bool _allOccurrences;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A modal of its own on top of the details dialog, so it takes focus and traps Tab the same way.
        if (firstRender)
            await BitFcDialogInterop.SetupAsync(JS, _dialogRef);
    }

    private async Task OnDialogKeyDown(KeyboardEventArgs e)
    {
        if (e.Key is "Escape" or "Esc")
            await OnCancel.InvokeAsync();
    }

    private Task Confirm() => OnConfirm.InvokeAsync(_allOccurrences);

    public async ValueTask DisposeAsync()
    {
        await BitFcDialogInterop.TeardownAsync(JS, _dialogRef);
    }
}
