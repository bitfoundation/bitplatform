using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// The Timeline component organizes and displays events or data chronologically in a linear fashion, often featuring points or segments representing individual items with associated details or actions.
/// </summary>
public partial class BitTimeline<TItem> : BitComponentBase where TItem : class
{
    private List<TItem> _items = [];
    private IEnumerable<TItem> _oldItems = default!;



    /// <summary>
    /// The EditContext, which is set if the timeline is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



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
    /// Defines whether to render timeline children horizontally.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Horizontal { get; set; }

    /// <summary>
    ///  List of Item, each can be with different contents in the timeline.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

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
    /// Reverses all of the timeline items direction.
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
        if (ChildContent is null && Items.Any() && Items != _oldItems)
        {
            _oldItems = Items;
            _items = Items.ToList();
        }

        return base.OnParametersSetAsync();
    }



    private string? GetItemClasses(TItem item)
    {
        StringBuilder className = new StringBuilder();

        if (GetColor(item) is not null)
        {
            className.Append(GetColor(item) switch
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

        if (GetSize(item) is not null)
        {
            className.Append(GetSize(item) switch
            {
                BitSize.Small => " bit-tln-ism",
                BitSize.Medium => " bit-tln-imd",
                BitSize.Large => " bit-tln-ilg",
                _ => " bit-tln-imd"
            });
        }

        if (GetClass(item) is not null)
        {
            className.Append(' ').Append(GetClass(item));
        }

        if (GetIsEnabled(item) is false)
        {
            className.Append(" bit-tln-ids");
        }

        if (GetReversed(item))
        {
            className.Append(" bit-tln-irv");
        }

        return className.ToString();
    }

    private async Task HandleOnItemClick(TItem item)
    {
        if (GetIsEnabled(item) is false) return;

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

    private string? GetIconName(TItem? item)
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

    private bool GetIsEnabled(TItem? item)
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

    private string? GetStyle(TItem? item)
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

    private RenderFragment<TItem>? GetPrimaryContent(TItem? item)
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

    private RenderFragment<TItem>? GetSecondaryContent(TItem? item)
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

    private string? GetPrimaryText(TItem? item)
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

    private string? GetSecondaryText(TItem? item)
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

    private bool GetHideDot(TItem? item)
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

        if (NameSelectors is null) return true;

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

        if (NameSelectors is null) return true;

        if (NameSelectors.Reversed.Selector is not null)
        {
            return NameSelectors.Reversed.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.Reversed.Name, false);
    }

    private RenderFragment<TItem>? GetDotTemplate(TItem? item)
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
}
