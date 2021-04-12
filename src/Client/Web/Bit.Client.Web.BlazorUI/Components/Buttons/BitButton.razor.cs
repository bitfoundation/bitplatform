using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitButton
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter] public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-button");
            if (IsEnabled)
            {
                ElementClassContainer.Add(ButtonStyle == ButtonStyle.Primary ? "primary" : "standard");
            }
            return base.GetElementClass();
        }

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
                    case nameof(ButtonStyle):
                        ButtonStyle = (ButtonStyle)parameter.Value;
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
}
