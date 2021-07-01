using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        private bool isIndeterminate;
        private BoxSide boxSide;
        private bool isChecked;
        private bool IsCheckedHasBeenSet;

        public ElementReference CheckboxElement { get; set; }

        [Inject] public IJSRuntime? JSRuntime { get; set; }

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
        /// Determines whether the checkbox should be shown before the label (start) or after (end)
        /// </summary>
        [Parameter]
        public BoxSide BoxSide
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
        /// An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on IsChecked state
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
        /// The content of checkbox, It can be Any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Callback that is called when the checked value has changed
        /// </summary>
        [Parameter] public EventCallback<bool> OnChange { get; set; }

        protected override string RootElementClass => "bit-chb";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsIndeterminate ? $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => IsChecked ? $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => BoxSide == BoxSide.End
                                        ? $"{RootElementClass}-end-{VisualClassRegistrar()}"
                                        : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false && IsChecked
                                        ? $"{RootElementClass}-checked-disabled-{VisualClassRegistrar()}"
                                        : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false && IsIndeterminate
                                        ? $"{RootElementClass}-indeterminate-disabled-{VisualClassRegistrar()}"
                                        : string.Empty);
        }

        protected async Task HandleCheckboxClick()
        {
            if (IsEnabled is false) return;

            if (IsIndeterminate)
            {
                if (IsCheckedHasBeenSet && IsIndeterminateChanged.HasDelegate is false) return;
                IsIndeterminate = false;
            }
            else
            {
                if (IsCheckedHasBeenSet && IsCheckedChanged.HasDelegate is false) return;
                IsChecked = !IsChecked;
            }

            await OnChange.InvokeAsync(IsChecked);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _ = JSRuntime?.SetProperty(CheckboxElement, "indeterminate", IsIndeterminate);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            IsCheckedHasBeenSet = false;

            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(IsChecked):
                        IsCheckedHasBeenSet = true;
                        IsChecked = (bool)parameter.Value;
                        break;
                    case nameof(IsCheckedChanged):
                        IsCheckedChanged = (EventCallback<bool>)parameter.Value;
                        break;
                    case nameof(BoxSide):
                        BoxSide = (BoxSide)parameter.Value;
                        break;
                    case nameof(IsIndeterminate):
                        IsIndeterminate = (bool)parameter.Value;
                        break;
                    case nameof(IsIndeterminateChanged):
                        IsIndeterminateChanged = (EventCallback<bool>)parameter.Value;
                        break;
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment?)parameter.Value;
                        break;
                    case nameof(OnChange):
                        OnChange = (EventCallback<bool>)parameter.Value;
                        break;
                }
            }
            return base.SetParametersAsync(parameters);
        }
    }
}
