using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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

        protected override string RootElementClass => "bit-button";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false ? string.Empty :
                                        ButtonStyle == ButtonStyle.Primary ? "primary" :
                                        "standard");
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }
    }
}
