using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// The ButtonGroup component can be used to group related buttons.
/// </summary>
public partial class BitButtonGroup<TItem> : BitComponentBase where TItem : class
{
    private TItem? _toggleItem;
    private List<TItem> _items = [];
    private IEnumerable<TItem> _oldItems = default!;


    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



    /// <summary>
    /// The content of the BitButtonGroup, that are BitButtonGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the ButtonGroup.
    /// </summary>
    [Parameter] public BitButtonGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// Defines the general colors available in the bit BlazorUI.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Expand the ButtonGroup width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered.
    /// </summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>
    ///  List of Item, each of which can be a button with different action in the ButtonGroup.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitButtonGroupNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback that is called when a button is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// The size of ButtonGroup, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the ButtonGroup.
    /// </summary>
    [Parameter] public BitButtonGroupClassStyles? Styles { get; set; }

    /// <summary>
    /// Display ButtonGroup with toggle mode enabled for each button.
    /// </summary>
    [Parameter] public bool Toggle { get; set; }

    /// <summary>
    /// The visual variant of the button group.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render ButtonGroup children vertically.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    internal void RegisterOption(BitButtonGroupOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        StateHasChanged();
    }

    internal void UnregisterOption(BitButtonGroupOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-btg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-btg-fil",
            BitVariant.Outline => "bit-btg-otl",
            BitVariant.Text => "bit-btg-txt",
            _ => "bit-btg-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-btg-pri",
            BitColor.Secondary => "bit-btg-sec",
            BitColor.Tertiary => "bit-btg-ter",
            BitColor.Info => "bit-btg-inf",
            BitColor.Success => "bit-btg-suc",
            BitColor.Warning => "bit-btg-wrn",
            BitColor.SevereWarning => "bit-btg-swr",
            BitColor.Error => "bit-btg-err",
            BitColor.PrimaryBackground => "bit-btg-pbg",
            BitColor.SecondaryBackground => "bit-btg-sbg",
            BitColor.TertiaryBackground => "bit-btg-tbg",
            BitColor.PrimaryForeground => "bit-btg-pfg",
            BitColor.SecondaryForeground => "bit-btg-sfg",
            BitColor.TertiaryForeground => "bit-btg-tfg",
            BitColor.PrimaryBorder => "bit-btg-pbr",
            BitColor.SecondaryBorder => "bit-btg-sbr",
            BitColor.TertiaryBorder => "bit-btg-tbr",
            _ => "bit-btg-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btg-sm",
            BitSize.Medium => "bit-btg-md",
            BitSize.Large => "bit-btg-lg",
            _ => "bit-btg-md"
        });

        ClassBuilder.Register(() => Vertical ? "bit-btg-vrt" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-btg-flw" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (ChildContent is not null || Items is null || Items.Any() is false) return;

        if (_oldItems is not null && Items.SequenceEqual(_oldItems)) return;

        _oldItems = Items;
        _items = [.. Items];
    }



    private async Task HandleOnItemClick(TItem item)
    {
        if (GetIsEnabled(item) is false) return;

        await OnItemClick.InvokeAsync(item);

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            buttonGroupItem.OnClick?.Invoke(buttonGroupItem);
        }
        else if (item is BitButtonGroupOption buttonGroupOption)
        {
            await buttonGroupOption.OnClick.InvokeAsync(buttonGroupOption);
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

        if (Toggle)
        {
            if (_toggleItem == item)
            {
                _toggleItem = null;
            }
            else
            {
                _toggleItem = item;
            }
        }
    }

    private string? GetItemClass(TItem? item)
    {
        List<string> classes = ["bit-btg-itm"];

        if (GetReversedIcon(item))
        {
            classes.Add("bit-btg-rvi");
        }

        if (_toggleItem == item)
        {
            classes.Add("bit-btg-chk");

            if (Classes?.ToggledButton.HasValue() ?? false)
            {
                classes.Add(Classes.ToggledButton!);
            }
        }

        var classItem = GetClass(item);
        if (classItem.HasValue())
        {
            classes.Add(classItem!);
        }

        if (Classes?.Button.HasValue() ?? false)
        {
            classes.Add(Classes.Button!);
        }

        return string.Join(' ', classes);
    }

    private string? GetItemStyle(TItem? item)
    {
        List<string> styles = new();

        var style = GetStyle(item);
        if (style.HasValue())
        {
            styles.Add(style!.Trim(';'));
        }

        if (Styles?.Button.HasValue() ?? false)
        {
            styles.Add(Styles.Button!.Trim(';'));
        }

        if (_toggleItem == item && (Styles?.ToggledButton.HasValue() ?? false))
        {
            styles.Add(Styles.ToggledButton!);
        }

        return string.Join(';', styles);
    }

    private string? GetItemText(TItem? item)
    {
        if (IconOnly) return null;

        if (Toggle)
        {
            if (_toggleItem == item)
            {
                var onText = GetOnText(item);
                if (onText.HasValue())
                {
                    return onText;
                }
            }
            else
            {
                var offText = GetOffText(item);
                if (offText.HasValue())
                {
                    return offText;
                }
            }
        }

        return GetText(item);
    }

    private string? GetItemTitle(TItem? item)
    {
        if (Toggle)
        {
            if (_toggleItem == item)
            {
                var onTitle = GetOnTitle(item);
                if (onTitle.HasValue())
                {
                    return onTitle;
                }
            }
            else
            {
                var offTitle = GetOffTitle(item);
                if (offTitle.HasValue())
                {
                    return offTitle;
                }
            }
        }

        return GetTitle(item);
    }

    private string? GetItemIconName(TItem? item)
    {
        if (Toggle)
        {
            if (_toggleItem == item)
            {
                var onIconName = GetOnIconName(item);
                if (onIconName.HasValue())
                {
                    return onIconName;
                }
            }
            else
            {
                var offIconName = GetOffIconName(item);
                if (offIconName.HasValue())
                {
                    return offIconName;
                }
            }
        }

        return GetIconName(item);
    }

    private string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Class;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Class;
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

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.IconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private string? GetOnIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnIconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnIconName.Selector is not null)
        {
            return NameSelectors.OnIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnIconName.Name);
    }

    private string? GetOffIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffIconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffIconName.Selector is not null)
        {
            return NameSelectors.OffIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffIconName.Name);
    }

    private bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.IsEnabled;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.IsEnabled;
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

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Style;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    private RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Template as RenderFragment<TItem>;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    private string? GetText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Text;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    private string? GetOnText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnText;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnText.Selector is not null)
        {
            return NameSelectors.OnText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnText.Name);
    }

    private string? GetOffText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffText;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffText.Selector is not null)
        {
            return NameSelectors.OffText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffText.Name);
    }

    private string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Title;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private string? GetOnTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnTitle;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnTitle;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnTitle.Selector is not null)
        {
            return NameSelectors.OnTitle.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnTitle.Name);
    }

    private string? GetOffTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffTitle;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffTitle;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffTitle.Selector is not null)
        {
            return NameSelectors.OffTitle.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffTitle.Name);
    }

    private bool GetReversedIcon(TItem? item)
    {
        if (item is null) return false;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.ReversedIcon;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.ReversedIcon;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.ReversedIcon.Selector is not null)
        {
            return NameSelectors.ReversedIcon.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.ReversedIcon.Name, false);
    }
}
