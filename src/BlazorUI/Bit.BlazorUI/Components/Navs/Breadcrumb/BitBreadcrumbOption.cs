namespace Bit.BlazorUI;

public partial class BitBreadcrumbOption : ComponentBase, IDisposable
{
    internal const string _OPTION_ID_ATTRIBUTE = "data-bit-brc-opt";

    internal string _OptionId { get; } = BitShortId.NewId();

    private bool _disposed;
    private string? _lastParametersSignature;
    private RenderFragment<BitBreadcrumbOption>? _lastTemplate;
    private RenderFragment<BitBreadcrumbOption>? _lastOverflowTemplate;



    [CascadingParameter] protected BitBreadcrumb<BitBreadcrumbOption> Parent { get; set; } = default!;



    /// <summary>
    /// CSS class attribute for breadcrumb option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// URL to navigate to when the breadcrumb option is clicked.
    /// If provided, the breadcrumb option will be rendered as a link.
    /// </summary>
    [Parameter] public string? Href { get; set; }

    /// <summary>
    /// Icon to render next to the item text.
    /// </summary>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether an option is enabled or not.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Display the breadcrumb option as the selected option.
    /// </summary>
    [Parameter] public bool IsSelected { get; set; }

    /// <summary>
    /// A unique value to use as a key of the breadcrumb option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the breadcrumb option.
    /// </summary>
    [Parameter] public EventCallback<BitBreadcrumbOption> OnClick { get; set; }

    /// <summary>
    /// The custom template for the option in overflow list.
    /// </summary>
    [Parameter] public RenderFragment<BitBreadcrumbOption>? OverflowTemplate { get; set; }

    /// <summary>
    /// Reverses the positions of the icon and the item text of the item content.
    /// </summary>
    [Parameter] public bool? ReversedIcon { get; set; }

    /// <summary>
    /// Style attribute for breadcrumb option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// The custom template for the option.
    /// </summary>
    [Parameter] public RenderFragment<BitBreadcrumbOption>? Template { get; set; }

    /// <summary>
    /// Text to display in the breadcrumb option.
    /// </summary>
    [Parameter] public string? Text { get; set; }



    protected override void OnInitialized()
    {
        Parent?.RegisterOptions(this);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // The parent renders the visible items from the options and reads their data during its own
        // render, so a change to an option's parameters isn't reflected until the parent re-renders.
        // Notify it only when a rendered parameter actually changes; the guard avoids a render loop
        // since OnParametersSet runs on every parent render. Reference-type params are folded into a
        // value-based signature (e.g. the icon's CSS classes) so an equal-but-new instance won't churn.
        // Template/OverflowTemplate are compared by reference identity since the parent renders them too.
        var signature = string.Join('\u001F', Text, Href, IconName, Icon?.GetCssClasses(), IsEnabled, IsSelected, Class, Style, ReversedIcon, Key);

        var changed = _lastParametersSignature != signature ||
                      ReferenceEquals(_lastTemplate, Template) is false ||
                      ReferenceEquals(_lastOverflowTemplate, OverflowTemplate) is false;

        if (_lastParametersSignature is not null && changed)
        {
            Parent?.NotifyOptionParametersChanged();
        }

        _lastParametersSignature = signature;
        _lastTemplate = Template;
        _lastOverflowTemplate = OverflowTemplate;

        base.OnParametersSet();
    }

    // Renders a hidden marker element in place, whose DOM order (in the hidden options container of
    // the breadcrumb) is used to keep the order of the registered items in sync with the markup order
    // of the options, even when an option is added or removed conditionally later on.
    // The option renders nothing but this single flat marker, so the breadcrumb's DOM read-back
    // (Utils.getChildrenAttributes) sees exactly one marker per option, in markup order.
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, _OPTION_ID_ATTRIBUTE, _OptionId);
        builder.CloseElement();
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || disposing is false) return;

        Parent?.UnregisterOptions(this);

        _disposed = true;
    }
}
