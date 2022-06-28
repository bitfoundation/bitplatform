using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorUI
{
    public partial class BitMessageBar
    {
        private BitMessageBarType messageBarType = BitMessageBarType.Info;
        private BitIconName? messageBarIcon;
        private bool ExpandSingleLine;

        /// <summary>
        /// Determines if the message bar is multi lined. If false, and the text overflows over buttons or to another line, it is clipped
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
        /// Custom Fabric icon name to replace the dismiss icon. If unset, default will be the Fabric Clear icon
        /// </summary>
        [Parameter] public BitIconName DismissIconName { get; set; } = BitIconName.Clear;

        /// <summary>
        /// Custom icon to replace the message bar icon. If unset, default will be the icon set by messageBarType.
        /// </summary>
        [Parameter] public BitIconName? MessageBarIconName { get; set; }

        /// <summary>
        /// Determines if the message bar text is truncated. If true, a button will render to toggle between a single line view and multiline view. This parameter is for single line message bars with no buttons only in a limited space scenario
        /// </summary>
        [Parameter] public bool Truncated { get; set; }

        /// <summary>
        /// The content of message bar
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// The content of the action to show on the message bar
        /// </summary>
        [Parameter] public RenderFragment? Actions { get; set; }

        /// <summary>
        /// Aria label on dismiss button if onDismiss is defined
        /// </summary>
        [Parameter] public string? DismissButtonAriaLabel { get; set; }

        /// <summary>
        /// Aria label on overflow button if truncated is true
        /// </summary>
        [Parameter] public string? OverflowButtonAriaLabel { get; set; }

        /// <summary>
        /// Custom role to apply to the message bar
        /// </summary>
        [Parameter] public string? Role { get; set; }

        /// <summary>
        /// Whether the message bar has a dismiss button and its callback. If null, dismiss button won't show
        /// </summary>
        [Parameter] public EventCallback OnDismiss { get; set; }

        public string LabelId { get; set; } = string.Empty;

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

        protected override Task OnParametersSetAsync()
        {
            messageBarIcon = MessageBarIconName.HasValue ? MessageBarIconName.Value : IconMap[MessageBarType];
            LabelId = $"MessageBar{UniqueId}";

            return base.OnParametersSetAsync();
        }

        private void ToggleExpandSingleLine()
        {
            ExpandSingleLine = !ExpandSingleLine;
        }

        private static Dictionary<BitMessageBarType, BitIconName> IconMap = new()
        {
            [BitMessageBarType.Info] = BitIconName.Info,
            [BitMessageBarType.Warning] = BitIconName.Info,
            [BitMessageBarType.Error] = BitIconName.ErrorBadge,
            [BitMessageBarType.Blocked] = BitIconName.Blocked2,
            [BitMessageBarType.SevereWarning] = BitIconName.Warning,
            [BitMessageBarType.Success] = BitIconName.Completed
        };

        private string GetTextRole()
        {
            switch (messageBarType)
            {
                case BitMessageBarType.Blocked:
                case BitMessageBarType.Error:
                case BitMessageBarType.SevereWarning:
                    return "alert";
                default:
                    return "status";
            }
        }

        private string GetAnnouncementPriority()
        {
            switch (messageBarType)
            {
                case BitMessageBarType.Blocked:
                case BitMessageBarType.Error:
                case BitMessageBarType.SevereWarning:
                    return "assertive";
                default:
                    return "polite";
            }
        }

        private bool HasDismiss { get => (OnDismiss.HasDelegate); }
        private string? GetAriaDescribedby => Actions is not null || HasDismiss ? LabelId : null;
        private string? GetRootElementRole => Actions is not null || HasDismiss ? "region" : null;
    }
}
