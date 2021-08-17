using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitToggleButton
    {
        private ButtonStyle buttonStyle = ButtonStyle.Primary;

        private int? tabIndex;
        private bool isChecked;

        [Parameter] public bool AllowDisabledFocus { get; set; } = true;
        [Parameter] public string? AriaDescription { get; set; }
        [Parameter] public bool AriaHidden { get; set; }

        /// <summary>
        /// determine if the button is checked state, default is true
        /// </summary>        
        [Parameter]
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (value == isChecked) return;
                isChecked = value;
                ClassBuilder.Reset();
                _ = IsCheckedChanged.InvokeAsync(value);
            }
        }

        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

        /// <summary>
        /// the icon that shows in the button
        /// </summary>
        [Parameter] public string? IconName { get; set; }

        /// <summary>
        /// the text that shows in the label
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// URL the link points to, if provided, button renders as an anchor
        /// </summary>
        [Parameter] public string? Href { get; set; }

        /// <summary>
        /// If Href provided, specifies how to open the link
        /// </summary>
        [Parameter] public string? Target { get; set; }

        /// <summary>
        /// The title to show when the mouse is placed on the button
        /// </summary>
        [Parameter] public string? Title { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<bool> OnChange { get; set; }
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
        private bool IsCheckedHasBeenSet;
        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
                if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;
                IsChecked = !IsChecked;
                await OnChange.InvokeAsync(IsChecked);
            }
        }

        protected override string RootElementClass => "bit-tgl-btn";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                                       ? ButtonStyle == ButtonStyle.Primary
                                           ? $"{RootElementClass}-primary-disabled-{VisualClassRegistrar()}"
                                           : $"{RootElementClass}-standard-disabled-{VisualClassRegistrar()}"
                                       : ButtonStyle == ButtonStyle.Primary
                                           ? $"{RootElementClass}-primary-{VisualClassRegistrar()}"
                                           : $"{RootElementClass}-standard-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => IsChecked is false
                                            ? string.Empty
                                            : ButtonStyle == ButtonStyle.Primary
                                               ? $"{RootElementClass}-primary-checked-{VisualClassRegistrar()}"
                                               : $"{RootElementClass}-standard-checked-{VisualClassRegistrar()}");
        }

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                tabIndex = AllowDisabledFocus ? null : -1;
            }

            await base.OnInitializedAsync();
        }
    }
}
