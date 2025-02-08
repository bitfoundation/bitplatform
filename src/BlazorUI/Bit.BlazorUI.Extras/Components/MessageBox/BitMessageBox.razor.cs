using System.Reflection.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// BitMessageBox is a pre-implemented box for showing messages with title and body.
/// </summary>
public partial class BitMessageBox : BitComponentBase
{
    /// <summary>
    /// The body of the message box.
    /// </summary>
    [Parameter] public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the message box.
    /// </summary>
    [Parameter] public BitMessageBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The text of the Ok button.
    /// </summary>
    [Parameter] public string? OkText { get; set; }

    /// <summary>
    /// The event callback for closing the message box.
    /// </summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the message box.
    /// </summary>
    [Parameter] public BitMessageBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the message box.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-msb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    private async Task CloseModal()
    {
        await OnClose.InvokeAsync();
    }

    private async Task OnOkClick()
    {
        await OnClose.InvokeAsync();
    }
}
