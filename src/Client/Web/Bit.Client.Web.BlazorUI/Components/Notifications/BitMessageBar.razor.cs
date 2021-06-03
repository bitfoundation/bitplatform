using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        private MessageBarStyle messageBarStyle = MessageBarStyle.Default;

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter] public string? MessageBarIconName { get; set; }

        [Parameter] public string DismissIconName { get; set; } = "Clear";

        [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

        [Parameter] public bool Truncated { get; set; } = false;

        public bool ElementTruncateState { get; set; } = true;

        [Parameter] public bool IsMultiline { get; set; } = false;

        [Parameter]
        public MessageBarStyle MessageBarStyle
        {
            get => messageBarStyle;
            set
            {
                messageBarStyle = value;
                ChooseMessageBarIcon();
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
                                      : MessageBarStyle == MessageBarStyle.Blocked ? $"{RootElementClass}-blocked-{VisualClassRegistrar()}"
                                      : $"{RootElementClass}-default-{VisualClassRegistrar()}");
        }

        public void ToggleElementTruncate()
        {
            ElementTruncateState = !ElementTruncateState;
        }

        public void ChooseMessageBarIcon()
        {
            switch (MessageBarStyle)
            {
                case MessageBarStyle.Default:
                    MessageBarIconName = "Info";
                    break;

                case MessageBarStyle.Error:
                    MessageBarIconName = "ErrorBadge";
                    break;

                case MessageBarStyle.Blocked:
                    MessageBarIconName = "Blocked2";
                    break;

                case MessageBarStyle.Success:
                    MessageBarIconName = "Completed";
                    break;

                case MessageBarStyle.Warning:
                    MessageBarIconName = "ErrorBadge";
                    break;

                case MessageBarStyle.Severe:
                    MessageBarIconName = "Warning";
                    break;
            }
        }

        public void Dismiss(MouseEventArgs args)
        {
            OnDismiss.InvokeAsync(args);
        }
    }
}
