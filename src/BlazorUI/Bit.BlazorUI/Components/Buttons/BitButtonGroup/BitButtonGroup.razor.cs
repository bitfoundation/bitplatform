using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitButtonGroup<TItem> : BitComponentBase where TItem : class
{
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
    /// Defines the general colors available in the bit BlazorUI.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

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
    /// The visual variant of the button group.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render ButtonGroup children vertically.
    /// </summary>
    [Parameter] public bool Vertical { get; set; }



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
            _ => "bit-btg-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btg-sm",
            BitSize.Medium => "bit-btg-md",
            BitSize.Large => "bit-btg-lg",
            _ => "bit-btg-md"
        });

        ClassBuilder.Register(() => Vertical ? "bit-btg-vrt" : "");
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
}
