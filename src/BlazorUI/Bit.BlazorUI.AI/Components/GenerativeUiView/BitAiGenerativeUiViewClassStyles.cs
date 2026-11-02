namespace Bit.BlazorUI;

public class BitAiGenerativeUiViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiGenerativeUiView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the rendered component of the BitAiGenerativeUiView.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag a component name nothing is registered under is shown as by the BitAiGenerativeUiView.
    /// </summary>
    public BitTagClassStyles? Unregistered { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the binding diagnostics of the BitAiGenerativeUiView.
    /// </summary>
    public string? Diagnostics { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag of each property that failed to bind in the BitAiGenerativeUiView.
    /// </summary>
    public BitTagClassStyles? Error { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitTag of each property the component of the BitAiGenerativeUiView does not declare.
    /// </summary>
    public BitTagClassStyles? Ignored { get; set; }
}
