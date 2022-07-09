namespace AdminPanelTemplate.App.Components;

public partial class ConfirmMessageBox : IDisposable
{
    public Action<bool> CallBackFunction { get; set; }

    private static event Action<string, string,string, Action<bool>> OnShow = default!;

    public static void Show(string message, string context, string title, Action<bool> callBack)
    {
        OnShow?.Invoke(message,context, title, callBack);
    }

    protected override void OnInitialized()
    {
        ConfirmMessageBox.OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private void ShowMessageBox(string message, string context, string title, Action<bool> callBack)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;

            Title = title;
            Message = message;
            Context = context;

            CallBackFunction = callBack;

            StateHasChanged();
        });
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Message { get; set; } = string.Empty;

    private string Context { get; set; } = string.Empty;



    public void Confirmation(bool value)
    {
        IsOpen = false;
        CallBackFunction?.Invoke(value);
    }

    public void Dispose()
    {
        OnShow -= ShowMessageBox;
    }
}
