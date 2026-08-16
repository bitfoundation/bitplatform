using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The Timeline component organizes and displays events or data chronologically in a linear fashion, often featuring points or segments representing individual items with associated details or actions.
/// </summary>
public partial class BitTimeline<TItem> : BitComponentBase where TItem : class
{
    private List<TItem> _items = [];
    private IEnumerable<TItem> _oldItems = default!;



    /// <summary>
    /// Alternates the side of the items, so each item sits on the opposite side of the line of the item before it.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Alternate { get; set; }

    /// <summary>
    /// The content of the BitTimeline, that are BitTimelineOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTimeline.
    /// </summary>
    [Parameter] public BitTimelineClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the timeline.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The default custom template for the dot of the items, used by the items that provide no dot template of their own.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? DotTemplate { get; set; }

    /// <summary>
    /// Defines whether to render timeline children horizontally.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    ///  List of Item, each can be with different contents in the timeline.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The default custom template that replaces the whole content of the items,
    /// used by the items that provide no template of their own.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitTimelineNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback that is called when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Renders the items in the reverse order, so the last item of the list is rendered first.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReverseOrder { get; set; }

    /// <summary>
    /// Reverses all of the timeline items direction, so their contents swap sides of the line.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// The size of timeline, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTimeline.
    /// </summary>
    [Parameter] public BitTimelineClassStyles? Styles { get; set; }

    /// <summary>
    /// Truncates the connecting line of the timeline at the first dot, the last dot, or both of them.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTimelineTruncateLine? TruncateLine { get; set; }

    /// <summary>
    /// The visual variant of the timeline.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }



    internal void RegisterOption(BitTimelineOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        StateHasChanged();
    }

