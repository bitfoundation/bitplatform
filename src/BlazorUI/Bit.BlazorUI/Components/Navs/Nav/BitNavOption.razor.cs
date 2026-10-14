namespace Bit.BlazorUI;

public partial class BitNavOption : ComponentBase, IDisposable
{
    private bool _disposed;
    private bool _isExpanded;
    private bool? _isExpandedParameter;



    internal IList<BitNavOption> ChildItems { get; set; } = [];



    [CascadingParameter] protected BitNavOption? Parent { get; set; }

    [CascadingParameter] protected BitNav<BitNavOption> Nav { get; set; } = default!;



    /// <summary>
    /// Aria-current token for active nav option. Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// The accessible name of the nav option, in place of its text. It also names a group header, whose
    /// CollapseAriaLabel/ExpandAriaLabel only apply when it is not set.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// The badge text to render after the text of the nav option, for a count or a short status ("3", "New").
    /// The option keeps its icon, its text and its states, and the badge is rendered after a custom template that
    /// renders in the Normal mode too. Takes precedence over <see cref="Dot"/> when both are set.
    /// </summary>
    /// <remarks>
    /// A group header in the Grouped render type does not render it, and neither does a separator or a template
    /// rendered in the Replace mode, which takes the place of everything the option would render. The IconOnly nav pins
    /// it to the corner of the icon.
    /// </remarks>
    [Parameter] public string? Badge { get; set; }

    /// <summary>
    /// The accessible description of the badge (or the dot) of the nav option, folded into the accessible name of
    /// the option so a count that only exists as a colored bubble is not lost on a screen reader ("Inbox (3 unread)").
    /// It falls back to the <see cref="Badge"/> text, and a <see cref="Dot"/> is only announced while this is set,
    /// since a dot carries no text of its own.
    /// </summary>
    [Parameter] public string? BadgeAriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the nav option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A list of options to render as children of the current nav option.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Aria label of the toggle button when the nav option is expanded and can be collapsed.
    /// </summary>
    [Parameter] public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The custom data for the nav option to provide additional state.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// The description for the nav option.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Renders a small dot after the text of the nav option, to mark it as needing attention without showing a
    /// number. Ignored while <see cref="Badge"/> is set.
    /// </summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>
    /// Aria label of the toggle button when the nav option is collapsed and can be expanded.
    /// </summary>
    [Parameter] public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Forces an anchor element render instead of button.
    /// </summary>
    [Parameter] public bool ForceAnchor { get; set; }

    /// <summary>
    /// Icon to render next to the nav option.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </summary>
    /// <example>
    /// Bootstrap: Icon=BitIconInfo.Bi("gear-fill")
    /// FontAwesome: Icon=BitIconInfo.Fa("solid house")
    /// Custom CSS: Icon=BitIconInfo.Css("my-icon-class")
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav option.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav option is disabled.
    /// </summary>
    [Parameter] public bool IsDisabled { get; set; }

    /// <summary>
    /// Whether or not the nav option is in an expanded state.
    /// </summary>
    /// <remarks>
    /// The value given is the state the option starts in, and the state it moves to whenever the value given changes;
    /// in between, the option keeps whatever the reader (or the nav's API) expanded or collapsed it to.
    /// </remarks>
    [Parameter]
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            // The markup hands its value over again on every render of the nav (the options are its content), so only
            // a value that differs from the last one handed over is applied: re-applying the same one would undo every
            // toggle of the reader at once, and an option written as expanded could never be collapsed.
            if (_isExpandedParameter == value) return;

            _isExpandedParameter = value;
            _isExpanded = value;
        }
    }

    /// <summary>
    /// Indicates that the nav option should render as a separator.
    /// </summary>
    [Parameter] public bool IsSeparator { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the nav option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a value representing the URL matching behavior of the nav option.
    /// </summary>
    [Parameter] public BitNavMatch? Match { get; set; }

    /// <summary>
    /// The relationship of the link of the nav option to the current document, rendered as the rel attribute of its anchor.
    /// </summary>
    /// <remarks>
    /// A link opening a new tab (<see cref="Target"/> of <c>_blank</c>) gets <c>noopener</c> added to whatever this
    /// says, unless it already says what the opener relationship should be (<see cref="BitLinkRels.NoOpener"/>,
    /// <see cref="BitLinkRels.NoReferrer"/> or <see cref="BitLinkRels.Opener"/>). Nothing adds <c>noreferrer</c> on its
    /// own: ask for it here where the page the link leads to must not learn which page it was followed from.
    /// </remarks>
    [Parameter] public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Custom CSS style for the nav option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The custom template to render at the end of the nav option, after its text and ahead of its badge, while
    /// the option keeps its default content. Takes precedence over the ItemSuffixTemplate of the nav.
    /// </summary>
    /// <remarks>
    /// It is rendered inside the anchor (or the button) the option is, so it holds content to look at - a counter,
    /// a status, a shortcut - and never a control of its own: a nested interactive element cannot be reached or
    /// named by assistive technologies. It is part of the accessible name of the option unless the option is named
    /// through an aria-label, so mark what it draws with <c>aria-hidden="true"</c> when it repeats the badge. The
    /// IconOnly nav, which is down to the icons of its options, does not render it.
    /// </remarks>
    [Parameter] public RenderFragment<BitNavOption>? SuffixTemplate { get; set; }

    /// <summary>
    /// Link target, specifies how to open the nav option's link.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the nav option to render.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? Template { get; set; }

    /// <summary>
    /// The render mode of the nav option's custom template.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode TemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Text to render for the nav option.
    /// </summary>
    [Parameter] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for the tooltip of the nav option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The nav option's link URL.
    /// </summary>
    [Parameter] public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected nav option by the current URL.
    /// </summary>
    [Parameter] public IEnumerable<string>? AdditionalUrls { get; set; }



    internal int Depth => Parent is null ? 0 : Parent.Depth + 1;

    // The expansion state the nav moves the option to, which the IsExpanded handed over by the markup only replaces
    // once that value changes.
    internal void SetExpanded(bool value) => _isExpanded = value;

    internal void InternalRecursiveStateHasChanged()
    {
        StateHasChanged();

        foreach (var child in ChildItems)
        {
            child.InternalRecursiveStateHasChanged();
        }
    }



    protected override async Task OnInitializedAsync()
    {
        if (Parent is null)
        {
            Nav?.RegisterOption(this);
        }
        else
        {
            Parent.ChildItems.Add(this);
        }

        // A nested option only re-renders its parent, not the nav, so the tree the nav has cached the
        // ancestors of its selected item from is told it changed shape.
        Nav?.InvalidateSelectedAncestors();

        if (Nav is not null)
        {
            Nav.SetItemExpanded(this, Nav.AllExpanded || (Nav.GetIsExpanded(this) ?? false));

            if (Nav.Mode == BitNavMode.Automatic)
            {
                // The nav decides when the URL match runs: a single pass after this registration batch renders
                // rather than one per option (which would flatten and scan the whole tree each time), except for
                // the options of the first render, whose selection has to be in the HTML a prerender sends.
                // It is asked after the expansion state is set, so the path the match opens is not undone here.
                Nav.OnOptionRegistered(this);
            }
        }

        Parent?.StateHasChanged();

        await base.OnInitializedAsync();
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        if (Parent is null)
        {
            Nav?.UnregisterOption(this);
        }
        else
        {
            Parent.ChildItems.Remove(this);
        }

        Nav?.InvalidateSelectedAncestors();

        Nav?.SetSelectedItemByCurrentUrl();

        Parent?.StateHasChanged();

        _disposed = true;
    }
}
