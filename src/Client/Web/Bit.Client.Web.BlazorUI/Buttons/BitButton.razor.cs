using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Buttons
{
    public partial class BitButton
    {
        [Parameter] public ButtonStyle Style { get; set; } = ButtonStyle.Primary;
        public string StyleClass => !IsEnabled ? "" : Style == ButtonStyle.Primary ? "style-primary" : "style-standard";
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Style):
                        Style = (ButtonStyle)parameter.Value;
                        break;
                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        break;
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }

    public enum ButtonStyle
    {
        Primary,
        Standard
    }
}
