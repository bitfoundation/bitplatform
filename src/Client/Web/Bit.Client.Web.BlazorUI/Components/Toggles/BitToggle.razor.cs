using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggle
    {
        private bool isChecked;

        [Parameter] public string OnText { get; set; }
        [Parameter] public string OffText { get; set; }
        [Parameter] public bool IsInlineLabel { get; set; }

        [Parameter]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked) return;
                isChecked = value;
                ClassBuilder.Reset();
                _ = IsCheckedChange.InvokeAsync();
            }
        }

        [Parameter] public EventCallback<bool> IsCheckedChange { get; set; }


        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-tgl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() =>
            {
                var isCheckedClass = IsChecked ? "checked" : "unchecked";
                var isEnabledClass = IsEnabled ? "enabled" : "disabled";
                return $"{RootElementClass}-{isEnabledClass}-{isCheckedClass}-{VisualClassRegistrar()}";
            });

            ClassBuilder.Register(() => IsInlineLabel ? $"{RootElementClass}-inline-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() =>
            string.IsNullOrWhiteSpace(OnText) || string.IsNullOrWhiteSpace(OffText) ?
                $"{RootElementClass}-without-onoff-{VisualClassRegistrar()}" :
                string.Empty);
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                IsChecked = !IsChecked;
                await OnClick.InvokeAsync(e);
            }
        }
    }
}
