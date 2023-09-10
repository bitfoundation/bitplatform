﻿
namespace Bit.BlazorUI;

public partial class BitNavOption : IDisposable
{
    private bool _disposed;


    internal IList<BitNavOption> Items { get; set; } = new List<BitNavOption>();


    [CascadingParameter] protected BitNav<BitNavOption> Nav { get; set; } = default!;
    [CascadingParameter] protected BitNavOption? Parent { get; set; }



    /// <summary>
    /// Aria-current token for active nav links.
    /// Must be a valid token value, and defaults to 'page'
    /// </summary>
    [Parameter] public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Aria label when items is collapsed and can be expanded
    /// </summary>
    [Parameter] public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The custom data for the nav option to provide additional state for the option.
    /// </summary>
    [Parameter] public object? Data { get; set; }

    /// <summary>
    /// The description for the nav option.
    /// </summary>
    [Parameter] public string? Description { get; set; }

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
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the link is in an expanded state
    /// </summary>
    [Parameter] public bool IsExpanded { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the item
    /// </summary>
    [Parameter] public string? Key { get; set; }

    /// <summary>
    /// Link target, specifies how to open the link
    /// </summary>
    [Parameter] public string? Target { get; set; }

    /// <summary>
    /// The custom template for the BitNavOption to render.
    /// </summary>
    [Parameter] public RenderFragment<BitNavOption>? Template { get; set; }

    /// <summary>
    /// The render mode of the BitNavOption's custom template.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode TemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Text to render for this link.
    /// </summary>
    [Parameter] public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for title tooltip.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// URL to navigate to for this link
    /// </summary>
    [Parameter] public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected item by the current URL.
    /// </summary>
    [Parameter] public IEnumerable<string>? AdditionalUrls { get; set; }



    protected override string RootElementClass => "bit-nvgo";

    protected override async Task OnInitializedAsync()
    {
        if (Parent is null)
        {
            Nav.RegisterOption(this);
        }
        else
        {
            Parent.Items.Add(this);
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
            Parent.Items.Remove(this);
        }

        _disposed = true;
    }
}
