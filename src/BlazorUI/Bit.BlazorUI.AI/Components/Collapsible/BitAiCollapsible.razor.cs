namespace Bit.BlazorUI;

/// <summary>
/// BitAiCollapsible is a disclosure panel with a real button header, so it is keyboard reachable and announced
/// correctly. It holds the reasoning of a model (BitAiReasoningPartView), and is drawn by a
/// <see cref="BitAccordion"/>.
/// </summary>
public partial class BitAiCollapsible : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the collapsible component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple collapsible
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiCollapsibleParams.ParamName)]
    public BitAiCollapsibleParams? CascadingParameters { get; set; }



    /// <summary>
    /// The body of the panel, rendered only while the panel is open.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the collapsible.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiCollapsibleClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom content of the header, which replaces the <see cref="Title"/> text beside the chevron.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// Whether the panel is open. The panel also opens and closes itself when the user toggles it, and reports that
    /// through <see cref="OpenChanged"/>.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>
    /// The callback raised with the new state when the user opens or closes the panel.
    /// </summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the collapsible.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiCollapsibleClassStyles? Styles { get; set; }

    /// <summary>
    /// The content shown at the end of the header, beside the toggle, such as a status chip.
    /// </summary>
    [Parameter] public RenderFragment? SuffixTemplate { get; set; }

    /// <summary>
    /// The text of the header, used when no <see cref="HeaderTemplate"/> is given.
    /// </summary>
    [Parameter] public string? Title { get; set; }



    protected override string RootElementClass => "bit-aiclp";

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



    // The panel moves itself whether or not the page binds Open, as a disclosure panel the user toggles always does;
    // a page that binds it hears about the move and keeps it.
    private async Task HandleOnIsExpandedChanged(bool isExpanded)
    {
        Open = isExpanded;

        await OpenChanged.InvokeAsync(isExpanded);
    }
}
