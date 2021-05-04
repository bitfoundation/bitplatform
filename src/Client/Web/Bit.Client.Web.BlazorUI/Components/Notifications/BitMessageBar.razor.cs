using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        private MessageBarStyle messageBarStyle = MessageBarStyle.Default;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter]
        public MessageBarStyle MessageBarStyle
        {
            get => messageBarStyle;
            set
            {
                messageBarStyle = value;
                ClassBuilder.Reset();
            }
        }

        protected override string RootElementClass => "bit-msg-bar";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => MessageBarStyle == MessageBarStyle.Warning ? $"{RootElementClass}-warning-{VisualClassRegistrar()}"
                                      : MessageBarStyle == MessageBarStyle.Severe ? $"{RootElementClass}-severe-{VisualClassRegistrar()}"
                                      : MessageBarStyle == MessageBarStyle.Error ? $"{RootElementClass}-error-{VisualClassRegistrar()}"
                                      : MessageBarStyle == MessageBarStyle.Success ? $"{RootElementClass}-success-{VisualClassRegistrar()}"
                                      : $"{RootElementClass}-default-{VisualClassRegistrar()}");
        }
    }
}
