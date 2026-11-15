namespace Bit.BlazorUI;

/// <summary>
/// BitAiSourceList is the deduplicated list of the sources a message cited, each a <see cref="BitAiSourceCard"/>
/// numbered to match the inline citation markers.
/// </summary>
/// <remarks>
/// A source cited several times is listed once, under the lowest number any of its citations carries, and the list is
/// ordered by those numbers - a source cited without a number goes last. Nothing is rendered while there is nothing to
/// list.
/// </remarks>
public partial class BitAiSourceList : BitAiComponentBase
{
    private List<(int Number, BitAiCitationSource Source)> _sources = [];



    /// <summary>
    /// Gets or sets the cascading parameters for the source list component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (the heading and the look) to be applied to
    /// multiple source list components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiSourceListParams.ParamName)]
    public BitAiSourceListParams? CascadingParameters { get; set; }



    /// <summary>
    /// The citations to list the sources of.
    /// </summary>
    [Parameter] public IReadOnlyList<BitAiCitation>? Citations { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the source list.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiSourceListClassStyles? Classes { get; set; }

    /// <summary>
    /// The callback for when the user activates a source card. When set, the cards become buttons instead of links.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitationSource> OnSourceClick { get; set; }

    /// <summary>
    /// Shows the heading above the list.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowHeading { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the source list.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiSourceListClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aisrl";

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

        _sources = (Citations ?? [])
            .GroupBy(c => c.Source.SourceId, StringComparer.Ordinal)
            .Select(g => (Number: g.Min(c => c.Number ?? int.MaxValue), Source: g.First().Source))
            .OrderBy(x => x.Number)
            .ToList();

        base.OnParametersSet();
    }



    // The heading names the list of cards under it (aria-labelledby), so it is named after the list's own id.
    private string HeaderId => $"{_Id}-hdr";
}
