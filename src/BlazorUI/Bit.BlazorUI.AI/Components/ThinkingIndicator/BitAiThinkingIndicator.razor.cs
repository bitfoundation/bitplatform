namespace Bit.BlazorUI;

/// <summary>
/// BitAiThinkingIndicator is shown while the model is working but has produced nothing yet: three bouncing dots and
/// a label, announced politely so a screen reader user learns that a response is coming without having the
/// announcement interrupt them.
/// </summary>
public partial class BitAiThinkingIndicator : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the thinking indicator component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple thinking indicator
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiThinkingIndicatorParams.ParamName)]
    public BitAiThinkingIndicatorParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the thinking indicator.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiThinkingIndicatorClassStyles? Classes { get; set; }

    /// <summary>
    /// The text shown and announced beside the dots, for example a tool-specific status.
    /// <br />
    /// The default value is <strong>null</strong>, which shows the Thinking text of the string table in force
    /// (<see cref="BitAiStrings.Thinking"/>).
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the thinking indicator.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiThinkingIndicatorClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aithi";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }
}
