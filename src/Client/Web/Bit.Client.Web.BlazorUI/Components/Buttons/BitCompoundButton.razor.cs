using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCompoundButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        private int? tabIndex;

        /// <summary>
        /// Whether the compound button can have focus in disabled mode
        /// </summary>
        [Parameter] public bool AllowDisabledFocus { get; set; } = true;

        /// <summary>
        /// Detailed description of the compound button for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

        /// <summary>
        /// If true, add an aria-hidden attribute instructing screen readers to ignore the element
        /// </summary>
        [Parameter] public bool AriaHidden { get; set; }

        [Parameter] public string? AriaLabel { get; set; }

        [Parameter] public string? Text { get; set; }

        /// <summary>
        /// Description of the action compound button takes
        /// </summary>
        [Parameter] public string? SecondaryText { get; set; }

        /// <summary>
        /// The style of compound button, Possible values: Primary | Standard
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

        /// <summary>
        /// Callback for when the compound button clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        [Parameter] public string? AriaDescription { get; set; }

        [Parameter] public bool AriaHidden { get; set; }

        protected override string RootElementClass => "bit-cmp-btn";

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
