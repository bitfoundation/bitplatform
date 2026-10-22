using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiToolResultView shows a tool result that arrived without a matching call, which happens with providers that
/// emit tool output as its own message.
/// </summary>
/// <remarks>
/// A result that does belong to a call is shown by the <see cref="BitAiToolCallView"/> of that call instead. A failed
/// result is shown as a compact <see cref="BitAiErrorView"/>, a textual one as it was written, and a structured one
/// as a <see cref="BitAiJsonView"/>.
/// </remarks>
public partial class BitAiToolResultView : BitAiComponentBase
{
    // Repair is not one of the glyphs of the core font, so it is drawn from the subset font of this package.
    private const string _ToolIcon = "bit-icon-ai bit-icon-ai--Repair";



    /// <summary>
    /// Gets or sets the cascading parameters for the tool result view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple tool result view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiToolResultViewParams.ParamName)]
    public BitAiToolResultViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the tool result view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiToolResultViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The result to render.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiToolResultPart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the tool result view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiToolResultViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aitrv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiToolResultViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }
}
