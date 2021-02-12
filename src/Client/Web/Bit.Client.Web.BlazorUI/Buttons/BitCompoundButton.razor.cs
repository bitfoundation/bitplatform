using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Buttons
{
    public partial class BitCompoundButton
    {
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter] public string Text { get; set; }

        [Parameter] public string SecondaryText { get; set; }

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
                if (parameter.Name is nameof(OnClick))
                    OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                else if (parameter.Name is nameof(Text))
                    Text = (string)parameter.Value;
                else if (parameter.Name is nameof(SecondaryText))
                    SecondaryText = (string)parameter.Value;
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
