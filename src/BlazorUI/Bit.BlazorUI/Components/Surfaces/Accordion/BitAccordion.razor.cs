namespace Bit.BlazorUI;

/// <summary>
/// The Accordion component allows the user to show and hide sections of related content on a page.
/// </summary>
public partial class BitAccordion : BitComponentBase
{
    private bool _hasBeenExpanded;



    /// <summary>
    /// The content rendered beside the header, outside of the toggle button and of the heading it sits in,
    /// so that it can hold its own interactive elements (a menu, a delete button, a switch).
    /// </summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>
    /// The color kind of the background of the accordion.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of the accordion.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Alias for the ChildContent parameter.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the accordion.
    /// </summary>
    [Parameter] public BitAccordionClassStyles? Classes { get; set; }

    /// <summary>
    /// The content of the accordion.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Default value for the IsExpanded parameter.
    /// </summary>
    [Parameter] public bool? DefaultIsExpanded { get; set; }

    /// <summary>
    /// A short description in the header of the accordion.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon to show in place of the expander icon while the accordion is expanded,
    /// using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandedExpanderIconName"/> when both are set.
    /// Setting either of them also turns the rotation of the expander icon off, since a swapped icon
    /// already reports the state on its own.
    /// </summary>
    [Parameter] public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon, from the built-in Fluent UI icons, to show in place of the
    /// expander icon while the accordion is expanded.
    /// Setting it also turns the rotation of the expander icon off, since a swapped icon already reports
    /// the state on its own.
    /// </summary>
    [Parameter] public string? ExpandedExpanderIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display as expander using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpanderIconName"/> when both are set.
    /// Defaults to the ChevronRight icon if neither property is set.
    /// </summary>
    [Parameter] public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display as expander from the built-in Fluent UI icons.
    /// Defaults to <c>ChevronRight</c> if not set.
    /// </summary>
    [Parameter] public string? ExpanderIconName { get; set; }

    /// <summary>
    /// Gets or sets the side of the header the expander icon sits on.
    /// <br />
    /// The default value is <see cref="BitIconPosition.End"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitIconPosition? ExpanderIconPosition { get; set; }

    /// <summary>
    /// Gets or sets the accessible label of the toggle button in the header, for a header whose own content
    /// does not name it - an icon-only <see cref="HeaderTemplate"/>, most of all.
    /// </summary>
    [Parameter] public string? HeaderAriaLabel { get; set; }

    /// <summary>
    /// Used to customize the header of the accordion. It replaces the whole default header, the expander
    /// icon included, and receives the current expanded state.
    /// </summary>
    [Parameter] public RenderFragment<bool>? HeaderTemplate { get; set; }

    /// <summary>
    /// Gets or sets the heading level (aria-level) reported for the header of the accordion, so that it
    /// takes its right place in the heading outline of the page.
    /// <br />
    /// The default value is <strong>3</strong>, and the value is clamped to the 1..6 range.
    /// </summary>
    [Parameter] public int? HeadingLevel { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of the accordion.
    /// </summary>
    [Parameter] public bool HideExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon to display at the start of the header using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display at the start of the header from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Determines whether the accordion is expanded or collapsed.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Delays the first render of the content of the accordion until it is expanded for the first time.
    /// The content stays in the DOM afterwards, so the state it holds survives a collapse.
    /// </summary>
    [Parameter] public bool LazyContent { get; set; }

    /// <summary>
    /// Gets or sets the maximum height of the content of the accordion (any CSS length), beyond which the
    /// content scrolls inside the accordion instead of growing it. The scrolling region is focusable, so
    /// that it can be scrolled by the keyboard as well.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Removes the default border of the accordion and gives a background color to the body.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool NoBorder { get; set; }

    /// <summary>
    /// Keeps the expander icon still instead of turning it over when the accordion is expanded.
    /// </summary>
    [Parameter] public bool NoExpanderRotation { get; set; }

    /// <summary>
    /// Callback that is called when the header is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback that is called when the IsExpanded value has changed.
    /// </summary>
    [Parameter] public EventCallback<bool> OnChange { get; set; }

    /// <summary>
    /// Callback that is called when the accordion is collapsed.
    /// </summary>
    [Parameter] public EventCallback OnCollapse { get; set; }

    /// <summary>
    /// Callback that is called when the accordion is expanded.
    /// </summary>
    [Parameter] public EventCallback OnExpand { get; set; }

    /// <summary>
    /// Gets or sets the size of the accordion, which drives the padding of the header and of the content
    /// and the size of the title.
    /// <br />
    /// The default value is <see cref="BitSize.Medium"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the accordion.
    /// </summary>
    [Parameter] public BitAccordionClassStyles? Styles { get; set; }

    /// <summary>
    /// Title in the header of accordion.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom content to render in place of the <see cref="Title"/>, leaving the rest of the header -
    /// the icon, the description and the expander - as it is. Unlike <see cref="HeaderTemplate"/>, which
    /// replaces the whole header, this only takes the place of the title text.
    /// </summary>
    [Parameter] public RenderFragment? TitleTemplate { get; set; }

    /// <summary>
    /// Gets or sets the duration of the expand/collapse transition in milliseconds, overriding the duration
    /// the theme provides. A reduced-motion preference still collapses it, unless the ForceAnimation
    /// parameter opts out of that.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public int? TransitionDuration { get; set; }

    /// <summary>
    /// Removes the content of the accordion from the DOM while it is collapsed, so that nothing it holds
    /// keeps running behind a closed header. The collapse of an accordion that unmounts its content is not
    /// animated, since there is nothing left to animate.
    /// </summary>
    [Parameter] public bool UnmountOnCollapse { get; set; }



    /// <summary>
    /// Expands the accordion. Does nothing if it is already expanded, and reports the change through the
    /// IsExpanded binding, OnChange and OnExpand.
    /// </summary>
    /// <remarks>
    /// A call of its own is not turned away by <see cref="BitComponentBase.IsEnabled"/> - a disabled
    /// accordion answers no pointer and no key, but the app can still open it to show why it is disabled -
    /// and a one-way bound <see cref="IsExpanded"/> still owns the state, so nothing happens there either.
    /// </remarks>
    public Task Expand() => SetExpanded(true);

    /// <summary>
    /// Collapses the accordion. Does nothing if it is already collapsed, and reports the change through the
    /// IsExpanded binding, OnChange and OnCollapse.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="BitComponentBase.IsEnabled"/>; see <see cref="Expand"/>.
    /// </remarks>
    public Task Collapse() => SetExpanded(false);

    /// <summary>
    /// Expands the accordion if it is collapsed and collapses it if it is expanded, reporting the change
    /// through the IsExpanded binding, OnChange and OnExpand/OnCollapse.
    /// </summary>
    /// <remarks>
    /// Not turned away by <see cref="BitComponentBase.IsEnabled"/>; see <see cref="Expand"/>.
    /// </remarks>
    public Task Toggle() => SetExpanded(IsExpanded is false);



    private string _HeaderId => $"{_Id}-hdr";
    private string _ContentId => $"{_Id}-cnt";

    private int _HeadingLevel => Math.Clamp(HeadingLevel ?? 3, 1, 6);

    // A region is not an interactive element, so it earns a tab stop only where it has something the
    // keyboard could not otherwise reach: the scroll of a content that is taller than its MaxHeight.
    private bool _IsContentFocusable => IsExpanded && MaxHeight.HasValue();

    private bool _ShouldRenderContent => UnmountOnCollapse
                                            ? IsExpanded
                                            : (LazyContent is false || _hasBeenExpanded || IsExpanded);



    protected override string RootElementClass => "bit-acd";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => IsExpanded ? "bit-acd-exp" : string.Empty);

