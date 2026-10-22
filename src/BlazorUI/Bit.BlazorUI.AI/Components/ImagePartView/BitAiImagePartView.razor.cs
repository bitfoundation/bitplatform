using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiImagePartView shows an image part of a message - one a model generated or a tool returned - with its caption.
/// </summary>
/// <remarks>
/// The image declares its intrinsic size where the part knows it, so the message list does not jump as images decode
/// during streaming, and it is loaded lazily, so a long transcript only fetches the images that scroll into view. The
/// alternative text of the part (<see cref="BitAiImagePart.Alt"/>) is its accessible name: a model will not always
/// provide one, so ask for it in the prompt or supply a fallback.
/// </remarks>
public partial class BitAiImagePartView : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the image part view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the classes and styles of its parts) to be
    /// applied to multiple image part view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiImagePartViewParams.ParamName)]
    public BitAiImagePartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the image part view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiImagePartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The image to render.
    /// </summary>
    [Parameter, EditorRequired] public BitAiImagePart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the image part view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiImagePartViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiipv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiImagePartViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }
}
