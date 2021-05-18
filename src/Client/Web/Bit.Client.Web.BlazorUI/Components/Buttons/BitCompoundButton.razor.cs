using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCompoundButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        [Parameter] public bool AllowDisabledFocus { get; set; } = true;
        [Parameter] public string AriaDescription { get; set; }
        [Parameter] public bool AriaHidden { get; set; }
        [Parameter] public string AriaLabel { get; set; }
        [Parameter] public string Text { get; set; }
        [Parameter] public string SecondaryText { get; set; }

        [Parameter]
        public ButtonStyle ButtonStyle
        {
            get => buttonStyle;
            set
            {
                buttonStyle = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-cmp-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                        ? string.Empty
                                        : ButtonStyle == ButtonStyle.Primary
                                            ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                            : $"{RootElementClass}-standard-{VisualClassRegistrar()}");
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }

        private Dictionary<string, object> SetNewAttributes()
        {
            var attributes = new Dictionary<string, object>();

            if (IsEnabled is false && AllowDisabledFocus is false)
            {
                attributes.Add("tabindex", -1);
            }

            return attributes;
        }
    }
}
