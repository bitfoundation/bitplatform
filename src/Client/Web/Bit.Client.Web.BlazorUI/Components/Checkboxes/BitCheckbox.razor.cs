using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCheckbox
    {
        public ElementReference CheckboxElement { get; set; }

        [Inject] public IJSRuntime JSRuntime { get; set; }

        [Parameter] public bool IsChecked { get; set; }

        [Parameter] public BoxSide BoxSide { get; set; }

        [Parameter] public bool IsIndeterminate { get; set; }

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<bool> OnChange { get; set; }

        protected override string GetElementClass()
        {
            ElementClassContainer.Clear();
            ElementClassContainer.Add("bit-checkbox-container");

            if (IsIndeterminate)
            {
                ElementClassContainer.Add("indeterminate");
            }

            if (IsChecked)
            {
                ElementClassContainer.Add("checked");
            }

            ElementClassContainer.Add(BoxSide == BoxSide.End ? "box-side-end" : "box-side-start");

            return base.GetElementClass();
        }

        protected async Task HandleCheckboxClick(MouseEventArgs e)
        {
            if (!IsEnabled) return;

            if (IsIndeterminate)
            {
                IsIndeterminate = false;
                await JSRuntime.SetElementProperty(CheckboxElement, "indeterminate", IsIndeterminate);
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
                await JSRuntime.SetElementProperty(CheckboxElement, "indeterminate", IsIndeterminate);
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
