namespace Bit.BlazorUI;

/// <summary>
/// Represents a single item (panel) of the <see cref="BitAccordionList{TItem}"/> component provided as a child component.
/// </summary>
public partial class BitAccordionListOption : ComponentBase, IAsyncDisposable
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
    /// The default child content of the option. Used for simple inline content without context.
    /// For templated content with access to the option instance, use <see cref="Body"/> instead.
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


    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
         if (Parent is not null)
         {
             Parent.RegisterOption(this);
         }

        await base.OnInitializedAsync();
    }

    // Renders the option's item in place, so the rendered order of the items always follows the
    // markup order of the options, even when an option is added or removed conditionally later on.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Parent is null) return;

        builder.OpenComponent<_BitAccordionListItem<BitAccordionListOption>>(0);
        builder.AddComponentParameter(1, nameof(_BitAccordionListItem<BitAccordionListOption>.AccordionList), Parent);
        builder.AddComponentParameter(2, nameof(_BitAccordionListItem<BitAccordionListOption>.Item), this);
        builder.CloseComponent();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false || _disposed) return;

         if (Parent is not null)
         {
            // Await the unregistration so that any UpdateBoundKeys or ExpandedKey(s) callbacks it
            // triggers are awaited and observed, rather than running as fire-and-forget.
             await Parent.UnregisterOption(this);
         }

        _disposed = true;
    }
}
