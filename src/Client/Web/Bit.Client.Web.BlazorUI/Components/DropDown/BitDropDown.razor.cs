﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDropDown
    {
        private bool isOpen = false;
        private bool isMultiSelect = false;
        private bool isRequired = false;
        private List<string> selectedMultipleKeys = new();
        private string selectedKey = string.Empty;
        private bool SelectedMultipleKeysHasBeenSet;
        private bool SelectedKeyHasBeenSet;
        private bool IsSelectedMultipleKeysChanged = false;
        private List<BitDropDownItem> NormalDropDownItems = new List<BitDropDownItem>();

        [Inject] public IJSRuntime? JSRuntime { get; set; }

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
        [Parameter] public List<BitDropDownItem> Items { get; set; } = new List<BitDropDownItem>();

        /// <summary>
        /// Keys of the selected items for multiSelect scenarios
        /// If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed
        /// </summary>
        [Parameter]
        public List<string> SelectedMultipleKeys
        {
            get => selectedMultipleKeys;
            set
            {
                if (selectedMultipleKeys.All(value.Contains) && selectedMultipleKeys.Count == value.Count) return;
                selectedMultipleKeys = value;
                _ = SelectedMultipleKeysChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<List<string>> SelectedMultipleKeysChanged { get; set; }

        /// <summary>
        /// Key of the selected item
        /// If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed
        /// </summary>
        [Parameter]
        public string SelectedKey
        {
            get => selectedKey;
            set
            {
                if (selectedKey == value) return;
                selectedKey = value;
                _ = SelectedKeyChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

        /// <summary>
        /// Keys that will be initially used to set selected items for multiSelect scenarios
        /// </summary>
        [Parameter] public List<string> DefaultSelectedMultipleKeys { get; set; } = new List<string>();

        /// <summary>
        /// Key that will be initially used to set selected item
        /// </summary>
        [Parameter] public string? DefaultSelectedKey { get; set; }

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
        [Parameter] public bool NotifyOnReselect { get; set; } = false;

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

        public string DropDownId { get; set; } = String.Empty;
        public string? DropdownLabelId { get; set; } = String.Empty;
        public string DropDownOptionId { get; set; } = String.Empty;
        public string DropDownCalloutId { get; set; } = string.Empty;
        public string DropDownOverlayId { get; set; } = String.Empty;


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

            ClassBuilder.Register(() => IsMultiSelect is false
                ? string.Empty
                : $"{RootElementClass}-{"multi"}-{VisualClassRegistrar()}");
        }

        protected async override Task OnParametersSetAsync()
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
            if (JSRuntime is null) return;

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen);
            IsOpen = false;
            StateHasChanged();
        }

        private async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled is false || JSRuntime is null) return;

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen);
            isOpen = !isOpen;
            await OnClick.InvokeAsync(e);
        }

        private async Task HandleItemClick(BitDropDownItem selectedItem)
        {
            if (!IsEnabled || !selectedItem.IsEnabled) return;

            if (isMultiSelect &&
                    SelectedMultipleKeysHasBeenSet &&
                    SelectedMultipleKeysChanged.HasDelegate is false) return;

            if (!isMultiSelect &&
                SelectedKeyHasBeenSet &&
                SelectedKeyChanged.HasDelegate is false) return;

            if (isMultiSelect)
            {
                if (IsSelectedMultipleKeysChanged is false) IsSelectedMultipleKeysChanged = true;

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

                SelectedMultipleKeys = Items.FindAll(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal).Select(i => i.Value).ToList();
                await OnSelectItem.InvokeAsync(selectedItem);
            }
            else
            {
                if (JSRuntime is null) return;

                var oldSelectedItem = Items.SingleOrDefault(i => i.IsSelected)!;
                var isSameItemSelected = oldSelectedItem == selectedItem;
                if (oldSelectedItem is not null) oldSelectedItem.IsSelected = false;
                selectedItem.IsSelected = true;
                Text = selectedItem.Text;
                SelectedKey = selectedItem.Value;
                var obj = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("BitDropDown.toggleDropDownCallout", obj, UniqueId, DropDownId, DropDownCalloutId, DropDownOverlayId, isOpen);
                isOpen = false;

                if (isSameItemSelected && !NotifyOnReselect) return;

                await OnSelectItem.InvokeAsync(selectedItem);
            }
        }

        private void InitText()
        {
            if (isMultiSelect)
            {
                if (SelectedMultipleKeysHasBeenSet || IsSelectedMultipleKeysChanged)
                {
                    ChangeAllItemsIsSelected(false);
                    Items.FindAll(i => SelectedMultipleKeys.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = true; });
                }
                else if (DefaultSelectedMultipleKeys.Count != 0)
                {
                    ChangeAllItemsIsSelected(false);
                    Items.FindAll(i => DefaultSelectedMultipleKeys.Contains(i.Value) && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = true; });
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
                if (SelectedKey.HasValue() && Items.Find(i => i.Value == SelectedKey && i.ItemType == BitDropDownItemType.Normal) is not null)
                {
                    Items.Find(i => i.Value == SelectedKey)!.IsSelected = true;
                    Items.FindAll(i => i.Value != SelectedKey).ForEach(i => { i.IsSelected = false; });
                    Text = Items.Find(i => i.Value == SelectedKey)!.Text;
                }
                else if (DefaultSelectedKey.HasValue() && Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal) is not null)
                {
                    Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal)!.IsSelected = true;
                    Items.FindAll(i => i.Value != DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = false; });
                    Text = Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal)!.Text;
                }
                else if (Items.FindAll(item => item.IsSelected is true && item.ItemType == BitDropDownItemType.Normal).Count != 0)
                {
                    var firstSelectedItem = Items.Find(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal)!;
                    Text = firstSelectedItem.Text;
                    Items.FindAll(i => i.Value != firstSelectedItem.Value).ForEach(i => { i.IsSelected = false; });
                }
            }
        }

        private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{DropDownId}-label {DropDownId}-option" : $"{DropDownId}-option";
        private int GetItemPosInSet(BitDropDownItem item) => NormalDropDownItems.IndexOf(item) + 1;
    }
}
