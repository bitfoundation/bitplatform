using System;

namespace Bit.BlazorUI.Playground.Web.Shared;

public partial class MessageBox : IDisposable
{
    private static event Action<string, string> OnShow = default!;

    public static void Show(string message, string title = "")
    {
        OnShow?.Invoke(message, title);
    }

    protected override void OnInitialized()
    {
        MessageBox.OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private void ShowMessageBox(string message, string title)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;

            Title = title;
            Body = message;

            StateHasChanged();
        });
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Body { get; set; } = string.Empty;

    private void OnCloseClick()
    {
        IsOpen = false;
    }

    private void OnOkClick()
    {
        IsOpen = false;
    }

    public void Dispose()
    {
        MessageBox.OnShow -= ShowMessageBox;
    }
}
