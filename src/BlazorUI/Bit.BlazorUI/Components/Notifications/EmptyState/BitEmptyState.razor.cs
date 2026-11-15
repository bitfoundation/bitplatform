namespace Bit.BlazorUI;

/// <summary>
/// BitEmptyState is what a page, a panel or a list shows when it has nothing to show: an icon, a title that says so, a
/// line on why or what to do about it, and the actions to get going.
/// </summary>
/// <remarks>
/// It is plain content, not a live region: a component that empties while the user looks at it (a list that a search
/// filters down to nothing) announces that itself. With no <see cref="Title"/> it says
/// <see cref="BitStrings.NoItemsFound"/> of <see cref="BitStrings.Current"/>.
/// </remarks>
public partial class BitEmptyState : BitComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the empty state component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple empty state components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitEmptyStateParams.ParamName)]
    public BitEmptyStateParams? CascadingParameters { get; set; }



    /// <summary>
    /// The actions under the text of the empty state, most of the time the button that adds the first item.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the empty state.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitEmptyStateClassStyles? Classes { get; set; }

    /// <summary>
    /// The line under the title, on why there is nothing to show or what to do about it.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// The custom content of the line under the title, which takes the place of <see cref="Description"/>.
    /// </summary>
    [Parameter] public RenderFragment? DescriptionTemplate { get; set; }

    /// <summary>
    /// Makes the title a heading of this level (1 to 6) in the outline of the page, for an empty state that stands
    /// for the whole of a page or a section. By default the title is no heading, as it is not in a list.
    /// </summary>
    [Parameter] public int? HeadingLevel { get; set; }

    /// <summary>
    /// The icon above the title, provided as custom CSS classes of an external icon library.
    /// </summary>
    /// <remarks>
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon above the title, from the built-in Fluent UI icons. By default there is no icon.
    /// </summary>
    /// <remarks>
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The custom content above the title (an illustration, most of the time), which takes the place of the icon.
    /// </summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the empty state.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitEmptyStateClassStyles? Styles { get; set; }

    /// <summary>
    /// The title of the empty state. Default: <see cref="BitStrings.NoItemsFound"/> of <see cref="BitStrings.Current"/>.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom content of the title, which takes the place of <see cref="Title"/>.
    /// </summary>
    [Parameter] public RenderFragment? TitleTemplate { get; set; }



    private int? _HeadingLevel => HeadingLevel is null ? null : Math.Clamp(HeadingLevel.Value, 1, 6);



    protected override string RootElementClass => "bit-ems";

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
