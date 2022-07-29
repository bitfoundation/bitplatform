using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

public partial class BitDropDown
{
    private bool isOpen;
    private bool isResponsiveModeEnabled;
    private bool isMultiSelect;
    private bool isRequired;
    private List<string> values = new();
    private bool ValuesHasBeenSet;
    private bool isValuesChanged;
    private List<BitDropDownItem> NormalDropDownItems = new();

    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Whether multiple items are allowed to be selected
    /// </summary>
    [Parameter]
    public bool IsMultiSelect
    {
        get => isMultiSelect;
        set
        {
            isMultiSelect = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether or not this dropdown is open
    /// </summary>
    [Parameter]
    public bool IsOpen
    {
        get => isOpen;
        set
        {
            isOpen = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Whether the drop down items get rendered in a side panel in small screen sizes or not 
    /// </summary>
    [Parameter]
    public bool IsResponsiveModeEnabled
    {
        get => isResponsiveModeEnabled;
        set
        {
            isResponsiveModeEnabled = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// Requires the end user to select an item in the dropdown.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// A list of items to display in the dropdown
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public List<BitDropDownItem> Items { get; set; } = new();
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists

    /// <summary>
    /// Keys of the selected items for multiSelect scenarios
    /// If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed
    /// </summary>
    [Parameter]
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    public List<string> Values
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists
    {
        get => values;
        set
        {
            if (value == null) return;
            if (values.All(value.Contains) && values.Count == value.Count) return;
            values = value;
            _ = ValuesChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<List<string>> ValuesChanged { get; set; }

    /// <summary>
    /// Gets or sets an expression that identifies the bound values.
    /// </summary>
    [Parameter] public Expression<Func<List<string>>>? ValuesExpression { get; set; }

    /// <summary>
    /// Keys that will be initially used to set selected items for multiSelect scenarios
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public List<string> DefaultValues { get; set; } = new();
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists

    /// <summary>
    /// Key that will be initially used to set selected item
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// Input placeholder Text, Displayed until an option is selected
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// the label associated with the dropdown
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The title to show when the mouse is placed on the drop down
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// When multiple items are selected, this still will be used to separate values in the dropdown title
    /// </summary>
    [Parameter] public string MultiSelectDelimiter { get; set; } = ", ";

    /// <summary>
    /// Callback for when the dropdown clicked
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Callback for when an item is selected
    /// </summary>
    [Parameter] public EventCallback<BitDropDownItem> OnSelectItem { get; set; }

    /// <summary>
    /// Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode
    /// </summary>
    [Parameter] public bool NotifyOnReselect { get; set; }

    /// <summary>
    /// Optional custom template for label
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    /// <summary>
    /// Optional custom template for selected option displayed in after selection
    /// </summary>
    [Parameter] public RenderFragment<BitDropDown>? TextTemplate { get; set; }

    /// <summary>
    /// Optional custom template for placeholder Text
    /// </summary>
    [Parameter] public RenderFragment<BitDropDown>? PlaceholderTemplate { get; set; }

    /// <summary>
    /// Optional custom template for chevron icon
    /// </summary>
    [Parameter] public RenderFragment? CaretDownFragment { get; set; }

    /// <summary>
    /// Optional custom template for drop-down item
    /// </summary>
    [Parameter] public RenderFragment<BitDropDownItem>? ItemTemplate { get; set; }

    public string? Text { get; set; }

    public string DropDownId { get; set; } = string.Empty;
    public string? DropdownLabelId { get; set; } = string.Empty;
    public string DropDownOptionId { get; set; } = string.Empty;
    public string DropDownCalloutId { get; set; } = string.Empty;
    public string DropDownOverlayId { get; set; } = string.Empty;


    [JSInvokable("CloseCallout")]
    public void CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        IsOpen = false;
    }

    protected override string RootElementClass => "bit-drp";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => Items.Any(prop => prop.IsSelected)
            ? string.Empty
            : $"{RootElementClass}-{"has-value"}-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => IsOpen is false
            ? string.Empty
            : $"{RootElementClass}-{"opened"}-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => IsResponsiveModeEnabled is false
            ? string.Empty
            : $"{RootElementClass}-{"responsive"}-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => IsMultiSelect is false
            ? string.Empty
            : $"{RootElementClass}-{"multi"}-{VisualClassRegistrar()}");

        ClassBuilder.Register(() => ValueInvalid is true
            ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}"
            : string.Empty);
    }

    protected override async Task OnParametersSetAsync()
    {
        DropDownId = $"Dropdown{UniqueId}";
        DropDownOptionId = $"{DropDownId}-option";
        DropdownLabelId = Label.HasValue() ? $"{DropDownId}-label" : null;
        DropDownOverlayId = $"{DropDownId}-overlay";
        DropDownCalloutId = $"{DropDownId}-list";

        NormalDropDownItems = Items.FindAll(i => i.ItemType == BitDropDownItemType.Normal).ToList();
        InitText();

        await base.OnParametersSetAsync();
    }

    private void ChangeAllItemsIsSelected(bool value)
    {
        foreach (var item in Items)
        {
            item.IsSelected = value;
        }
    }

    private async Task CloseCallout()
    {
        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen, isResponsiveModeEnabled);
        IsOpen = false;
        StateHasChanged();
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        var obj = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen, isResponsiveModeEnabled);
        isOpen = !isOpen;
        await OnClick.InvokeAsync(e);
    }

    private async Task HandleItemClick(BitDropDownItem selectedItem)
    {
        if (IsEnabled is false || selectedItem.IsEnabled is false) return;

        if (isMultiSelect &&
                ValuesHasBeenSet &&
                ValuesChanged.HasDelegate is false) return;

        if (isMultiSelect is false &&
            ValueHasBeenSet &&
            ValueChanged.HasDelegate is false) return;

        if (isMultiSelect)
        {
            if (isValuesChanged is false) isValuesChanged = true;

            selectedItem.IsSelected = !selectedItem.IsSelected;
            if (selectedItem.IsSelected)
            {
                if (Text.HasValue())
                {
                    Text += MultiSelectDelimiter;
                }

                Text += selectedItem.Text;
            }
            else
            {
                Text = string.Empty;
                foreach (var item in Items)
                {
                    if (item.IsSelected)
                    {
                        if (Text.HasValue())
                        {
                            Text += MultiSelectDelimiter;
                        }

                        Text += item.Text;
                    }
                }
            }

            Values = Items.FindAll(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal).Select(i => i.Value).ToList();
            await OnSelectItem.InvokeAsync(selectedItem);
        }
        else
        {
            var oldSelectedItem = Items.SingleOrDefault(i => i.IsSelected)!;
            var isSameItemSelected = oldSelectedItem == selectedItem;
            if (oldSelectedItem is not null) oldSelectedItem.IsSelected = false;
            selectedItem.IsSelected = true;
            Text = selectedItem.Text;
            CurrentValueAsString = selectedItem.Value;
            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen, isResponsiveModeEnabled);
            isOpen = false;

            if (isSameItemSelected && !NotifyOnReselect) return;

            await OnSelectItem.InvokeAsync(selectedItem);
        }
    }

    private void InitText()
    {
        if (isMultiSelect)
        {
            if (ValuesHasBeenSet || isValuesChanged)
            {
                ChangeAllItemsIsSelected(false);
                Items.FindAll(i => Values.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = true; });
            }
            else if (DefaultValues.Count != 0)
            {
                ChangeAllItemsIsSelected(false);
                Items.FindAll(i => DefaultValues.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = true; });
            }

            Text = string.Empty;
            Items.ForEach(i =>
            {
                if (i.IsSelected && i.ItemType == BitDropDownItemType.Normal)
                {
                    if (Text.HasValue())
                    {
                        Text += MultiSelectDelimiter;
                    }

                    Text += i.Text;
                }
            });
        }
        else
        {
            if (CurrentValue.HasValue() && Items.Find(i => i.Value == CurrentValue && i.ItemType == BitDropDownItemType.Normal) is not null)
            {
                Items.Find(i => i.Value == CurrentValue)!.IsSelected = true;
                Items.FindAll(i => i.Value != CurrentValue).ForEach(i => i.IsSelected = false);
                Text = Items.Find(i => i.Value == CurrentValue)!.Text;
            }
            else if (DefaultValue.HasValue() && Items.Any(i => i.Value == DefaultValue && i.ItemType == BitDropDownItemType.Normal))
            {
                Items.Find(i => i.Value == DefaultValue && i.ItemType == BitDropDownItemType.Normal)!.IsSelected = true;
                Items.FindAll(i => i.Value != DefaultValue && i.ItemType == BitDropDownItemType.Normal).ForEach(i => i.IsSelected = false);
                Text = Items.Find(i => i.Value == DefaultValue && i.ItemType == BitDropDownItemType.Normal)!.Text;
            }
            else if (Items.FindAll(item => item.IsSelected is true && item.ItemType == BitDropDownItemType.Normal).Count != 0)
            {
                var firstSelectedItem = Items.Find(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal)!;
                Text = firstSelectedItem.Text;
                Items.FindAll(i => i.Value != firstSelectedItem.Value).ForEach(i => i.IsSelected = false);
            }
        }
    }

    private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{DropDownId}-label {DropDownId}-option" : $"{DropDownId}-option";
    private int GetItemPosInSet(BitDropDownItem item) => NormalDropDownItems.IndexOf(item) + 1;

    private string GetCssClassForItem(BitDropDownItem item)
    {
        StringBuilder stringBuilder = new StringBuilder("bit-drp-chb");
        stringBuilder.Append(' ').Append("bit-drp-chb-").Append(VisualClassRegistrar());

        if (item.IsSelected)
        {
            stringBuilder
                .Append(' ').Append(RootElementClass).Append("-slc-").Append(VisualClassRegistrar())
                .Append(' ').Append("bit-drp-chb-checked-").Append(VisualClassRegistrar());
        }

        if (item.IsEnabled is false && item.IsSelected)
        {
            stringBuilder
                .Append(' ').Append(RootElementClass).Append("-slc-").Append(VisualClassRegistrar())
                .Append(' ').Append("bit-drp-chb-checked-disabled-").Append(VisualClassRegistrar());
        }

        stringBuilder
            .Append(' ').Append("bit-drp-chb-")
            .Append(item.IsEnabled ? "enabled" : "disabled")
            .Append('-').Append(VisualClassRegistrar());

        return stringBuilder.ToString();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void RegisterFieldIdentifier()
    {
        if (IsMultiSelect)
        {
            RegisterFieldIdentifier(ValuesExpression, typeof(List<string>));
        }
        else
        {
            base.RegisterFieldIdentifier();
        }
    }
}
