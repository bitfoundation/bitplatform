using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCompoundButton
    {
        [Parameter] public string Text { get; set; }

        [Parameter] public string SecondaryText { get; set; }

        [Parameter] public ButtonStyle Style { get; set; } = ButtonStyle.Primary;

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        public string StyleClass => !IsEnabled ? "" : Style == ButtonStyle.Primary ? "primary" : "standard";

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
                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        break;

                    case nameof(Text):
                        Text = (string)parameter.Value;
                        break;

                    case nameof(SecondaryText):
                        SecondaryText = (string)parameter.Value;
                        break;

                    case nameof(Style):
                        Style = (ButtonStyle)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}