using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using System;

namespace Bit.Client.Web.BlazorUI.Checkboxes
{
    public partial class BitCheckbox
    {
        [Inject] public IJSRuntime JSRuntime { get; set; }


        [Parameter] public string Label { get; set; }
        [Parameter] public RenderFragment CustomLabel { get; set; }
        [Parameter] public bool IsIndeterminate { get; set; }
        [Parameter] public bool IsChecked { get; set; }
        [Parameter] public BoxSide BoxSide { get; set; }
        [Parameter] public Action<bool> OnCheckboxChange { get; set; }


        public ElementReference TargetCheckbox { get; set; }
        public string IndeterminateClass => IsIndeterminate ? "indeterminate" : string.Empty;
        public string CheckedClass => IsChecked ? "checked" : string.Empty;
        public string BoxSideClass => BoxSide == BoxSide.End ? "box-side-end" : "box-side-start";
        public bool HasCustomLabel => CustomLabel is not null ? true : false;

        protected void HandleIndeterminate() {
            if (IsIndeterminate) {
                IsIndeterminate = false;
                _ = JSRuntime.SetElementProperty(TargetCheckbox, "indeterminate", IsIndeterminate);
            }
        }
        protected void HandleCheckboxClick()
        {
            if (IsEnabled)
            {
                if (IsIndeterminate)
                {
                    HandleIndeterminate();
                }
                else
                {
                    if (IsChecked)
                    {
                        IsChecked = false;
                    }
                    else
                    {
                        IsChecked = true;
                    }
                }
            }
            OnCheckboxChange?.Invoke(IsChecked);
        }
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) 
            {
                _ = JSRuntime.SetElementProperty(TargetCheckbox, "indeterminate", IsIndeterminate);
            }
            
            return base.OnAfterRenderAsync(firstRender);
        }
        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Label):
                        Label = (string)parameter.Value;
                        break;
                    case nameof(CustomLabel):
                        CustomLabel = (RenderFragment)parameter.Value;
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
                        OnCheckboxChange = (Action<bool>)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }

    public enum BoxSide { 
        Start,
        End
    }
}
