using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public bool IsIndeterminate { get; set; }

        [Parameter] public bool IsChecked { get; set; }

        [Parameter] public BoxSide BoxSide { get; set; }

        [Parameter] public EventCallback<bool> OnCheckboxChange { get; set; }

        public ElementReference TargetCheckbox { get; set; }

        public string IndeterminateClass => IsIndeterminate ? "indeterminate" : string.Empty;

        public string CheckedClass => IsChecked ? "checked" : string.Empty;

        public string BoxSideClass => BoxSide == BoxSide.End ? "box-side-end" : "box-side-start";

        protected async Task HandleIndeterminate()
        {
            if (IsIndeterminate)
            {
                IsIndeterminate = false;
                await JSRuntime.SetElementProperty(TargetCheckbox, "indeterminate", IsIndeterminate);
            }
        }

        protected async Task HandleCheckboxClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                if (IsIndeterminate)
                {
                    await HandleIndeterminate();
                }
                else
                {
                    IsChecked = !IsChecked;
                }

                await OnCheckboxChange.InvokeAsync(IsChecked);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.SetElementProperty(TargetCheckbox, "indeterminate", IsIndeterminate);
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

                    case nameof(OnCheckboxChange):
                        OnCheckboxChange = (EventCallback<bool>)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }

    public enum BoxSide
    {
        Start,
        End
    }
}