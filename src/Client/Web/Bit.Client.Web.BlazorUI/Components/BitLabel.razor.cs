using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI.Components
{
    public partial class BitLabel
    {
        [Parameter] public string Text { get; set; }
        [Parameter] public bool IsRequired { get; set; }
        [Parameter] public string For { get; set; }

        public string RequiredClass => IsRequired ? "required" : string.Empty;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(Text):
                        Text = (string)parameter.Value;
                        break;
                    case nameof(IsRequired):
                        IsRequired = (bool)parameter.Value;
                        break;
                    case nameof(For):
                        For = (string)parameter.Value;
                        break;
                }
            }

            return base.SetParametersAsync(parameters);
        }

    }
}
