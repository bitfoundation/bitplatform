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
        private string focusClass = "";
        private string expandClass = "";
        private bool isOpen = false;
        private bool isMultiSelect = false;
        private bool isRequired = false;
        private string? text;

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
        [Parameter] public List<DropDownItem> Items { get; set; } = new List<DropDownItem>();

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
        [Parameter] public EventCallback<DropDownItem> OnSelectItem { get; set; }

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
            DropdownLabelId = Label.HasValue() ? $"{DropDownId}-label" : null;
            DropDownOptionId = $"{DropDownId}-option";
            await base.OnParametersSetAsync();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsEnabled && firstRender)
            {
                _ = JSRuntime?.RegisterOnDocumentClickEvent(this, "CloseCallout");
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        internal void ChangeAllItemsIsSelected(bool value)
        {
            foreach (var item in Items)
            {
                item.IsSelected = value;
            }
        }

        [JSInvokable]
        public void CloseCallout()
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

        private async Task HandleItemClick(DropDownItem? selectedItem)
        {
            if (selectedItem is not null)
            {
                if (selectedItem.IsEnabled)
                {
                    if (IsMultiSelect)
                    {
                        if (text.HasValue())
                        {
                            text += MultiSelectDelimiter;
                        }

                        text += selectedItem.Text;
                    }
                    else
                    {
                        ChangeAllItemsIsSelected(false);
                        text = selectedItem.Text;
                        selectedItem.IsSelected = true;
                        isOpen = false;
                    }

                    await OnSelectItem.InvokeAsync(selectedItem);
                }
            }
            else
            {
                if (IsMultiSelect)
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
            }
        }

        private string GetDropdownAriaLabelledby => Label.HasValue() ? $"{DropDownId}-label {DropDownId}-option" : $"{DropDownId}-option";
    }
}
