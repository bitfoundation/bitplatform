namespace Bit.BlazorUI;

public partial class BitNavOption : ComponentBase, IDisposable
{
    private bool _disposed;



    internal IList<BitNavOption> ChildItems { get; set; } = [];



    [CascadingParameter] protected BitNavOption? Parent { get; set; }

    [CascadingParameter] protected BitNav<BitNavOption> Nav { get; set; } = default!;



    /// <summary>
    /// Aria-current token for active nav option. Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// Aria label for nav option. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.
    /// </summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the nav option.
    /// </summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>
    /// A list of options to render as children of the current nav option.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Aria label when nav  option is collapsed and can be expanded.
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
    /// Aria label when nav option is collapsed and can be expanded.
    /// </summary>
    [Parameter] public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Forces an anchor element render instead of button.
    /// </summary>
    [Parameter] public bool ForceAnchor { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav option.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav option is enabled.
    /// </summary>
    [Parameter] public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether or not the nav option is in an expanded state.
    /// </summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>
    /// Indicates that the nav option should render as a separator.
    /// </summary>
    [Parameter] public bool IsSeparator { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the nav option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Custom CSS style for the nav option.
    /// </summary>
    [Parameter] public string? Style { get; set; }

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



    protected override async Task OnInitializedAsync()
    {
        if (Parent is null)
        {
            Nav.RegisterOption(this);
        }
        else
        {
            Parent.ChildItems.Add(this);
        }

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

        if (Parent is null)
        {
            Nav.UnregisterOption(this);
        }
        else
        {
            Parent.ChildItems.Remove(this);
        }

        _disposed = true;
    }
}
