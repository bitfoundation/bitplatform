using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLabel
    {
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public bool IsRequired { get; set; }

        [Parameter] public string For { get; set; }

        public string RequiredClass => IsRequired ? "required" : string.Empty;

        public override Task SetParametersAsync(ParameterView parameters)
        {
            foreach (ParameterValue parameter in parameters)
            {
                switch (parameter.Name)
                {
                    case nameof(ChildContent):
                        ChildContent = (RenderFragment)parameter.Value;
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