namespace Bit.BlazorUI;

public partial class BitButtonGroupOption : ComponentBase, IDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitButtonGroup<BitButtonGroupOption> Parent { get; set; } = default!;


    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
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
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
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



    protected override async Task OnInitializedAsync()
    {
        Parent.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        Parent.UnregisterOption(this);

        _disposed = true;
    }
}
