using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiCitationMarker is the small numbered chip that appears inline where a claim is attributed to a source.
/// </summary>
/// <remarks>
/// It is a button rather than a link, because activating it reveals the source in place (<see cref="OnActivate"/>)
/// instead of navigating away. Its accessible name comes from <see cref="BitAiStrings.CitationLabel"/>, and its native
/// tooltip previews the title of the source and the quote the claim rests on.
/// </remarks>
public partial class BitAiCitationMarker : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the citation marker component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple citation marker
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiCitationMarkerParams.ParamName)]
    public BitAiCitationMarkerParams? CascadingParameters { get; set; }



    /// <summary>
    /// The citation this marker points at.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiCitation Citation { get; set; } = default!;

    /// <summary>
    /// Custom CSS classes for different parts of the citation marker.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiCitationMarkerClassStyles? Classes { get; set; }

    /// <summary>
    /// The callback for when the user activates the marker.
    /// </summary>
    [Parameter] public EventCallback<BitAiCitation> OnActivate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the citation marker.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiCitationMarkerClassStyles? Styles { get; set; }



    protected override string RootElementClass => "bit-aicmk";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiCitationMarkerParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // The title of the source, and the passage the claim rests on below it - cut short, since a native tooltip
    // has no scrolling and a whole paragraph in one covers the text being read.
    private string _Tooltip => Citation.Quote is { Length: > 0 } quote
        ? $"{Citation.Source.Title}\n\n{Truncate(quote)}"
        : Citation.Source.Title;

    private async Task HandleOnClick()
    {
        // A screen reader or a script still delivers a click to a disabled button now and then.
        if (Disabled) return;

        await OnActivate.InvokeAsync(Citation);
    }

    private static string Truncate(string value) =>
        value.Length <= 220 ? value : string.Concat(value.AsSpan(0, 220).TrimEnd(), "...");
}
