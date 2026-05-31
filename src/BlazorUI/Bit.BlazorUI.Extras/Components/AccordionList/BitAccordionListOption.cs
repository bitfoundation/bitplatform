namespace Bit.BlazorUI;

/// <summary>
/// Represents a single item (panel) of the <see cref="BitAccordionList{TItem}"/> component provided as a child component.
/// </summary>
public partial class BitAccordionListOption : ComponentBase, IDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitAccordionList<BitAccordionListOption> Parent { get; set; } = default!;


    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A short description rendered in the header of the option.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon to display as the expander using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpanderIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display as the expander from the built-in Fluent UI icons.
    /// </summary>
    [Parameter] public string? ExpanderIconName { get; set; }

    /// <summary>
    /// The content (body) of the option that is shown when the option is expanded. The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? Body { get; set; }

    /// <summary>
    /// Alias for the <see cref="Body"/> parameter (the default child content). Used for simple inline content.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The custom template for the header of the option. The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? HeaderTemplate { get; set; }

    /// <summary>
    /// Whether or not the option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines whether the option is initially expanded.
    /// </summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>
    /// A unique value to use as the key of the option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// The click event handler of the header of the option.
    /// </summary>
    [Parameter] public EventCallback<BitAccordionListOption> OnClick { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The title (header text) of the option.
    /// </summary>
    [Parameter] public string? Title { get; set; }


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
