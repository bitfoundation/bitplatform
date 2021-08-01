using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        private BitMessageBarType messageBarType = BitMessageBarType.Info;

        /// <summary>
        /// Determines if the message bar is multi-lined. If false, and the text overflows over buttons or to another line
        /// </summary>
        [Parameter] public bool IsMultiline { get; set; } = true;

        /// <summary>
        /// The type of message bar to render
        /// </summary>
        [Parameter]
        public BitMessageBarType MessageBarType
        {
            get => messageBarType;
            set
            {
                ClassBuilder.Reset();
                messageBarType = value;
            }
        }

        /// <summary>
        /// Custom icon to replace the dismiss icon. If unset, default will be the Clear icon
        /// </summary>
        [Parameter] public string DismissIconName { get; set; } = "Clear";

        /// <summary>
        /// Determines if the message bar text is truncated. This parameter is for single-line message bars with no buttons only in a limited space scenario
        /// </summary>
        [Parameter] public bool Truncated { get; set; }

        /// <summary>
        /// The content of message bar, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// The content of the action to show on the message bar, It can be Any custom tag
        /// </summary>
        [Parameter] public RenderFragment? Actions { get; set; }

        /// <summary>
        /// Aria label on dismiss button if onDismiss is defined
        /// </summary>
        [Parameter] public string? DismissButtonAriaLabel { get; set; }

        /// <summary>
        /// Aria label on overflow button if truncated is defined
        /// </summary>
        [Parameter] public string? OverflowButtonAriaLabel { get; set; }

        /// <summary>
        /// Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show
        /// </summary>
        [Parameter] public EventCallback OnDismiss { get; set; }

        private bool HasDismiss { get => (OnDismiss.HasDelegate); }

        private bool ExpandSingleLine { get; set; }

        protected override string RootElementClass => "bit-msg-bar";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                        ? string.Empty
                                        : MessageBarType == BitMessageBarType.Info ? $"{RootElementClass}-info-{VisualClassRegistrar()}"
                                        : MessageBarType == BitMessageBarType.Warning ? $"{RootElementClass}-warning-{VisualClassRegistrar()}"
                                        : MessageBarType == BitMessageBarType.Error ? $"{RootElementClass}-error-{VisualClassRegistrar()}"
                                        : MessageBarType == BitMessageBarType.Blocked ? $"{RootElementClass}-blocked-{VisualClassRegistrar()}"
                                        : MessageBarType == BitMessageBarType.SevereWarning ? $"{RootElementClass}-severe-warning-{VisualClassRegistrar()}"
                                        : $"{RootElementClass}-success-{VisualClassRegistrar()}");
        }

        private void ToggleExpandSingleLine()
        {
            ExpandSingleLine = !ExpandSingleLine;
        }

        private static Dictionary<BitMessageBarType, string> IconMap = new()
        {
            [BitMessageBarType.Info] = "Info",
            [BitMessageBarType.Warning] = "Info",
            [BitMessageBarType.Error] = "ErrorBadge",
            [BitMessageBarType.Blocked] = "Blocked2",
            [BitMessageBarType.SevereWarning] = "Warning",
            [BitMessageBarType.Success] = "Completed"
        };
    }
}
