using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitMessageBar : BitComponentBase
    {
        [Parameter]
        public bool IsMultiline { get; set; } = true;

        [Parameter]
        public BitMessageBarType MessageBarType { get; set; } = BitMessageBarType.Info;

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

        [Parameter]
        public BitMessageBar? ComponentRef
        {
            get => componentRef;
            set
            {
                if (value == componentRef)
                    return;
                componentRef = value;
                if (value != null)
                    MessageBarType = value.MessageBarType;
                ComponentRefChanged.InvokeAsync(value);
            }
        }

        [Parameter]
        public EventCallback<BitMessageBar> ComponentRefChanged { get; set; }

        private BitMessageBar? componentRef;

        protected bool HasDismiss { get => (OnDismiss.HasDelegate); }
        protected override string RootElementClass => $"bit-msg-bar";

        protected bool HasExpand { get => (Truncated && Actions == null); }

        protected bool ExpandSingelLine { get; set; }

        protected void Truncate()
        {
            ExpandSingelLine = !ExpandSingelLine;
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                ComponentRef = this as BitMessageBar;
            return base.OnAfterRenderAsync(firstRender);
        }

        protected string GetTypeCss()
        {
            return MessageBarType switch
            {
                BitMessageBarType.Warning => $"{RootElementClass}--warning",
                BitMessageBarType.Error => $"{RootElementClass}--error",
                BitMessageBarType.Blocked => $"{RootElementClass}--blocked",
                BitMessageBarType.SevereWarning => $"{RootElementClass}--severeWarning",
                BitMessageBarType.Success => $"{RootElementClass}--success",
                _ => "",
            };
        }
    }
}
