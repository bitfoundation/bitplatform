﻿namespace Bit.Websites.Platform.Client.Shared;

public partial class MessageBox : IDisposable
{
    private bool _isOpen;
    private string? _title;
    private string? _body;

    private TaskCompletionSource<object?>? _tsc;

    private async Task OnCloseClick()
    {
        _isOpen = false;
        await JSRuntime.ToggleBodyOverflow(false);
        _tsc?.SetResult(null);
        _tsc = null;
    }

    private async Task OnOkClick()
    {
        _isOpen = false;
        await JSRuntime.ToggleBodyOverflow(false);
        _tsc?.SetResult(null);
        _tsc = null;
    }

    Action? _dispose;
    bool _disposed = false;

    protected override Task OnInitAsync()
    {
        _dispose = PubSubService.Subscribe(PubSubMessages.SHOW_MESSAGE, async args =>
        {
            (var message, string title, TaskCompletionSource<object?> tsc) = ((string message, string title, TaskCompletionSource<object?> tsc))args!;
            await (_tsc?.Task ?? Task.CompletedTask);
            _tsc = tsc;
            await ShowMessageBox(message, title);
        });

        return base.OnInitAsync();
    }

    private async Task ShowMessageBox(string message, string title = "")
    {
        await InvokeAsync(() =>
        {
            _ = JSRuntime.ToggleBodyOverflow(true);

            _isOpen = true;
            _title = title;
            _body = message;

            StateHasChanged();
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        _tsc?.TrySetResult(null);
        _tsc = null;
        _dispose?.Invoke();

        _disposed = true;
    }
}
