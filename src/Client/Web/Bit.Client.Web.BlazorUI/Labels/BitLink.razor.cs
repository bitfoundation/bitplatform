using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Labels
{
    public partial class BitLink
    {
        [Parameter] public string Text { get; set; }
        [Parameter] public string Href { get; set; } = string.Empty;
        [Parameter] public string Target { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected virtual async Task HandleClick(MouseEventArgs e)
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
                    case nameof(Text):
                        Text = (string)parameter.Value;
                        break;
                    case nameof(Href):
                        Href = (string)parameter.Value;
                        break;
                    case nameof(Target):
                        Target = (string)parameter.Value;
                        break;
                    case nameof(OnClick):
                        OnClick = (EventCallback<MouseEventArgs>)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
