namespace Bit.BlazorUI;

/// <summary>
/// BitAiArtifactPartView shows where a message introduced an artifact.
/// </summary>
/// <remarks>
/// The artifact belongs to the conversation rather than to the message, so the part resolves it by identifier and shows
/// the shared <see cref="BitAiArtifactView"/>, opened at the version this turn produced. A later revision therefore
/// does not rewrite what an earlier turn appears to have said. An artifact the conversation does not hold is shown as a
/// placeholder tag named after it.
/// </remarks>
public partial class BitAiArtifactPartView : BitAiComponentBase
{
    private BitAiArtifact? _artifact;
    private BitAiIconClassesCache<BitTagClassStyles> _placeholderClasses;



    /// <summary>
    /// Gets or sets the cascading parameters for the artifact part view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple artifact part view
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiArtifactPartViewParams.ParamName)]
    public BitAiArtifactPartViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the artifact part view.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiArtifactPartViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The conversation that holds the artifact.
    /// </summary>
    [Parameter] public BitAiConversation? Conversation { get; set; }

    /// <summary>
    /// The part that references the artifact.
    /// </summary>
    [Parameter, EditorRequired] public BitAiArtifactPart Part { get; set; } = default!;

    /// <summary>
    /// Custom CSS styles for different parts of the artifact part view.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiArtifactPartViewClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aiapv";

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

        _artifact = Conversation?.FindArtifact(Part.ArtifactId);

        base.OnParametersSet();
    }



    // The classes handed to the placeholder tag: the app's own for it, with the glyph of this package's icon font added
    // to the icon, since MapLayers is not in the core's (see BitAiIconClasses). Kept while the app's own do not change,
    // so a re-render hands the tag the same instance rather than resetting its builder.
    private BitTagClassStyles _PlaceholderClasses =>
        _placeholderClasses.Get("bit-icon-ai bit-icon-ai--MapLayers", Classes?.Placeholder, BitAiIconClasses.With);
}
