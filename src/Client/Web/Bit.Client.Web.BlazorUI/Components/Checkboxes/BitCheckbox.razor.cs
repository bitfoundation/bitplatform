using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        private bool isIndeterminate;
        private BoxSide boxSide;
        private bool isChecked;

        public ElementReference CheckboxElement { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public bool IsChecked
        {
            get => isChecked; set
            {
                isChecked = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public BoxSide BoxSide
        {
            get => boxSide; set
            {
                boxSide = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set
            {
                isIndeterminate = value;
                JSRuntime.SetProperty(CheckboxElement, "indeterminate", value);
                ClassBuilder.Reset();
            }
        }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<bool> OnChange { get; set; }

        protected override string RootElementClass => "bit-chb-container";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsIndeterminate ? $"{RootElementClass}-indeterminate-{VisualClassRegistrar()}" : string.Empty);
            ClassBuilder.Register(() => IsChecked ? $"{RootElementClass}-checked-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => BoxSide == BoxSide.End
                                        ? $"{RootElementClass}-box-side-end-{VisualClassRegistrar()}"
                                        : $"{RootElementClass}-box-side-start-{VisualClassRegistrar()}");

            ClassBuilder.Register(() => IsEnabled is false && IsChecked
                                        ? $"{RootElementClass}-checked-disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsEnabled is false && IsIndeterminate
                                        ? $"{RootElementClass}-indeterminate-disabled-{VisualClassRegistrar()}" : string.Empty);
        }

        protected async Task HandleCheckboxClick(MouseEventArgs e)
        {
            if (IsEnabled is false) return;

            if (IsIndeterminate)
            {
                IsIndeterminate = false;
            }
            else
            {
                IsChecked = !IsChecked;
            }

            await OnChange.InvokeAsync(IsChecked);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.SetProperty(CheckboxElement, "indeterminate", IsIndeterminate);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
                        break;

                    case nameof(IsChecked):
                        IsChecked = (bool)parameter.Value;
                        break;

                    case nameof(IsIndeterminate):
                        IsIndeterminate = (bool)parameter.Value;
                        break;

                    case nameof(BoxSide):
                        BoxSide = (BoxSide)parameter.Value;
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
