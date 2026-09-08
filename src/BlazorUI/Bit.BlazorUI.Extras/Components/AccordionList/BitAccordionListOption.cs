namespace Bit.BlazorUI;

/// <summary>
/// Represents a single item (panel) of the <see cref="BitAccordionList{TItem}"/> component provided as a child component.
/// </summary>
public partial class BitAccordionListOption : ComponentBase, IAsyncDisposable
{
    private bool _disposed;


    [CascadingParameter] protected BitAccordionList<BitAccordionListOption>? Parent { get; set; }


    /// <summary>
    /// The content rendered beside the header of the option, outside of the toggle button and of the heading
    /// it sits in, so that it can hold its own interactive elements (a menu, a delete button, a switch).
    /// The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? Actions { get; set; }

    /// <summary>
    /// The custom CSS classes of the option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A short description rendered in the header of the option.
    /// </summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon to show in place of the expander icon while the option is expanded, using custom
    /// CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandedExpanderIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon, from the built-in Fluent UI icons, to show in place of the expander
    /// icon while the option is expanded.
    /// </summary>
    [Parameter] public string? ExpandedExpanderIconName { get; set; }

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
    /// The custom content to render in place of the expander icon of the option, leaving the rest of the header
    /// as it is. The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? ExpanderTemplate { get; set; }

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
    /// The accessible label of the toggle button in the header of the option, for a header whose own content does
    /// not name it - an icon-only header template, most of all.
    /// </summary>
    [Parameter] public string? HeaderAriaLabel { get; set; }

    /// <summary>
    /// The custom template for the header of the option. The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? HeaderTemplate { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of the option, overriding the value of the AccordionList.
    /// </summary>
    [Parameter] public bool? HideExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon to display at the start of the header of the option using custom CSS classes for
    /// external icon libraries. Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display at the start of the header of the option from the built-in
    /// Fluent UI icons.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

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
    /// Leaves the option where it is: its header keeps its colors and its place in the tab order, but it no
    /// longer answers the pointer or the keyboard. Overrides the value of the AccordionList.
    /// </summary>
    [Parameter] public bool? ReadOnly { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The title (header text) of the option.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// The custom content to render in place of the <see cref="Title"/> of the option, leaving the rest of the
    /// header as it is. The context value provides the option itself.
    /// </summary>
    [Parameter] public RenderFragment<BitAccordionListOption>? TitleTemplate { get; set; }


    internal void InternalStateHasChanged()
    {
        StateHasChanged();
    }



    protected override async Task OnInitializedAsync()
    {
        // An option outside of an accordion list, or inside one closed over another item type, receives no
        // cascading parent and would otherwise render nothing at all without saying why.
        if (Parent is null)
        {
            throw new InvalidOperationException(
                $"{nameof(BitAccordionListOption)} must be placed inside a BitAccordionList whose TItem is {nameof(BitAccordionListOption)}.");
        }

        Parent.RegisterOption(this);

        await base.OnInitializedAsync();
    }

    // Renders the option's item in place, so the rendered order of the items always follows the
    // markup order of the options, even when an option is added or removed conditionally later on.
    protected override void OnParametersSet()
    {
        // The list hands its options their parameters in markup order, every one of them, every time it
        // renders - which is the order the list wants its items in, and not the order they registered
        // themselves in once one of them has been added conditionally. A render of the option's own is no
        // use here: only the option that was just added has one when it is added.
        Parent?.ReportOptionOrder(this);

        base.OnParametersSet();
    }

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
