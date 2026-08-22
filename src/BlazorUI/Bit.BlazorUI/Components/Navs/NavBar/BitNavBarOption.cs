namespace Bit.BlazorUI;

public partial class BitNavBarOption : ComponentBase, IDisposable
{
    // The marker the navbar reads the markup order of the options back from, and the value that tells the
    // options apart in that read-back.
    internal const string _OPTION_ID_ATTRIBUTE = "data-bit-nbr-opt";

    internal string _OptionId { get; } = BitShortId.NewId();

    private bool _disposed;
    private string? _lastUrl;
    private BitNavMatch? _lastMatch;
    private IEnumerable<string>? _lastAdditionalUrls;



    [CascadingParameter] protected BitNavBar<BitNavBarOption> NavBar { get; set; } = default!;



    /// <summary>
    /// The value of the aria-current attribute of the navbar option when it is the selected one.
    /// </summary>
    [Parameter] public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// The accessible label of the navbar option, announced instead of its content.
    /// When it is not provided and the text of the option is not rendered (in the IconOnly or the
    /// HideUnselectedText modes), the text of the option is used, so an icon is never left unnamed.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// The badge text to render on the icon of the navbar option, for a count or a short status.
    /// Takes precedence over <see cref="Dot"/> when both are set.
    /// </summary>
    [Parameter] public string? Badge { get; set; }

    /// <summary>
    /// The accessible description of the badge (or the dot) of the navbar option, folded into the accessible
    /// name of the option so a count that only exists as a colored bubble is not lost on a screen reader
    /// ("Messages (5 unread)"). It falls back to the <see cref="Badge"/> text, and a <see cref="Dot"/> is
    /// only announced while this is set, since a dot carries no text of its own.
    /// </summary>
    [Parameter] public string? BadgeAriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the navbar option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The custom data for the navbar option to provide additional state.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// Renders a small dot on the icon of the navbar option, to mark it as needing attention without
    /// showing a number. Ignored while <see cref="Badge"/> is set.
    /// </summary>
    [Parameter] public bool Dot { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// FontAwesome: Icon=BitIconInfo.Fa("solid house")
    /// Bootstrap: Icon=BitIconInfo.Bi("gear-fill")
    /// Custom CSS: Icon=BitIconInfo.Css("my-icon-class")
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Home</c>).
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the navbar option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key or id of the navbar option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Modifies how the URL of the navbar option is matched against the current URL in the automatic mode.
    /// Takes precedence over the Match of the navbar itself.
    /// </summary>
    [Parameter] public BitNavMatch? Match { get; set; }

    /// <summary>
    /// The icon to display while the navbar option is the selected one, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// A navigation bar conventionally marks its current destination with the filled variant of the same
    /// glyph, which is what this pair of parameters is for. While neither is set, the selected option keeps
    /// its <see cref="Icon"/> / <see cref="IconName"/>.
    /// </remarks>
    [Parameter] public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// The name of the icon to display from the built-in Fluent UI icons while the navbar option is the
    /// selected one. For external icon libraries, use <see cref="SelectedIcon"/> instead.
    /// </summary>
    [Parameter] public string? SelectedIconName { get; set; }

    /// <summary>
    /// Custom CSS style for the navbar option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Link target, specifies how to open the navbar option's link.
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the navbar option to render.
    /// </summary>
    [Parameter] public RenderFragment<BitNavBarOption>? Template { get; set; }

    /// <summary>
    /// Text to render for the navbar option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Text for the tooltip of the navbar option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The navbar option's link URL.
    /// </summary>
    [Parameter] public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected navbar option by the current URL.
    /// </summary>
    [Parameter] public IEnumerable<string>? AdditionalUrls { get; set; }



    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
        NavBar?.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    // A URL (or the way it is matched) that changes after the option was registered points the option at
    // another page, so the automatic mode has to look for its selected option again. Only an actual change
    // flags it: flagging on every parameter set would have each recompute render, and each render flag
    // another recompute.
    protected override void OnParametersSet()
    {
        var additionalUrls = AdditionalUrls?.ToArray();

        if (Url != _lastUrl || Match != _lastMatch ||
            (additionalUrls ?? []).SequenceEqual(_lastAdditionalUrls ?? []) is false)
        {
            _lastUrl = Url;
            _lastMatch = Match;
            _lastAdditionalUrls = additionalUrls;

            NavBar?.MarkSelectionDirty();
        }

        base.OnParametersSet();
    }

    // Renders the option's item in place, so the rendered order of the items always follows the
    // markup order of the options, even when an option is added or removed conditionally later on.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (NavBar is null) return;

        builder.OpenComponent<_BitNavBarItem<BitNavBarOption>>(0);
        builder.AddComponentParameter(1, nameof(_BitNavBarItem<BitNavBarOption>.NavBar), NavBar);
        builder.AddComponentParameter(2, nameof(_BitNavBarItem<BitNavBarOption>.Item), this);
        builder.CloseComponent();
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        NavBar?.UnregisterOption(this);

        _disposed = true;
    }
}
