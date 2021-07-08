using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        private MessageBarStyle messageBarStyle = MessageBarStyle.Default;

        public bool ElementTruncateState { get; set; } = true;

        /// <summary>
        /// The content of messagebar, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Custom icon to replace the messagebar icon. If unset, default will be the icon set by MessageBarStyle
        /// </summary>
        [Parameter] public string? MessageBarIconName { get; set; }

        /// <summary>
        /// Custom icon to replace the dismiss icon. If unset, default will be the Clear icon
        /// </summary>
        [Parameter] public string DismissIconName { get; set; } = "Clear";

        /// <summary>
        /// Whether the messagebar has a dismiss button and its callback. If null, dismiss button won't show
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

        /// <summary>
        /// Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario
        /// </summary>
        [Parameter] public bool Truncated { get; set; } = false;

        /// <summary>
        /// Determines if the messagebar is multi lined. If false, and the text overflows over buttons or to another line
        /// </summary>
        [Parameter] public bool IsMultiline { get; set; } = false;

        /// <summary>
        /// The style of MessageBar to render
        /// </summary>
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
