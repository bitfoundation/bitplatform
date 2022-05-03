using Bit.Client.Web.BlazorUI.Components;

namespace TodoTemplate.App.Shared;

public partial class MessageBox : IDisposable
{
    private static event Action<string,BitIconName, BitButtonType, BitButtonStyle, BitIconName, BitIconPosition, string, string, string> OnShow = default!;

    public static void Show(string message, BitIconName closeIconName = BitIconName.ChromeClose
        , BitButtonType buttonType = BitButtonType.Button, BitButtonStyle buttonStyle = BitButtonStyle.Standard,
        BitIconName bodyIcon = BitIconName.Info, BitIconPosition bodyIconPosition = BitIconPosition.End,
        string buttonText = "OK", string title = "", string buttonClass = "")
    {
        OnShow?.Invoke(message, closeIconName, buttonType, buttonStyle, bodyIcon, bodyIconPosition, buttonText,title, buttonClass);
    }

    protected override void OnInitialized()
    {
        MessageBox.OnShow += ShowMessageBox;

        base.OnInitialized();
    }

    private void ShowMessageBox(string message, BitIconName closeIconName = BitIconName.ChromeClose
        , BitButtonType buttonType = BitButtonType.Button, BitButtonStyle buttonStyle = BitButtonStyle.Standard,
        BitIconName bodyIcon = BitIconName.Info, BitIconPosition bodyIconPosition = BitIconPosition.End,
        string buttonText = "OK",string title = "", string buttonClass = "")
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
            ButtonText = buttonText;
            BodyIcon = bodyIcon;
            BodyIconPosition = bodyIconPosition;

            StateHasChanged();
        });
    }

    // ========================================================================

    #region properties
    private bool IsOpen { get; set; }
    private string Title { get; set; } = string.Empty;
    private string Body { get; set; } = string.Empty;
    /// <summary>
    /// icon of message box header close button. If unset, default will be the bit "ChromeClose" icon
    /// </summary>
    private BitIconName CloseIconName { get; set; }
    /// <summary>
    /// css class of message box footer button. If unset, default will be an empty string
    /// </summary>
    private string ButtonClass { get; set; } = string.Empty;
    /// <summary>
    /// Type of message box footer button. If unset, default will be the "Button" type
    /// </summary>
    private BitButtonType ButtonType { get; set; }
    /// <summary>
    /// Style of message box footer button. If unset, default will be the "Standard" style
    /// </summary>
    private BitButtonStyle ButtonStyle { get; set; }
    /// <summary>
    /// text of message box footer button. If unset, default will be the "OK" text
    /// </summary>
    private string ButtonText { get; set; } = string.Empty;
    /// <summary>
    /// icon of message box body. If unset, default will be the bit "info" icon
    /// </summary>
    private BitIconName BodyIcon { get; set; }
    /// <summary>
    /// icon position of message box body. If unset, default will be the "End" of body
    /// </summary>
    private BitIconPosition BodyIconPosition { get; set; }
    #endregion

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
