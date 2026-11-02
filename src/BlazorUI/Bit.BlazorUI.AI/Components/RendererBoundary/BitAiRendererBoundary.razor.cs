namespace Bit.BlazorUI;

/// <summary>
/// BitAiRendererBoundary contains a failure inside one custom renderer, so the rest of the conversation survives it.
/// </summary>
/// <remarks>
/// Custom renderers are handed data the model produced, which means they are handed shapes their author did not
/// anticipate. Without a boundary, one such shape throws during rendering and takes the whole page with it. With one,
/// the rest of the conversation survives and the reader sees which piece failed.
/// <br />
/// It is built on Blazor's own <see cref="ErrorBoundary"/>, so it catches what the content throws while it renders,
/// runs its lifecycle methods or handles an event - never a fire-and-forget task. The content is rendered with no
/// element of the boundary's own around it; the root element is the notice rendered in its place once it has failed,
/// so <see cref="BitComponentBase.Class"/>, <see cref="BitComponentBase.Style"/>, <see cref="BitComponentBase.Id"/>
/// and the splatted attributes land on that notice alone. Every render of the boundary by its parent tries the
/// content again, which is how a renderer that failed on a partial value recovers once the value is complete; a
/// content that fails again keeps the notice, whether it throws while it renders or in a lifecycle method.
/// </remarks>
public partial class BitAiRendererBoundary : BitAiComponentBase
{
    // Moves on each time content that failed is tried again, which replaces the error boundary that failed with a new
    // one (see BitAiRendererErrorBoundary for why not ErrorBoundary.Recover).
    private int _generation;
    private BitAiRendererErrorBoundary? _boundary;



    /// <summary>
    /// Gets or sets the cascading parameters for the renderer boundary component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple renderer boundary
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiRendererBoundaryParams.ParamName)]
    public BitAiRendererBoundaryParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content to protect.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the renderer boundary.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiRendererBoundaryClassStyles? Classes { get; set; }

    /// <summary>
    /// What the notice says once the content has failed.
    /// <br />
    /// The default value is the RenderFailed text of the <see cref="BitAiStrings"/> in force.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the renderer boundary.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiRendererBoundaryClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-airbd";

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

        // A new render of the boundary is new content to try: a renderer that threw on a partial value gets another
        // go once its parent hands it the next one, in a boundary of its own that has never failed.
        if (_boundary?.HasFailed is true)
        {
            _generation++;
        }

        base.OnParametersSet();
    }
}
