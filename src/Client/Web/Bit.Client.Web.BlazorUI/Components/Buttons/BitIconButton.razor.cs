﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
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
        /// The aria label of the icon button for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaLabel { get; set; }

        /// <summary>
        /// The icon name for the icon shown in the button
        /// </summary>
        [Parameter] public string? IconName { get; set; }

        /// <summary>
        /// The tooltip to show when the mouse is placed on the icon button
        /// </summary>
        [Parameter] public string? ToolTip { get; set; }

        /// <summary>
        /// Callback for when the button clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

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
