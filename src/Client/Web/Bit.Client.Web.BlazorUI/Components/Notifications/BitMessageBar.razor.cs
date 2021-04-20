using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public MessageBarStyle MessageBarStyle { get; set; } = MessageBarStyle.Default;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(MessageBarStyle):
                        MessageBarStyle = (MessageBarStyle)parameter.Value;
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
