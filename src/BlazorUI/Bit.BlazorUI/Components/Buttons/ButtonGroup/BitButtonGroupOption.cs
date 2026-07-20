namespace Bit.BlazorUI;

public partial class BitButtonGroupOption : ComponentBase, IDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitButtonGroup<BitButtonGroupOption> Parent { get; set; } = default!;


    /// <summary>
    /// The accessible label of the option, rendered as the aria-label attribute.
    /// </summary>
    /// <remarks>
    /// Required for icon-only options, and strongly recommended in toggle mode when
    /// <see cref="OnText"/>/<see cref="OffText"/> are used, so that the accessible name
    /// of the option stays the same while its toggle state changes.
    /// </remarks>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// The content of the badge rendered at the end of the option, usually a short count.
    /// </summary>
    [Parameter] public string? Badge { get; set; }

    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The url of the link rendered by the option. If provided, the option renders as an anchor tag instead of a button.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the option text
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether or not the option is in the loading state, which replaces its icon with a spinner and blocks its click.
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// A unique value to use as a key of the option
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the option is not checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OffIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OffIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OffIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: OffIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: OffIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? OffIcon { get; set; }

    /// <summary>
    /// The icon of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffIconName { get; set; }

    /// <summary>
    /// The text of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffText { get; set; }

    /// <summary>
    /// The title of the option when it is not checked in toggle mode.
    /// </summary>
    [Parameter] public string? OffTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display when the option is checked using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="OnIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="OnIconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: OnIcon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: OnIcon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? OnIcon { get; set; }

    /// <summary>
    /// The icon of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnIconName { get; set; }

    /// <summary>
    /// The text of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnText { get; set; }

    /// <summary>
    /// The title of the option when it is checked in toggle mode.
    /// </summary>
    [Parameter] public string? OnTitle { get; set; }

    /// <summary>
    /// Click event handler of the option.
    /// </summary>
    [Parameter] public EventCallback<BitButtonGroupOption> OnClick { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the main content of the option.
    /// </summary>
    [Parameter] public bool ReversedIcon { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The target attribute of the link when the option renders as an anchor (by providing the Href parameter).
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitButtonGroupOption>? Template { get; set; }

    /// <summary>
    /// Text to render in the option
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Title to render in the option
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Determines if the item is toggled. This property's value is assigned by the component.
    /// </summary>
    public bool IsToggled { get; internal set; }



    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
        Parent?.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    // Renders the option's item in place, so the rendered order of the items always follows the
    // markup order of the options, even when an option is added or removed conditionally later on.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Parent is null) return;

        builder.OpenComponent<_BitButtonGroupItem<BitButtonGroupOption>>(0);
        builder.AddComponentParameter(1, nameof(_BitButtonGroupItem<BitButtonGroupOption>.ButtonGroup), Parent);
        builder.AddComponentParameter(2, nameof(_BitButtonGroupItem<BitButtonGroupOption>.Item), this);
        builder.CloseComponent();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        Parent?.UnregisterOption(this);

        _disposed = true;
    }
}
