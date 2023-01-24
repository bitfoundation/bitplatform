
namespace Bit.BlazorUI;

public partial class BitNavOption : IDisposable
{
    internal IList<BitNavOption> _options = new List<BitNavOption>();

    [CascadingParameter] protected BitNavGroup NavGroup { get; set; } = default!;
    [CascadingParameter] protected BitNavOption? NavOption { get; set; }

    /// <summary>
    /// Aria-current token for active nav links.
    /// Must be a valid token value, and defaults to 'page'
    /// </summary>
    [Parameter] public BitNavOptionAriaCurrent AriaCurrent { get; set; } = BitNavOptionAriaCurrent.Page;

    /// <summary>
    /// Aria label when options is collapsed and can be expanded
    /// </summary>
    [Parameter] public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// A list of options to render as children of the current option
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Aria label when group is collapsed and can be expanded.
    /// </summary>
    [Parameter] public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. 
    /// Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public bool ForceAnchor { get; set; }

    /// <summary>
    /// Name of an icon to render next to this link button
    /// </summary>
    [Parameter] public BitIconName? IconName { get; set; }

    /// <summary>
    /// Whether or not the link is in an expanded state
    /// </summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the option.
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Text to render for this link.
    /// </summary>
    [Parameter] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for title tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Link target, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// URL to navigate to for this link
    /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
    [Parameter] public string? Url { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    protected override string RootElementClass => "bit-nvgo";

    protected override async Task OnInitializedAsync()
    {        
        if (NavOption is null)
        {
            NavGroup.RegisterOptions(this);
        }

        if (NavOption is not null)
        {
            NavGroup.RegisterChildOptions(NavOption, this);
        }

        await base.OnInitializedAsync();
    }

    internal void SetIsExpanded(bool value) => IsExpanded = value;
    internal void SetKey(string value) => Key = value;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (NavOption is null)
        {
            NavGroup.UnregisterOptions(this);
        }

        if (NavOption is not null)
        {
            NavGroup.UnregisterChildOptions(NavOption, this);
        }
    }
}
