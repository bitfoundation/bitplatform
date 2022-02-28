using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitModal
    {
        private bool isOpen;
        private bool IsOpenHasBeenSet { get; set; }

        /// <summary>
        /// Determines the ARIA role of the dialog (alertdialog/dialog). If this is set, it will override the ARIA role determined by IsBlocking and IsModeless.
        /// </summary>
        [Parameter]
        public bool? IsAlert
        {
            get => IsAlertRole;
            set
            {
                IsAlertRole = value ?? (IsBlocking && !IsModeless);
            }
        }

        /// <summary>
        /// Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).
        /// </summary>
        
        [Parameter] public bool IsBlocking { get; set; }

        /// <summary>
        /// Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.
        /// </summary>
        [Parameter] public bool IsModeless { get; set; }

        /// <summary>
        /// Whether the dialog is displayed.
        /// </summary>
        [Parameter]
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                if (value == isOpen) return;
                isOpen = value;
                _ = IsOpenChanged.InvokeAsync(value);
            }
        }
        [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

        /// <summary>
        /// The content of the Modal, it can be any custom tag or text.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// A callback function for when the Modal is dismissed light dismiss, before the animation completes.
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnDismiss { get; set; }

        /// <summary>
        /// ARIA id for the subtitle of the Modal, if any.
        /// </summary>
        [Parameter] public string SubtitleAriaId { get; set; } = string.Empty;

        /// <summary>
        /// ARIA id for the title of the Modal, if any.
        /// </summary>
        [Parameter] public string TitleAriaId { get; set; } = string.Empty;

        protected override string RootElementClass => "bit-mdl";
        private bool IsAlertRole { get; set; }

        private void CloseCallout(MouseEventArgs e)
        {
            if (IsOpenHasBeenSet && IsOpenChanged.HasDelegate is false) return;

            if (IsBlocking == false)
            {
                IsOpen = false;
                _ = OnDismiss.InvokeAsync(e);
            }
        }
    }
}
