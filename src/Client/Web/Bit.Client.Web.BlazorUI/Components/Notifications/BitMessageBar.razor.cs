using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public MessageBarStyle MessageBarStyle { get; set; } = MessageBarStyle.Default;

        public string StyleClass => MessageBarStyle == MessageBarStyle.Warning
                                        ? "warning"
                                        : MessageBarStyle == MessageBarStyle.Severe
                                            ? "severe"
                                            : MessageBarStyle == MessageBarStyle.Error
                                                ? "error"
                                                : MessageBarStyle == MessageBarStyle.Success
                                                    ? "success"
                                                    : "default";

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-message-bar");

            ElementClassContainer.Add(MessageBarStyle == MessageBarStyle.Warning ? "warning"
                                    : MessageBarStyle == MessageBarStyle.Severe ? "severe"
                                    : MessageBarStyle == MessageBarStyle.Error ? "error"
                                    : MessageBarStyle == MessageBarStyle.Success ? "success"
                                    : "default");

            return base.GetElementClass();
        }

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
}
