using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDropDown
    {
        private string focusClass = "";
        private string expandClass = "";
        private bool isOpen = false;
        private bool isMultiSelect = false;
        private bool isRequired = false;
        private string? text;
        private List<string> selectedMultipleKeys = new();
        private string selectedKey = string.Empty;
        private bool SelectedMultipleKeysHasBeenSet;
        private bool SelectedKeyHasBeenSet;
        private bool IsSelectedMultipleKeysChanged = false;

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
        /// Input placeholder text, Displayed until an option is selected
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
        /// Shows the custom Label for drop down
        /// </summary>
        [Parameter] public RenderFragment? LabelFragment { get; set; }

        public string FocusClass
        {
            get => focusClass;
            set
            {
                focusClass = value;
                ClassBuilder.Reset();
            }
        }

        public string ExpandClass
        {
            get => expandClass;
            set
            {
                expandClass = value;
                ClassBuilder.Reset();
            }
        }

        public string DropDownId { get; set; } = String.Empty;
        public string? DropdownLabelId { get; set; } = String.Empty;
        public string DropDownOptionId { get; set; } = String.Empty;

        protected override string RootElementClass => "bit-drp";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => FocusClass.HasNoValue()
                ? string.Empty
                : $"{RootElementClass}-{FocusClass}-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => ExpandClass.HasNoValue()
                ? string.Empty
                : $"{RootElementClass}-{ExpandClass}-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => Items.Any(prop => prop.IsSelected)
                ? string.Empty
                : $"{RootElementClass}-{"hasValue"}-{VisualClassRegistrar()}");

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

            InitText();

            await base.OnParametersSetAsync();
        }

        internal void ChangeAllItemsIsSelected(bool value)
        {
            foreach (var item in Items)
            {
                item.IsSelected = value;
            }
        }

        private void CloseCallout()
        {
            IsOpen = false;
            FocusClass = "";
            StateHasChanged();
        }

        private async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (isOpen is false)
                {
                    isOpen = true;
                    FocusClass = "focused";
                }
                else
                {
                    isOpen = false;
                    FocusClass = "";
                }
                await OnClick.InvokeAsync(e);
            }
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
                    if (text.HasValue())
                    {
                        text += MultiSelectDelimiter;
                    }

                    text += selectedItem.Text;
                }
                else
                {
                    text = string.Empty;
                    foreach (var item in Items)
                    {
                        if (item.IsSelected)
                        {
                            if (text.HasValue())
                            {
                                text += MultiSelectDelimiter;
                            }

                            text += item.Text;
                        }
                    }
                }

                SelectedMultipleKeys = Items.FindAll(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal).Select(i => i.Value).ToList();
                await OnSelectItem.InvokeAsync(selectedItem);
            }
            else
            {
                var oldSelectedItem = Items.SingleOrDefault(i => i.IsSelected)!;
                var isSameItemSelected = oldSelectedItem == selectedItem;
                if (oldSelectedItem is not null) oldSelectedItem.IsSelected = false;
                selectedItem.IsSelected = true;
                text = selectedItem.Text;
                SelectedKey = selectedItem.Value;
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

                text = string.Empty;
                Items.ForEach(i =>
                {
                    if (i.IsSelected && i.ItemType == BitDropDownItemType.Normal)
                    {
                        if (text.HasValue())
                        {
                            text += MultiSelectDelimiter;
                        }

                        text += i.Text;
                    }
                });
            }
            else
            {
                if (SelectedKey.HasValue() && Items.Find(i => i.Value == SelectedKey && i.ItemType == BitDropDownItemType.Normal) is not null)
                {
                    Items.Find(i => i.Value == SelectedKey)!.IsSelected = true;
                    Items.FindAll(i => i.Value != SelectedKey).ForEach(i => { i.IsSelected = false; });
                    text = Items.Find(i => i.Value == SelectedKey)!.Text;
                }
                else if (DefaultSelectedKey.HasValue() && Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal) is not null)
                {
                    Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal)!.IsSelected = true;
                    Items.FindAll(i => i.Value != DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal).ForEach(i => { i.IsSelected = false; });
                    text = Items.Find(i => i.Value == DefaultSelectedKey && i.ItemType == BitDropDownItemType.Normal)!.Text;
                }
                else if (Items.FindAll(item => item.IsSelected is true && item.ItemType == BitDropDownItemType.Normal).Count != 0)
                {
                    var firstSelectedItem = Items.Find(i => i.IsSelected && i.ItemType == BitDropDownItemType.Normal)!;
                    text = firstSelectedItem.Text;
                    Items.FindAll(i => i.Value != firstSelectedItem.Value).ForEach(i => { i.IsSelected = false; });
                }
            }
        }

        private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{DropDownId}-label {DropDownId}-option" : $"{DropDownId}-option";
    }
}
