using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Notifications
{
    public partial class BitMessageBar
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public MessageBarStyle Style { get; set; } = MessageBarStyle.Default;

        public string StyleClass => Style == MessageBarStyle.Warning ? "warning" : Style == MessageBarStyle.Severe ? "severe" : Style == MessageBarStyle.Error ? "error" : Style == MessageBarStyle.Success ? "success" : "default";

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Style):
                        Style = (MessageBarStyle)parameter.Value;
                        break;
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }

    public enum MessageBarStyle
    {
        Default,
        Error,
        Success,
        Warning,
        Severe
    }
}
