using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggle
    {
        private bool isChecked;
        private bool isInlineLable;

        [Parameter] public string OnText { get; set; }
        [Parameter] public string OffText { get; set; }

        [Parameter]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsInlineLable
        {
            get => isInlineLable;
            set
            {
                isInlineLable = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        protected override string RootElementClass => "bit-tgl";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() =>
            {
                string cssClass = string.Empty;
                if (IsEnabled)
                {
                    if (isChecked)
                        cssClass = $"{RootElementClass}-checked-{VisualClassRegistrar()}";
                    else
                        cssClass = $"{RootElementClass}-unchecked-{VisualClassRegistrar()}";
                }
                else
                {
                    if (isChecked)
                        cssClass = $"{RootElementClass}-disabled-checked-{VisualClassRegistrar()}";
                    else
                        cssClass = $"{RootElementClass}-disabled-unchecked-{VisualClassRegistrar()}";
                }

                return cssClass;
            });

            ClassBuilder.Register(() => IsInlineLable ? $"{RootElementClass}-inline-{VisualClassRegistrar()}" : string.Empty);
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
