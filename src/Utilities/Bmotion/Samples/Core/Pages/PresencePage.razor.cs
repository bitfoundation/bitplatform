using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bmotion.Samples.Core.Pages;

public partial class PresencePage
{
    private static readonly TimeSpan ToastLifetime = TimeSpan.FromSeconds(5);
    private const int MaxToasts = 4;

    private static readonly BmPreset FadePreset = BmPresets.Fade();
    private static readonly BmPreset ModalPreset = BmPresets.Modal();
    private static readonly BmPreset ToastPreset = BmPresets.Toast();

    private static readonly string[] Messages = ["Draft saved", "Link copied", "Settings updated", "Upload finished", "Invite sent"];

    private static readonly string[] Steps =
    [
        "Pick a plan",
        "Tell us who you are",
        "Choose a payment method",
        "Review and confirm",
    ];

    private readonly CancellationTokenSource _disposed = new();
    private readonly List<Toast> _toasts = [];
    private int _nextToast = 1;

    private bool _dialogOpen;
    private int _dialogExits;
    private ElementReference _opener;
    private ElementReference _close;
    // What takes focus after the next render: the Close button once the dialog is there, the opener once it is closing.
    private Func<ElementReference>? _focusNext;

    private int _step;
    private int _direction = 1;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusNext is null) return;

        var element = _focusNext();
        _focusNext = null;
        await element.FocusAsync();
    }

    private void OpenDialog()
    {
        _dialogOpen = true;
        _focusNext = () => _close;
    }

    private void CloseDialog()
    {
        if (_dialogOpen is false) return;

        _dialogOpen = false;
        _focusNext = () => _opener;
    }

    private void OnDialogKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") CloseDialog();
    }

    private void OnDialogGone() => _dialogExits++;

    private void AddToast()
    {
        var toast = new Toast(_nextToast, $"{Messages[(_nextToast - 1) % Messages.Length]} · #{_nextToast}");
        _nextToast++;

        _toasts.Add(toast);
        if (_toasts.Count > MaxToasts) _toasts.RemoveAt(0);

        _ = DismissLaterAsync(toast);
    }

    private async Task DismissLaterAsync(Toast toast)
    {
        try
        {
            await Task.Delay(ToastLifetime, _disposed.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(() =>
        {
            if (_toasts.Remove(toast)) StateHasChanged();
        });
    }

    private void Dismiss(Toast toast) => _toasts.Remove(toast);

    private void Go(int direction)
    {
        _direction = direction;
        _step += direction;
    }

    public void Dispose()
    {
        _disposed.Cancel();
        _disposed.Dispose();
    }

    private sealed record Toast(int Id, string Text);
}
