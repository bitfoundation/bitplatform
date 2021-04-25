using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLink
    {
        [Parameter] public string Target { get; set; }

        [Parameter] public string Href { get; set; } = string.Empty;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-link";

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
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
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
