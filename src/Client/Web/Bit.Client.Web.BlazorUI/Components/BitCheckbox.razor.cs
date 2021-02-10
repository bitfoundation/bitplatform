using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Components
{
    public partial class BitCheckbox
    {

        [Parameter] public string Label { get; set; }
        [Parameter] public bool IsChecked { get; set; }
        [Parameter] public bool IsIndeterminate { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnCheckboxClick { get; set; }

        public string CheckedClass => IsChecked ? "checked" : string.Empty;
        public string IndeterminateClass => IsIndeterminate ? "indeterminate" : string.Empty;

        public string InputUniqueIdentifier = Guid.NewGuid().ToString("n");

        protected void HandleCheckboxClick()
        {
            if (IsEnabled)
            {
                if (IsChecked)
                {
                    IsChecked = false;
                }
                else {
                    IsChecked = true;
                }
                StateHasChanged();
            }
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
                    case nameof(IsChecked):
                        IsChecked = (bool)parameter.Value;
                        break;
                    case nameof(IsIndeterminate):
                        IsIndeterminate = (bool)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }
    }
}
