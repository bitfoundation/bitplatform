namespace TodoTemplate.App.Shared;

public partial class MessageBox : IDisposable
{
    private static event Action<string, string, BitIconName, string, BitButtonType, BitButtonStyle> OnShow = default!;

    public static void Show(string message, string title = "", BitIconName closeIconName = BitIconName.ChromeClose,
        string buttonClass = "", BitButtonType buttonType = BitButtonType.Button, BitButtonStyle buttonStyle = BitButtonStyle.Standard)
    {
        OnShow?.Invoke(message, title, closeIconName, buttonClass, buttonType, buttonStyle);
    }

    protected override void OnInitialized()
    {
        MessageBox.OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private void ShowMessageBox(string message, string title, BitIconName closeIconName = BitIconName.ChromeClose,
        string buttonClass = "", BitButtonType buttonType = BitButtonType.Button, BitButtonStyle buttonStyle = BitButtonStyle.Standard)
    {
        InvokeAsync(() =>
        {
            IsOpen = true;

            Title = title;
            Body = message;
            CloseIconName = closeIconName;
            ButtonClass = buttonClass;
            ButtonType = buttonType;
            ButtonStyle = buttonStyle;

            StateHasChanged();
        });
    }

    // ========================================================================

    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Body { get; set; } = string.Empty;

    private BitIconName CloseIconName { get; set; } 
    private string ButtonClass { get; set; } = string.Empty;
    private BitButtonType ButtonType { get; set; } 
    private BitButtonStyle ButtonStyle { get; set; }

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
