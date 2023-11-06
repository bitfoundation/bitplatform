using System.Text;
using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

public partial class BitButtonGroup<TItem> where TItem : class
{
    private bool vertical;
    private BitButtonSize? size;
    private BitButtonColor? color;
    private BitButtonStyle buttonStyle = BitButtonStyle.Primary;

    private List<TItem> _items = new();
    private IEnumerable<TItem> _oldItems = default!;



    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



    /// <summary>
    /// The style of button, Possible values: Primary | Standard
    /// </summary>
    [Parameter]
    public BitButtonStyle ButtonStyle
    {
        get => buttonStyle;
        set
        {
            buttonStyle = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The content of the BitButtonGroup, that are BitButtonGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The color of button
    /// </summary>
    [Parameter]
    public BitButtonColor? Color
    {
        get => color;
        set
        {
            if (color == value) return;
            color = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    ///  List of Item, each of which can be a button with different action in the ButtonGroup.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Defines whether to render ButtonGroup children vertically.
    /// </summary>
    [Parameter]
    public bool Vertical
    {
        get => vertical;
        set
        {
            if (vertical == value) return;

            vertical = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitButtonGroupNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback is called when the button or button item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnClick { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// The size of button, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter]
    public BitButtonSize? Size
    {
        get => size;
        set
        {
            if (size == value) return;
            size = value;
            ClassBuilder.Reset();
        }
    }


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
        ClassBuilder.Register(() => ButtonStyle switch
        {
            BitButtonStyle.Primary => $" {RootElementClass}-pri",
            BitButtonStyle.Standard => $" {RootElementClass}-std",
            BitButtonStyle.Text => $" {RootElementClass}-txt",
            _ => $" {RootElementClass}-pri"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitButtonColor.Info => $" {RootElementClass}-inf",
            BitButtonColor.Success => $" {RootElementClass}-suc",
            BitButtonColor.Warning => $" {RootElementClass}-wrn",
            BitButtonColor.SevereWarning => $" {RootElementClass}-swr",
            BitButtonColor.Error => $" {RootElementClass}-err",
            _ => string.Empty
        });

        ClassBuilder.Register(() => Vertical ? $"{RootElementClass}-vrt" : "");
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

    private string? GetButtonClass(int index)
    {
        StringBuilder className = new StringBuilder();

        className.Append(ButtonStyle switch
        {
            BitButtonStyle.Primary => $" {RootElementClass}-ipr",
            BitButtonStyle.Standard => $" {RootElementClass}-ist",
            BitButtonStyle.Text => $" {RootElementClass}-itx",
            _ => $" {RootElementClass}-ipr"
        });

        className.Append(Color switch
        {
            BitButtonColor.Info => $" {RootElementClass}-iif",
            BitButtonColor.Success => $" {RootElementClass}-isc",
            BitButtonColor.Warning => $" {RootElementClass}-iwn",
            BitButtonColor.SevereWarning => $" {RootElementClass}-isr",
            BitButtonColor.Error => $" {RootElementClass}-ier",
            _ => string.Empty
        });

        className.Append(Size switch
        {
            BitButtonSize.Small => $" {RootElementClass}-sm",
            BitButtonSize.Medium => $" {RootElementClass}-md",
            BitButtonSize.Large => $" {RootElementClass}-lg",
            _ => string.Empty
        });

        if (index == 0)
        {
            className.Append($" {RootElementClass}-ift");
        }

        if (index == (_items.Count - 1))
        {
            className.Append($" {RootElementClass}-ilt");
        }

        return className.ToString();
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

    private async Task HandleOnItemClick(TItem item)
    {
        if (GetIsEnabled(item) is false) return;

        await OnClick.InvokeAsync(item);

        await InvokeItemClick(item);
    }

    private async Task InvokeItemClick(TItem item)
    {
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
}
