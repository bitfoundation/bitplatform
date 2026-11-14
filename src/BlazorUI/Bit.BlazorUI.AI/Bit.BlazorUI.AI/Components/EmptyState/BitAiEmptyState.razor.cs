namespace Bit.BlazorUI;

/// <summary>
/// BitAiEmptyState is what an empty conversation shows: a glyph, a heading, a line under it, and example prompts the
/// user can start from with one tap.
/// </summary>
/// <remarks>
/// It is a <see cref="BitEmptyState"/> with the words of a conversation (<see cref="BitAiStrings.EmptyTitle"/> and
/// <see cref="BitAiStrings.EmptyDescription"/>), a sparkle for its icon and the suggestions for its actions, so it has the
/// parts, the names and the behavior of the core one. The suggestions matter more than the heading: an empty box is the
/// hardest part of a chat product, and three concrete examples solve it.
/// </remarks>
public partial class BitAiEmptyState : BitAiComponentBase
{
    private BitEmptyStateClassStyles? _emptyStateClasses;
    private BitEmptyStateClassStyles? _emptyStateStyles;



    /// <summary>
    /// Gets or sets the cascading parameters for the empty state component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple empty state components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiEmptyStateParams.ParamName)]
    public BitAiEmptyStateParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the empty state.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitAiEmptyStateClassStyles? Classes { get; set; }

    /// <summary>
    /// The supporting line under the heading, in place of the one of the string table (<see cref="BitAiStrings.EmptyDescription"/>).
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom content of the supporting line, which takes the place of <see cref="Description"/>.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Makes the heading a heading of this level (1 to 6) in the outline of the page. By default it is no heading.
    /// </summary>
    [Parameter] public int? HeadingLevel { get; set; }

    /// <summary>
    /// The icon of the empty state, provided as custom CSS classes of an external icon library.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon of the empty state, from the built-in Fluent UI icons.
    /// <br />
    /// The default value is <strong>AutoEnhanceOn</strong>, a sparkle.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The template rendered in place of the icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// The callback for when a suggestion is chosen.
    /// </summary>
    [Parameter] public EventCallback<string> OnSuggestion { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the empty state.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitAiEmptyStateClassStyles? Styles { get; set; }

    /// <summary>
    /// The example prompts offered as one-tap starters.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    /// <summary>
    /// The heading, in place of the one of the string table (<see cref="BitAiStrings.EmptyTitle"/>).
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom content of the heading, which takes the place of <see cref="Title"/>.
    /// </summary>
    [Parameter] public RenderFragment? TitleTemplate { get; set; }



    protected override string RootElementClass => "bit-aiems";

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

        // The parts of the core empty state carry the classes and the styles given to the same parts of this one; the
        // suggestions are its actions, so they are drawn in its content.
        _emptyStateClasses = new()
        {
            Icon = _IsDefaultIcon ? $"bit-icon-ai bit-icon-ai--AutoEnhanceOn {Classes?.Icon}" : Classes?.Icon,
            Title = Classes?.Title,
            Description = Classes?.Description,
            Content = Classes?.Suggestions,
        };

        _emptyStateStyles = new()
        {
            Icon = Styles?.Icon,
            Title = Styles?.Title,
            Description = Styles?.Description,
            Content = Styles?.Suggestions,
        };

        base.OnParametersSet();
    }



    // The default sparkle is not one of the 25 glyphs the core font carries, so it is drawn from this package's own subset
    // (bit-icon-ai). An icon the app names is its own to load; an empty Icon names none, so the sparkle stands in for it too.
    private bool _IsDefaultIcon => (Icon is null || Icon.IsEmpty) && IconName is null;

    private BitIconInfo? _Icon => _IsDefaultIcon ? null : Icon;

    private string? _IconName => _IsDefaultIcon ? "AutoEnhanceOn" : IconName;

    private async Task HandleOnSuggestion(string suggestion)
    {
        if (Disabled) return;

        await OnSuggestion.InvokeAsync(suggestion);
    }
}
