namespace Bit.BlazorUI;

public partial class BitDropdownOption<TValue> : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitDropdown<BitDropdownOption<TValue>, TValue> Parent { get; set; } = default!;


    /// <summary>
    /// The aria label attribute for the dropdown option.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the dropdown option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon name from the Fluent UI icon set to display for the dropdown option.
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The id for the dropdown option.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The custom data for the dropdown item to provide extra state for the template.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// Determines if the dropdown option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines if the dropdown option is hidden.
    /// </summary>
    [Parameter] public bool IsHidden { get; set; }

    /// <summary>
    /// The type of the dropdown option.
    /// </summary>
    [Parameter] public BitDropdownItemType ItemType { get; set; } = BitDropdownItemType.Normal;

    /// <summary>
    /// Custom CSS style for the dropdown option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The text to render for the dropdown option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The title attribute for the dropdown option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The value of the dropdown option.
    /// </summary>
    [Parameter] public TValue? Value { get; set; }



    /// <summary>
    /// Determines if the option is selected. This property's value is assigned by the component.
    /// </summary>
    public bool IsSelected { get; internal set; }



    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
        Parent?.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // The parent caches search results keyed on the option instances; when this option's own
        // parameters change (e.g. Text) the parent must drop that cache so the new data is searched.
        Parent?.NotifyOptionParametersChanged();

        base.OnParametersSet();
    }

    // Renders the option's item in place, so the rendered order of the items always follows the
    // markup order of the options, even when an option is added or removed conditionally later on.
    // In virtualize mode the dropdown itself renders the items out of its Items collection, so the
    // option renders nothing (it only registers itself).
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Parent is null || Parent.Virtualize) return;
        if (Parent.ShouldRenderOptionItem(this) is false) return;

        builder.OpenComponent<_BitDropdownItem<BitDropdownOption<TValue>, TValue>>(0);
        builder.AddComponentParameter(1, nameof(_BitDropdownItem<BitDropdownOption<TValue>, TValue>.Dropdown), Parent);
        builder.AddComponentParameter(2, nameof(_BitDropdownItem<BitDropdownOption<TValue>, TValue>.Item), this);
        builder.AddComponentParameter(3, nameof(_BitDropdownItem<BitDropdownOption<TValue>, TValue>.ItemCheckIconCss), Parent.GetItemCheckIconCss());
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

        Parent?.UnregisterOption(this);

        _disposed = true;
    }
}
