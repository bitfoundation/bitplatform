using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        private bool isIndeterminate;
        private bool isChecked;
        private bool IsCheckedHasBeenSet;
        private bool IsIndeterminateHasBeenSet;
        private BitBoxSide boxSide;

        [Inject] public IJSRuntime? JSRuntime { get; set; }

        public ElementReference CheckboxElement { get; set; }

        /// <summary>
        /// Detailed description of the checkbox input for the benefit of screen readers
        /// </summary>
        [Parameter] public string? AriaDescription { get; set; }

        /// <summary>
        /// ID for element that contains label information for the checkbox
        /// </summary>
        [Parameter] public string? AriaLabelledby { get; set; }

        /// <summary>
        /// The position in the parent set (if in a set) for aria-posinset
        /// </summary>
        [Parameter] public int? AriaPositionInSet { get; set; }

        /// <summary>
        /// The total size of the parent set (if in a set) for aria-setsize
        /// </summary>
        [Parameter] public int? AriaSetSize { get; set; }

        /// <summary>
        /// Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI
        /// </summary>
        [Parameter] public string? Name { get; set; }

        /// <summary>
        /// Title text applied to the root element and the hidden checkbox input
        /// </summary>
        [Parameter] public string? Title { get; set; }

        /// <summary>
        /// Default checkbox state
        /// Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.
        /// </summary>
        [Parameter] public bool? DefaultIsChecked { get; set; }

        /// <summary>
        /// Checkbox state, control the checked state at a higher level
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

        /// <summary>
        /// Callback that is called when the IsChecked parameter changed
        /// </summary>
        [Parameter] public EventCallback<bool> IsCheckedChanged { get; set; }

        /// <summary>
        /// Custom icon for the check mark rendered by the checkbox instade of default check mark icon
        /// </summary>
        [Parameter] public BitIcon CheckmarkIconName { get; set; } = BitIcon.Accept;

        /// <summary>
        /// he aria label of the icon for the benefit of screen readers
        /// </summary>
        [Parameter] public string? CheckmarkIconAriaLabel { get; set; }

        /// <summary>
        /// Determines whether the checkbox should be shown before the label (start) or after (end)
        /// </summary>
        [Parameter]
        public BitBoxSide BoxSide
        {
            get => boxSide;
            set
            {
                if (value == boxSide) return;
                boxSide = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Default indeterminate visual state for checkbox
        /// </summary>
        [Parameter] public bool? DefaultIsIndeterminate { get; set; }

        /// <summary>
        /// An indeterminate visual state for checkbox. 
        /// Setting indeterminate state takes visual precedence over checked given but does not affect on IsChecked state
        /// </summary>
        [Parameter]
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                if (value == isIndeterminate) return;
                isIndeterminate = value;
                _ = JSRuntime?.SetProperty(CheckboxElement, "indeterminate", value);
                ClassBuilder.Reset();
                _ = IsIndeterminateChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        ///  Callback that is called when the IsIndeterminate parameter changed
        /// </summary>
        [Parameter] public EventCallback<bool> IsIndeterminateChanged { get; set; }

        /// <summary>
        ///  Callback that is called when the check box is cliced
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// The content of checkbox, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Callback that is called when the checked value has changed
        /// </summary>
        [Parameter] public EventCallback<bool> OnChange { get; set; }

        public string InputId { get; set; } = string.Empty;

        protected override string RootElementClass => "bit-chb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsIndeterminate ? $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => IsChecked ? $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => BoxSide == BitBoxSide.End
                                        ? $"{RootElementClass}-end-{VisualClassRegistrar()}"
                                        : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false && IsChecked
                                        ? $"{RootElementClass}-checked-disabled-{VisualClassRegistrar()}"
                                        : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false && IsIndeterminate
                                        ? $"{RootElementClass}-indeterminate-disabled-{VisualClassRegistrar()}"
                                        : string.Empty);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _ = JSRuntime?.SetProperty(CheckboxElement, "indeterminate", IsIndeterminate);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected override async Task OnParametersSetAsync()
        {
            if (IsCheckedHasBeenSet is false && DefaultIsChecked is not null)
            {
                IsChecked = DefaultIsChecked.Value;
            }

            if (IsIndeterminateHasBeenSet is false && DefaultIsIndeterminate is not null)
            {
                IsIndeterminate = DefaultIsIndeterminate.Value;
            }

            InputId = $"checkbox-{UniqueId}";
            await base.OnParametersSetAsync();
        }

        private async Task HandleCheckboxClick(MouseEventArgs args)
        {
            if (IsEnabled is false) return;
            await OnClick.InvokeAsync(args);

            if (IsIndeterminate)
            {
                if (IsIndeterminateHasBeenSet && IsIndeterminateChanged.HasDelegate is false) return;
                IsIndeterminate = false;
            }
            else
            {
                if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;
                IsChecked = !IsChecked;
                await OnChange.InvokeAsync(IsChecked);
            }
        }
    }
}
