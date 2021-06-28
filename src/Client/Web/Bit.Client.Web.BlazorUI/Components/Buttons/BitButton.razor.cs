using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        private int? tabIndex;

        /// <summary>
        /// Allow focus on button when it is disabled
        /// </summary>
        [Parameter] public bool AllowDisabledFocus { get; set; } = true;

        /// <summary>
        /// Define a string value to describe the button for a screen reader
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

        /// <summary>
        /// Hide button from a screen reader and display it visually
        /// </summary>
        [Parameter] public bool AriaHidden { get; set; }

        /// <summary>
        /// Define a string to label the button for a screen reader
        /// </summary>
        [Parameter] public string? AriaLabel { get; set; }

        /// <summary>
        /// If provided, button renders as an anchor
        /// </summary>
        [Parameter] public string? Href { get; set; }

        /// <summary>
        /// If Href provided, specifies how to open the link, Possible values: _blank | _self | _parent | _top 
        /// </summary>
        [Parameter] public string? Target { get; set; }

        /// <summary>
        /// Define a string value as a tooltip
        /// </summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>
        /// Define Any custom tag or text to associate with the button
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Execute a method when a button is clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Define The style of button, Possible values: Primary | Standard
        /// </summary>
        [Parameter]
        public ButtonStyle ButtonStyle
        {
            get => buttonStyle;
            set
            {
                buttonStyle = value;
                ClassBuilder.Reset();
            }
        }

        protected override string RootElementClass => "bit-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                           ? string.Empty
                                           : ButtonStyle == ButtonStyle.Primary
                                               ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                               : $"{RootElementClass}-standard-{VisualClassRegistrar()}");
        }

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
