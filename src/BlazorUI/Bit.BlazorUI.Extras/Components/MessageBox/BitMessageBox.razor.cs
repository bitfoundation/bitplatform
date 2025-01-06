using System.Reflection.Metadata;

namespace Bit.BlazorUI;

public partial class BitMessageBox
{
    /// <summary>
    /// The body of the message box.
    /// </summary>
    [Parameter] public string? Body { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    [Parameter] public string? OkText { get; set; }

    /// <summary>
    /// The event callback for closing the message box.
    /// </summary>
    [Parameter] public Action? OnClose { get; set; }

    /// <summary>
    /// The title of the message box.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    private void CloseModal()
    {
        OnClose?.Invoke();
    }

    private void OnOkClick()
    {
        OnClose?.Invoke();
    }
}
