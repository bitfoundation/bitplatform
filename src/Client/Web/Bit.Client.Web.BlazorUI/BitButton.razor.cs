using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitButton
    {
        public ElementReference Button { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
                await OnClick.InvokeAsync(e);
        }

        [Parameter]
        public string Text { get; set; }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                if (parameter.Name is nameof(OnClick))
                    OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                else if (parameter.Name is nameof(Text))
                    Text = (string)parameter.Value;
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
