﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar
    {
        protected override string RootElementClass => "bit-msg-bar";

        [Parameter]
        public bool IsMultiline { get; set; } = true;

        [Parameter]
        public BitMessageBarType MessageBarType { get; set; } = BitMessageBarType.Info;

        [Parameter]
        public string DismissIconName { get; set; } = "Clear";

        [Parameter]
        public bool Truncated { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public RenderFragment? Actions { get; set; }

        [Parameter]
        public string? DismissButtonAriaLabel { get; set; }

        [Parameter]
        public string? OverflowButtonAriaLabel { get; set; }

        [Parameter]
        public EventCallback OnDismiss { get; set; }

        protected bool HasDismiss { get => (OnDismiss.HasDelegate); }

        protected bool ExpandSingelLine { get; set; }

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

        protected void Truncate()
        {
            ExpandSingelLine = !ExpandSingelLine;
        }

        public static Dictionary<BitMessageBarType, string> IconMap = new()
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
