namespace Bit.BlazorUI;

public class BitMenuButtonOption : ComponentBase, IDisposable
{
    private bool _disposed;

    [CascadingParameter] protected BitMenuButton<BitMenuButtonOption> Parent { get; set; } = default!;


    /// <summary>
    /// The accessible name of the option, for the benefit of screen readers.
    /// Set it on an option whose visible label is an icon alone, or is too terse to stand on its own.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Turns the option into a check item: it is announced as a checkbox inside the menu, carries its
    /// <see cref="IsChecked"/> state as a check mark, and flips that state when it is clicked.
    /// </summary>
    /// <remarks>
    /// A menu of check items is usually one the user works inside of, so pair it with
    /// <c>CloseOnItemClick="false"</c> on the menu button to keep the callout open between the toggles.
    /// </remarks>
    [Parameter] public bool Checkable { get; set; }

    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// The value of the href attribute of the option. If provided, the option renders as an anchor tag instead of button.
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
    /// Bootstrap: OnIcon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon="BitIconInfo.Fa("solid house")"
    /// Custom CSS: Icon="BitIconInfo.Css("my-icon-class")"
    /// </example>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the option text.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// The checked state of a <see cref="Checkable"/> option, which supports two-way binding
    /// (<c>@bind-IsChecked</c>). The menu button flips it as the option is clicked.
    /// </summary>
    [Parameter] public bool IsChecked { get; set; }

    /// <summary>
    /// The callback that is called when the <see cref="IsChecked"/> value changes, which is what makes
    /// <c>@bind-IsChecked</c> work.
    /// </summary>
    [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

    /// <summary>
    /// Whether or not the option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// If true, the option renders as the label of the group of options that follow it, instead of as a
    /// clickable item. It is presentational: the keyboard navigation steps over it.
    /// </summary>
    [Parameter] public bool IsHeader { get; set; }

    /// <summary>
    /// Determines the selection state of the item.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// If true, the option renders as a separator line instead of a clickable item.
    /// </summary>
    [Parameter] public bool IsSeparator { get; set; }

    /// <summary>
    /// A unique value to use as a key of the option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the option.
    /// </summary>
    [Parameter] public EventCallback<BitMenuButtonOption> OnClick { get; set; }

    /// <summary>
    /// The trailing text of the option, shown at its far end and read after its label - a keyboard shortcut,
    /// a count, a short hint.
    /// </summary>
    [Parameter] public string? SecondaryText { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The value of the target attribute of the option when the option renders as an anchor tag (by providing the Href value).
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitMenuButtonOption>? Template { get; set; }

    /// <summary>
    /// Text to render in the option.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the option.
    /// </summary>
    [Parameter] public string? Title { get; set; }


    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }

    // The checked state is written back through here rather than assigned from the outside, so that an option
    // bound with @bind-IsChecked reports the change to the page and one left unbound still keeps it: the
    // parameter holds the value until the page's next render, which only overwrites it if the page has a value
    // of its own to write.
    internal async Task SetIsChecked(bool value)
    {
        if (IsChecked == value) return;

        IsChecked = value;

        await IsCheckedChanged.InvokeAsync(value);

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

        builder.OpenComponent<_BitMenuButtonItem<BitMenuButtonOption>>(0);
        builder.AddComponentParameter(1, nameof(_BitMenuButtonItem<BitMenuButtonOption>.MenuButton), Parent);
        builder.AddComponentParameter(2, nameof(_BitMenuButtonItem<BitMenuButtonOption>.Item), this);
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
