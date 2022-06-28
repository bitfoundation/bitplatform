using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI
{
    public partial class BitIconButton
    {
        private int? tabIndex;

        /// <summary>
        /// Whether the icon button can have focus in disabled mode
        /// </summary>
        [Parameter] public bool AllowDisabledFocus { get; set; } = true;

        /// <summary>
        /// Detailed description of the icon button for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

        /// <summary>
        /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
        /// </summary>
        [Parameter] public bool AriaHidden { get; set; }

        /// <summary>
        /// The icon name for the icon shown in the button
        /// </summary>
        [Parameter] public BitIconName IconName { get; set; }

        /// <summary>
        /// The tooltip to show when the mouse is placed on the icon button
        /// </summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>
        /// Callback for when the button clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// URL the link points to, if provided, button renders as an anchor
        /// </summary>
        [Parameter] public string? Href { get; set; }

        /// <summary>
        /// If Href provided, specifies how to open the link
        /// </summary>
        [Parameter] public string? Target { get; set; }

        /// <summary>
        /// The type of the button
        /// </summary>
        [Parameter] public BitButtonType ButtonType { get; set; } = BitButtonType.Button;

        protected override string RootElementClass => "bit-ico-btn";

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                tabIndex = AllowDisabledFocus ? null : -1;
            }

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }
    }
}