    internal void UnregisterOption(BitTimelineOption option)
    {
        // The options unregister while they are being disposed, which also happens when the timeline
        // itself goes away, so a re-render is only requested while the timeline is still alive.
        if (IsDisposed) return;

        _items.Remove((option as TItem)!);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-tln";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-tln-pri",
            BitColor.Secondary => "bit-tln-sec",
            BitColor.Tertiary => "bit-tln-ter",
            BitColor.Info => "bit-tln-inf",
            BitColor.Success => "bit-tln-suc",
            BitColor.Warning => "bit-tln-wrn",
            BitColor.SevereWarning => "bit-tln-swr",
            BitColor.Error => "bit-tln-err",
            _ => "bit-tln-pri"
        });

        ClassBuilder.Register(() => Horizontal ? "bit-tln-hrz" : string.Empty);

        ClassBuilder.Register(() => Reversed ? "bit-tln-rvs" : string.Empty);

        ClassBuilder.Register(() => Alternate ? "bit-tln-alt" : string.Empty);

        ClassBuilder.Register(() => ReverseOrder ? "bit-tln-rvo" : string.Empty);

        // The truncation classes key off the first and the last item in the DOM, so reversing the
        // rendered order swaps which end of the line the Start and the End values refer to.
        ClassBuilder.Register(() => (TruncateLine, ReverseOrder) switch
        {
            (BitTimelineTruncateLine.Start, false) or (BitTimelineTruncateLine.End, true) => "bit-tln-tls",
            (BitTimelineTruncateLine.End, false) or (BitTimelineTruncateLine.Start, true) => "bit-tln-tle",
            (BitTimelineTruncateLine.Both, _) => "bit-tln-tlb",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-tln-sm",
            BitSize.Medium => "bit-tln-md",
            BitSize.Large => "bit-tln-lg",
            _ => "bit-tln-md"
        });

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-tln-fil",
            BitVariant.Outline => "bit-tln-otl",
            BitVariant.Text => "bit-tln-txt",
            _ => "bit-tln-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override Task OnParametersSetAsync()
    {
        // Note: no Items.Any() guard here, so a transition from a populated collection to an empty one
        // still runs the comparison and clears _items instead of leaving the previous items rendered.
        if (ChildContent is null && Options is null)
        {
            var items = Items ?? [];

            if (_oldItems is null || items.SequenceEqual(_oldItems) is false)
            {
                _items = items.ToList();
                // Store an independent snapshot so a later in-place mutation of the same Items instance is
                // still detected (a reference copy would compare the collection against itself).
                _oldItems = _items.ToList();
            }
        }

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // timeline's own parameters (Styles, IsEnabled, ...) change, so push a re-render to each one.
        RefreshOptions();

        return base.OnParametersSetAsync();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitTimelineOption)?.InternalStateHasChanged();
        }
    }

    // Honors the item's own Key when provided (stable identity across reordering) and otherwise falls
    // back to a per-component, position-based key so that duplicate/equal items cannot collide.
    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    private string? GetKey(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Key;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }



    internal string? GetItemClasses(TItem item, bool isInteractive)
    {
        StringBuilder className = new StringBuilder();

        var color = GetColor(item);
        if (color is not null)
        {
            className.Append(color switch
            {
                BitColor.Primary => " bit-tln-ipr",
                BitColor.Secondary => " bit-tln-ise",
                BitColor.Tertiary => " bit-tln-ite",
                BitColor.Info => " bit-tln-iin",
                BitColor.Success => " bit-tln-isu",
                BitColor.Warning => " bit-tln-iwr",
                BitColor.SevereWarning => " bit-tln-isw",
                BitColor.Error => " bit-tln-ier",
                _ => " bit-tln-ipr"
            });
        }

        var size = GetSize(item);
        if (size is not null)
        {
            className.Append(size switch
            {
                BitSize.Small => " bit-tln-ism",
                BitSize.Medium => " bit-tln-imd",
                BitSize.Large => " bit-tln-ilg",
                _ => " bit-tln-imd"
            });
        }

        var variant = GetVariant(item);
        if (variant is not null)
        {
            className.Append(variant switch
            {
                BitVariant.Fill => " bit-tln-ivf",
                BitVariant.Outline => " bit-tln-ivo",
                BitVariant.Text => " bit-tln-ivt",
                _ => " bit-tln-ivf"
            });
        }

        var @class = GetClass(item);
        if (@class is not null)
        {
            className.Append(' ').Append(@class);
        }

        if (GetIsEnabled(item) is false)
        {
            className.Append(" bit-tln-ids");
        }

        if (GetReversed(item))
        {
            className.Append(" bit-tln-irv");
        }

        if (isInteractive)
        {
            className.Append(" bit-tln-int");
        }

        return className.ToString();
    }

    // An item only takes part in the keyboard and pointer interactions when something actually listens
    // to its click, so that a purely presentational timeline is not exposed as a list of buttons.
    internal bool IsItemInteractive(TItem? item)
    {
        if (item is null) return false;

        if (OnItemClick.HasDelegate) return true;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.OnClick is not null;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.OnClick.HasDelegate;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.OnClick.Selector is not null)
        {
            return NameSelectors.OnClick.Selector!(item) is not null;
        }

        return item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name) is not null;
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        // The item renders as a div, where the disabled attribute is ineffective, so gate the handler
        // with the combined enabled state (timeline-level and item-level) to match the rendered state.
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        await OnItemClick.InvokeAsync(item);

        if (item is BitTimelineItem timelineItem)
        {
            timelineItem.OnClick?.Invoke(timelineItem);
        }
        else if (item is BitTimelineOption timelineOption)
        {
            await timelineOption.OnClick.InvokeAsync(timelineOption);
        }
        else
        {
            if (NameSelectors is null) return;

            if (NameSelectors.OnClick.Selector is not null)
            {
                NameSelectors.OnClick.Selector!(item)?.Invoke(item);
            }
            else
            {
                item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
            }
        }
    }

    internal string? GetAriaLabel(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.AriaLabel;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    private string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Class;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal BitIconInfo? GetIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Icon;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Icon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Icon.Selector is not null)
        {
            return NameSelectors.Icon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
    }

    internal string? GetIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.IconName;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    internal bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.IsEnabled;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    internal string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Style;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal RenderFragment<TItem>? GetPrimaryContent(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.PrimaryContent as RenderFragment<TItem>;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.PrimaryContent as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.PrimaryContent.Selector is not null)
        {
            return NameSelectors.PrimaryContent.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.PrimaryContent.Name);
    }

    internal RenderFragment<TItem>? GetSecondaryContent(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.SecondaryContent as RenderFragment<TItem>;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.SecondaryContent as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SecondaryContent.Selector is not null)
        {
            return NameSelectors.SecondaryContent.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.SecondaryContent.Name);
    }

    internal string? GetPrimaryText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.PrimaryText;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.PrimaryText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.PrimaryText.Selector is not null)
        {
            return NameSelectors.PrimaryText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.PrimaryText.Name);
    }

    internal string? GetSecondaryText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.SecondaryText;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.SecondaryText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SecondaryText.Selector is not null)
        {
            return NameSelectors.SecondaryText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.SecondaryText.Name);
    }

    internal bool GetHideDot(TItem? item)
    {
        if (item is null) return false;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.HideDot;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.HideDot;
        }

        // A custom item type with no name selectors exposes no HideDot value, so the dot is rendered
        // (the same default as the BitTimelineItem and the BitTimelineOption APIs).
        if (NameSelectors is null) return false;

        if (NameSelectors.HideDot.Selector is not null)
        {
            return NameSelectors.HideDot.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.HideDot.Name, false);
    }

    private bool GetReversed(TItem? item)
    {
        if (item is null) return false;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Reversed;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Reversed;
        }

        // A custom item type with no name selectors exposes no Reversed value, so the item keeps the
        // direction of the timeline itself.
        if (NameSelectors is null) return false;

        if (NameSelectors.Reversed.Selector is not null)
        {
            return NameSelectors.Reversed.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.Reversed.Name, false);
    }

    internal RenderFragment<TItem>? GetDotTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.DotTemplate as RenderFragment<TItem>;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.DotTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.DotTemplate.Selector is not null)
        {
            return NameSelectors.DotTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.DotTemplate.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Template as RenderFragment<TItem>;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    internal string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Title;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private BitSize? GetSize(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Size;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Size;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Size.Selector is not null)
        {
            return NameSelectors.Size.Selector!(item);
        }

        return item.GetValueFromProperty<BitSize?>(NameSelectors.Size.Name, null);
    }

    private BitColor? GetColor(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Color;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Color;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Color.Selector is not null)
        {
            return NameSelectors.Color.Selector!(item);
        }

        return item.GetValueFromProperty<BitColor?>(NameSelectors.Color.Name, null);
    }

    private BitVariant? GetVariant(TItem? item)
    {
        if (item is null) return null;

        if (item is BitTimelineItem timelineItem)
        {
            return timelineItem.Variant;
        }

        if (item is BitTimelineOption timelineOption)
        {
            return timelineOption.Variant;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Variant.Selector is not null)
        {
            return NameSelectors.Variant.Selector!(item);
        }

        return item.GetValueFromProperty<BitVariant?>(NameSelectors.Variant.Name, null);
    }
}
