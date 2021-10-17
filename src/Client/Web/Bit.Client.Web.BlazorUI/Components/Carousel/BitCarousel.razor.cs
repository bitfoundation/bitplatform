﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCarousel
    {
        private BitCarouselItem? SelectedItem;
        private List<BitCarouselItem> AllCarouselItems = new();
        private string? selectedKey;
        private bool SelectedKeyHasBeenSet;

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public string? SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;

                SelectItemByKey(value);
            }
        }

        [Parameter] public EventCallback<string?> SelectedKeyChanged { get; set; }

        protected override string RootElementClass => "bit-crsl";

        internal void RegisterItem(BitCarouselItem carouselItem)
        {
            if (IsEnabled is false)
            {
                carouselItem.IsEnabled = false;
            }

            AllCarouselItems.Add(carouselItem);

            if (AllCarouselItems.Count == 1 || SelectedKey.HasValue() && SelectedKey == carouselItem.Key)
            {
                carouselItem.SetState(true);
                SelectedItem = carouselItem;
            }
            StateHasChanged();
        }

        internal void UnregisterItem(BitCarouselItem carouselItem)
        {
            AllCarouselItems.Remove(carouselItem);
        }

        internal async Task SelectItem(BitCarouselItem item)
        {
            if (SelectedKeyHasBeenSet && SelectedKeyChanged.HasDelegate is false) return;

            SelectedItem?.SetState(false);
            item.SetState(true);

            SelectedItem = item;
            selectedKey = item.Key;

            await SelectedKeyChanged.InvokeAsync(selectedKey);
        }

        private void SelectItemByKey(string? key)
        {
            if (key.HasNoValue()) return;

            var newItem = AllCarouselItems.FirstOrDefault(i => i.Key == key);

            if (newItem == null || newItem == SelectedItem || newItem.IsEnabled is false)
            {
                _ = SelectedKeyChanged.InvokeAsync(selectedKey);
                return;
            }

            _ = SelectItem(newItem);
        }

        private void SelectItemByIndex(int index)
        {
            var newItem = AllCarouselItems.ElementAt(index);

            if (newItem == null || newItem == SelectedItem || newItem.IsEnabled is false)
            {
                return;
            }

            _ = SelectItem(newItem);
        }
    }
}