        ClassBuilder.Register(() => IsExpanded ? Classes?.Expanded : string.Empty);

        ClassBuilder.Register(() => NoBorder ? "bit-acd-nbd" : string.Empty);

        ClassBuilder.Register(() => MaxHeight.HasValue() ? "bit-acd-mxh" : string.Empty);

        ClassBuilder.Register(() => ExpanderIconPosition is BitIconPosition.Start ? "bit-acd-sei" : string.Empty);

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-acd-sm",
            BitSize.Medium => "bit-acd-md",
            BitSize.Large => "bit-acd-lg",
            _ => "bit-acd-md"
        });

        ClassBuilder.Register(() => Background switch
        {
            BitColorKind.Primary => "bit-acd-pbg",
            BitColorKind.Secondary => "bit-acd-sbg",
            BitColorKind.Tertiary => "bit-acd-tbg",
            BitColorKind.Transparent => "bit-acd-rbg",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Border switch
        {
            BitColorKind.Primary => "bit-acd-pbr",
            BitColorKind.Secondary => "bit-acd-sbr",
            BitColorKind.Tertiary => "bit-acd-tbr",
            BitColorKind.Transparent => "bit-acd-rbr",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsExpanded ? Styles?.Expanded : string.Empty);

        // The duration is handed to the stylesheet as the -full token rather than written into the transitions
        // directly, so the reduced-motion collapse in the stylesheet can still shorten it (an inline duration
        // would be out of reach of any media query).
        StyleBuilder.Register(() => TransitionDuration.HasValue
                                    ? FormattableString.Invariant($"--bit-acd-dur-full:{Math.Max(0, TransitionDuration.Value)}ms")
                                    : string.Empty);

        StyleBuilder.Register(() => MaxHeight.HasValue() ? $"--bit-acd-max-h:{MaxHeight}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (IsExpandedHasBeenSet is false && DefaultIsExpanded.HasValue)
        {
            await AssignIsExpanded(DefaultIsExpanded.Value);
        }

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        if (IsExpanded) _hasBeenExpanded = true;

        base.OnParametersSet();
    }



    private async Task HandleOnClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        await AssignExpanded(IsExpanded is false);
    }

    // The public methods are not called from an event handler, so nothing re-renders the component on their
    // behalf the way Blazor does after a click.
    private async Task SetExpanded(bool value)
    {
        if (await AssignExpanded(value) is false) return;

        StateHasChanged();
    }

    private async Task<bool> AssignExpanded(bool value)
    {
        // AssignIsExpanded reports back only whether the assignment was allowed at all (a one-way bound
        // IsExpanded refuses it), not whether it changed anything, so the state the accordion is already in
        // is caught here - otherwise Expand on an open accordion would report an expansion of its own.
        if (IsExpanded == value) return false;

        if (await AssignIsExpanded(value) is false) return false;

        if (IsExpanded) _hasBeenExpanded = true;

        await OnChange.InvokeAsync(IsExpanded);

        if (IsExpanded)
        {
            await OnExpand.InvokeAsync();
        }
        else
        {
            await OnCollapse.InvokeAsync();
        }

        return true;
    }
}
